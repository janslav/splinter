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
        /// <summary>
        /// Creates the mutants and runs tests on them.
        /// </summary>
        IReadOnlyCollection<SingleMutationTestResult> CreateMutantsAndRunTestsOnThem(
            MutationTestSessionInput input,
            IProgress<Tuple<int, int, int>> progress,
            IMutationTestsOrderingStrategy orderingStrategy,
            bool keepTryingNonfailedTests);
    }

    /// <summary>
    /// The creation of mutated assemblies and running tests against them is driven from here.
    /// </summary>
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

        /// <summary>
        /// Creates the mutants and runs tests on them.
        /// </summary>
        public IReadOnlyCollection<SingleMutationTestResult> CreateMutantsAndRunTestsOnThem(
            MutationTestSessionInput input,
            IProgress<Tuple<int, int, int>> progress,
            IMutationTestsOrderingStrategy orderingStrategy,
            bool keepTryingNonfailedTests)
        {
            var allMutants = new List<Mutation>();

            try
            {
                var allMutationResults = new List<SingleMutationTestResult>();

                int testsCount = 0;
                int testsFinishedCount = 0;
                int testsInProgressCount = 0;

                var mutations = this.allTurtles.SelectMany(t =>
                {
                    var mutants = t.TryCreateMutants(input);

                    ReportMutantsCreated(input, progress, mutants, ref testsCount, testsFinishedCount, testsInProgressCount);

                    if (mutants.Count == 0)
                    {
                        allMutationResults.Add(CreateResultForUnmutableMethod(input));
                    }

                    allMutants.AddRange(mutants);
                    return mutants;
                });

                var mutationRuns = mutations.AsParallel()// .WithDegreeOfParallelism(Environment.ProcessorCount * 4)
                    .Select(mutation =>
                {
                    var failingTests = new List<MethodRef>();
                    var passingTests = new List<MethodRef>();

                    //on one directory (one mutant), we run the tests one after the other, not in parallel. This is by design.
                    foreach (var test in orderingStrategy.OrderTestsForRunning(mutation))
                    {
                        if (!keepTryingNonfailedTests && failingTests.Count > 0)
                        {
                            break;
                        }

                        ReportTestRunStarting(progress, testsCount, testsFinishedCount, ref testsInProgressCount);

                        var testPassed = this.RunTestOnMutation(mutation, test);
                        if (testPassed)
                        {
                            passingTests.Add(test.Method);
                            orderingStrategy.NotifyTestPased(mutation, test);
                        }
                        else
                        {
                            failingTests.Add(test.Method);
                            orderingStrategy.NotifyTestFailed(mutation, test);
                        }

                        ReportTestRunFinished(progress, testsCount, ref testsFinishedCount, ref testsInProgressCount);
                    }

                    return CreateResultObject(mutation, failingTests, passingTests);
                });

                allMutationResults.AddRange(mutationRuns);
                return allMutationResults;
            }
            finally
            {
                foreach (var m in allMutants)
                {
                    m.Dispose();
                }
            }
        }

        private static SingleMutationTestResult CreateResultObject(Mutation mutation, List<MethodRef> failingTests, List<MethodRef> passingTests)
        {
            var notRun = mutation.Input.Subject.TestMethods.Select(tm => tm.Method).Except(passingTests).Except(failingTests);

            return new SingleMutationTestResult(
                mutation.Input.Subject.Method,
                mutation.InstructionIndex,
                mutation.Description,
                passingTests,
                failingTests,
                notRun.ToArray());
        }

        private static SingleMutationTestResult CreateResultForUnmutableMethod(MutationTestSessionInput input)
        {
            return new SingleMutationTestResult(
                input.Subject.Method,
                0,
                "",
                input.Subject.TestMethods.Select(tm => tm.Method).ToArray(),
                new MethodRef[0],
                new MethodRef[0]);
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

            var exitCode = this.executableUtils.RunProcessAndWaitForExit(processInfo, mutation.Id);
            return exitCode == 0;
        }

        private static void ReportTestRunFinished(IProgress<Tuple<int, int, int>> progress, int testsCount, ref int testsFinishedCount, ref int testsInProgressCount)
        {
            if (progress != null)
            {
                Interlocked.Decrement(ref testsInProgressCount);
                Interlocked.Increment(ref testsFinishedCount);
                progress.Report(Tuple.Create(testsFinishedCount, testsInProgressCount, testsCount));
            }
        }

        private static void ReportTestRunStarting(IProgress<Tuple<int, int, int>> progress, int testsCount, int testsFinishedCount, ref int testsInProgressCount)
        {
            if (progress != null)
            {
                Interlocked.Increment(ref testsInProgressCount);
                progress.Report(Tuple.Create(testsFinishedCount, testsInProgressCount, testsCount));
            }
        }

        private static void ReportMutantsCreated(MutationTestSessionInput input, IProgress<Tuple<int, int, int>> progress, IReadOnlyCollection<Mutation> mutants, ref int testsCount, int testsFinishedCount, int testsInProgressCount)
        {
            if (progress != null)
            {
                Interlocked.Add(ref testsCount, mutants.Count * input.Subject.TestMethods.Count);
                progress.Report(Tuple.Create(testsFinishedCount, testsInProgressCount, testsCount));
            }
        }

        private MethodDefinition GetMethodDef(MethodRef method)
        {
            return this.codeCache.GetAssemblyDefinition(method.Assembly).GetMethodByFullName(method.FullName);
        }
    }
}
