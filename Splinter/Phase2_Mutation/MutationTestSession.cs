﻿using System;
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
        IReadOnlyCollection<SingleMutationTestResult> Run(MutationTestSessionInput input, IProgress<Tuple<int, int>> progress = null);
    }

    public class MutationTestSession : IMutationTestSession
    {
        private readonly ILog log;

        private IReadOnlyCollection<IMethodTurtle> allTurtles = null;

        private readonly ICodeCache codeCache;

        private readonly IWindowsErrorReporting errorReportingSwitch;

        private readonly IExecutableUtils executableUtils;

        public MutationTestSession(ILog log, IUnityContainer container, IExecutableUtils executableUtils, ICodeCache codeCache, IWindowsErrorReporting errorReportingSwitch)
        {
            this.log = log;
            this.codeCache = codeCache;
            this.errorReportingSwitch = errorReportingSwitch;
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

        public IReadOnlyCollection<SingleMutationTestResult> Run(MutationTestSessionInput input, IProgress<Tuple<int, int>> progress)
        {
            using (this.errorReportingSwitch.TurnOffErrorReporting())
            {
                var unmutableMethods = new List<SingleMutationTestResult>();

                int testsCount = 0;
                int testsFinishedCount = 0;

                var mutations = this.allTurtles.SelectMany(t =>
                {
                    var mutants = t.TryCreateMutants(input);

                    if (mutants.Count == 0)
                    {
                        unmutableMethods.Add(new SingleMutationTestResult(
                                input.Subject.Method,
                                0,
                                "",
                                input.Subject.TestMethods.Select(tm => tm.Method).ToArray(),
                                new MethodRef[0]));
                    }

                    return mutants;
                });

                var results = mutations.AsParallel().Select(mutation =>
                {
                    Interlocked.Add(ref testsCount, input.Subject.TestMethods.Count);
                    ReportProgress(progress, testsCount, testsFinishedCount);

                    var failingTests = new List<MethodRef>();
                    var passingTests = new List<MethodRef>();

                    using (mutation)
                    {
                        //on one directory (one mutant), we run the tests one after the other, not in parallel. This is by design.
                        foreach (var test in mutation.Input.Subject.TestMethods)
                        {
                            var shadowedTestAssembly = mutation.TestDirectory.GetEquivalentShadowPath(test.Method.Assembly);
                            var processInfo = test.Runner.GetProcessInfoToRunTest(mutation.TestDirectory.Shadow, shadowedTestAssembly, test.Method.FullName);

                            this.log.DebugFormat(
                                "Running test '{0}' for mutation '{1}' in method '{2}'.",
                                test.Method.FullName,
                                mutation.Description,
                                mutation.Input.Subject.Method.FullName);

                            var exitCode = this.executableUtils.RunProcessAndWaitForExit(processInfo, mutation.TestDirectory.ShadowId);
                            if (exitCode == 0)
                            {
                                passingTests.Add(test.Method);
                            }
                            else
                            {
                                failingTests.Add(test.Method);
                            }

                            Interlocked.Increment(ref testsFinishedCount);
                            ReportProgress(progress, testsCount, testsFinishedCount);
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
        }

        private static void ReportProgress(IProgress<Tuple<int, int>> progress, int testsCount, int testsFinishedCount)
        {
            if (progress != null)
            {
                progress.Report(Tuple.Create(testsFinishedCount, testsCount));
            }
        }

        private MethodDefinition GetMethodDef(MethodRef method)
        {
            return this.codeCache.GetAssembly(method.Assembly).GetMethodByFullName(method.FullName);
        }
    }
}
