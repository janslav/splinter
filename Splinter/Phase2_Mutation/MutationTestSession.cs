using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.ReflectionModel;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using System.Diagnostics;
using System.Threading;

using Microsoft.Practices.Unity;

using log4net;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;

using Splinter;
using Splinter.Contracts;
using Splinter.Contracts.DTOs;
using Splinter.Phase2_Mutation.NinjaTurtles;
using Splinter.Phase2_Mutation.NinjaTurtles.Turtles;
using Splinter.Phase2_Mutation.DTOs;
using Splinter.Utils;
using Splinter.Utils.Cecil;

namespace Splinter.Phase2_Mutation
{
    /// <summary>
    /// The creation of mutated assemblies and running tests against them is driven from here.
    /// </summary>
    public interface IMutationTestSession
    {
        IReadOnlyCollection<SingleMutationTestResult> CreateMutantsAndRunTestsOnThem(
            MutationTestSessionInput input,
            IProgress<Tuple<int, int, int>> progress = null);
    }

    public class MutationTestSession : IMutationTestSession
    {
        private readonly ILog log;

        private readonly ICodeCache codeCache;

        private readonly IExecutableUtils executableUtils;

        private IReadOnlyCollection<IMethodTurtle> allTurtles = null;

        public MutationTestSession(
            ILog log,
            IUnityContainer container,
            IExecutableUtils executableUtils,
            ICodeCache codeCache)
        {
            this.log = log;
            this.codeCache = codeCache;
            this.executableUtils = executableUtils;

            this.ImportTurtles(container);
        }

        /// <summary>
        /// Turtles = the implementatinos of method code mutators
        /// </summary>
        private void ImportTurtles(IUnityContainer container)
        {
            var catalog = new ApplicationCatalog();

            this.allTurtles = catalog
                .Where(i => i.Exports(typeof(IMethodTurtle)))
                .Select(i =>
                {
                    var type = ReflectionModelServices.GetPartType(i).Value;
                    var turtle = container.Resolve(type);
                    return (IMethodTurtle)turtle;
                }).ToArray();
        }

        public IReadOnlyCollection<SingleMutationTestResult> CreateMutantsAndRunTestsOnThem(
            MutationTestSessionInput input,
            IProgress<Tuple<int, int, int>> progress)
        {

            var allMutants = new List<Mutation>();

            try
            {
                var unmutableMethods = new List<SingleMutationTestResult>();

                int testsCount = 0;
                int testsFinishedCount = 0;
                int testsInProgressCount = 0;

                var mutations = this.allTurtles.SelectMany(t =>
                {
                    var mutants = t.TryCreateMutants(input);

                    Interlocked.Add(ref testsCount, mutants.Count * input.Subject.TestMethods.Count);
                    ReportProgress(progress, testsFinishedCount, testsInProgressCount, testsCount);

                    if (mutants.Count == 0)
                    {
                        unmutableMethods.Add(new SingleMutationTestResult(
                                input.Subject.Method,
                                0,
                                "",
                                input.Subject.TestMethods.Select(tm => tm.Method).ToArray(),
                                new MethodRef[0]));
                    }

                    allMutants.AddRange(mutants);
                    return mutants;
                });

                var results = mutations.AsParallel()// .WithDegreeOfParallelism(Environment.ProcessorCount * 4)
                    .Select(mutation =>
                {
                    var failingTests = new List<MethodRef>();
                    var passingTests = new List<MethodRef>();

                    using (mutation)
                    {
                        //on one directory (one mutant), we run the tests one after the other, not in parallel. This is by design.
                        foreach (var test in mutation.Input.Subject.TestMethods)
                        {
                            Interlocked.Increment(ref testsInProgressCount);
                            ReportProgress(progress, testsFinishedCount, testsInProgressCount, testsCount);

                            var testPassed = this.RunTestOnMutation(mutation, test);
                            if (testPassed)
                            {
                                passingTests.Add(test.Method);
                            }
                            else
                            {
                                failingTests.Add(test.Method);
                            }

                            Interlocked.Decrement(ref testsInProgressCount);
                            Interlocked.Increment(ref testsFinishedCount);
                            ReportProgress(progress, testsFinishedCount, testsInProgressCount, testsCount);
                        }
                    }

                    return new SingleMutationTestResult(
                        mutation.Input.Subject.Method,
                        mutation.InstructionIndex,
                        mutation.Description,
                        passingTests,
                        failingTests);
                });

                unmutableMethods.AddRange(results);
                return unmutableMethods;
            }
            finally
            {
                //a second failsafe. We really don't want to leave the copied directories around.
                foreach (var m in allMutants)
                {
                    m.Dispose();
                }
            }
        }

        private bool RunTestOnMutation(Mutation mutation, TestMethodRef test)
        {
            var shadowedTestAssembly = mutation.TestDirectory.GetEquivalentShadowPath(test.Method.Assembly);
            var processInfo = test.Runner.GetProcessInfoToRunTest(mutation.TestDirectory.Shadow, shadowedTestAssembly, test.Method.FullName);

            this.log.DebugFormat(
                "Running test '{0}' for mutation '{1}' in method '{2}'.",
                test.Method.FullName,
                mutation.Description,
                mutation.Input.Subject.Method.FullName);

            var exitCode = this.executableUtils.RunProcessAndWaitForExit(processInfo, mutation.TestDirectory.ShadowId);
            return exitCode == 0;
        }

        private static void ReportProgress(IProgress<Tuple<int, int, int>> progress, int testsFinishedCount, int testsInProgressCount, int testsCount)
        {
            if (progress != null)
            {
                progress.Report(Tuple.Create(testsFinishedCount, testsInProgressCount, testsCount));
            }
        }

        private MethodDefinition GetMethodDef(MethodRef method)
        {
            return this.codeCache.GetAssembly(method.Assembly).GetMethodByFullName(method.FullName);
        }
    }
}
