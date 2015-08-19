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
using Splinter.Utils;
using Splinter.Utils.Cecil;
using Splinter.Phase0_Boot;

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
            DirectoryInfo modelDirectory,
            IEnumerable<TestSubjectMethodRef> subjects,
            IProgress<Tuple<int, int, int>> progress,
            IMutationTestsOrderingStrategy orderingStrategy,
            bool keepTryingNonFailedTests);
    }

    /// <summary>
    /// The creation of mutated assemblies and running tests against them is driven from here.
    /// </summary>
    public class MutationTestSession : IMutationTestSession
    {
        private readonly ILog log;

        private readonly ICodeCache codeCache;

        private readonly IExecutableUtils executableUtils;

        private readonly SplinterConfigurationSection configuration;

        private IReadOnlyCollection<IMethodTurtle> allTurtles = null;

        public MutationTestSession(
            ILog log,
            IUnityContainer container,
            IExecutableUtils executableUtils,
            ICodeCache codeCache,
            SplinterConfigurationSection configuration)
        {
            this.log = log;
            this.codeCache = codeCache;
            this.executableUtils = executableUtils;
            this.configuration = configuration;

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
            DirectoryInfo modelDirectory,
            IEnumerable<TestSubjectMethodRef> subjects,
            IProgress<Tuple<int, int, int>> progress,
            IMutationTestsOrderingStrategy orderingStrategy,
            bool keepTryingNonFailedTests)
        {
            var allMutants = new List<Mutation>();

            try
            {
                var allMutationResults = new List<SingleMutationTestResult>();

                int testsCount = 0;
                int testsFinishedCount = 0;
                int testsInProgressCount = 0;

                var mutations = subjects.SelectMany(subject =>
                    this.allTurtles.SelectMany(t =>
                    {
                        var mutants = t.TryCreateMutants(modelDirectory, subject);

                        if (mutants.Count == 0)
                        {
                            allMutationResults.Add(CreateResultForUnmutableMethod(subject));
                        }
                        else
                        {
                            ReportMutantsCreated(subject, progress, mutants, ref testsCount, testsFinishedCount, testsInProgressCount);
                            allMutants.AddRange(mutants);
                        }

                        return mutants;
                    }));

                var mutationRuns = mutations.AsParallel()// .WithDegreeOfParallelism(Environment.ProcessorCount * 4)
                    .Select(mutation =>
                {
                    var failingTests = new List<MethodRef>();
                    var passingTests = new List<MethodRef>();
                    var timeoutedTests = new List<MethodRef>();

                    var testsCoveringThisMutation = this.GetTestsCoveringThisMutation(mutation);

                    //on one directory (one mutant), we run the tests one after the other, not in parallel. This is by design.
                    foreach (var test in orderingStrategy.OrderTestsForRunning(testsCoveringThisMutation))
                    {
                        if (!keepTryingNonFailedTests && failingTests.Count > 0)
                        {
                            var testsNotUsedAgainstThisMutant = mutation.Subject.AllTestMethods.Count()
                                - (failingTests.Count + passingTests.Count + timeoutedTests.Count);

                            Interlocked.Add(ref testsCount, -testsNotUsedAgainstThisMutant);

                            break;
                        }

                        ReportTestRunStarting(progress, testsCount, testsFinishedCount, ref testsInProgressCount);

                        var timer = Stopwatch.StartNew();
                        var testRunResult = this.RunTestOnMutation(mutation, test);
                        timer.Stop();

                        if (testRunResult.TimedOut)
                        {
                            timeoutedTests.Add(test.Method);
                            orderingStrategy.NotifyTestTimedOut(mutation, test, timer.Elapsed);
                        }
                        else if (testRunResult.ExitCode == 0)
                        {
                            passingTests.Add(test.Method);
                            orderingStrategy.NotifyTestPassed(mutation, test, timer.Elapsed);
                        }
                        else
                        {
                            failingTests.Add(test.Method);
                            orderingStrategy.NotifyTestFailed(mutation, test, timer.Elapsed);
                        }

                        ReportTestRunFinished(progress, testsCount, ref testsFinishedCount, ref testsInProgressCount);
                    }

                    return CreateResultObject(mutation, failingTests, passingTests, timeoutedTests);
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

        private List<TestMethodRef> GetTestsCoveringThisMutation(Mutation mutation)
        {
            var assembly = this.codeCache.GetAssemblyDefinition(mutation.Subject.Method.Assembly);
            var spOfMutation = assembly.GetNearestSequencePointInstructionOffset(mutation.Subject.Method.FullName, mutation.InstructionOffset);
            var testsCoveringThisSequencePoint = mutation.Subject.TestMethodsBySequencePointInstructionOffset
                .Where(t =>
                {
                    var spOfTest = assembly.GetNearestSequencePointInstructionOffset(mutation.Subject.Method.FullName, t.Item1);
                    return spOfMutation == spOfTest;
                }).SelectMany(t => t.Item2).Distinct().ToList();
            return testsCoveringThisSequencePoint;
        }

        private static SingleMutationTestResult CreateResultObject(Mutation mutation, List<MethodRef> failingTests, List<MethodRef> passingTests, List<MethodRef> timeoutedTests)
        {
            var notRun = mutation.Subject.AllTestMethods.Select(tm => tm.Method).Except(passingTests).Except(failingTests).Except(timeoutedTests);

            return new SingleMutationTestResult(
                mutation.Subject.Method,
                mutation.InstructionOffset,
                mutation.Description,
                passingTests: passingTests,
                failingTests: failingTests,
                timeoutedTests: timeoutedTests,
                testsNotRun: notRun.ToArray());
        }

        private static SingleMutationTestResult CreateResultForUnmutableMethod(TestSubjectMethodRef subject)
        {
            return new SingleMutationTestResult(
                subject.Method,
                0,
                "",
                passingTests: subject.AllTestMethods.Select(tm => tm.Method).ToArray(),
                failingTests: new MethodRef[0],
                timeoutedTests: new MethodRef[0],
                testsNotRun: new MethodRef[0]);
        }

        private ProcessRunResult RunTestOnMutation(Mutation mutation, TestMethodRef test)
        {
            var shadowedTestAssembly = mutation.TestDirectory.GetEquivalentShadowPath(test.Method.Assembly);
            var processInfo = test.Runner.GetProcessInfoToRunTest(mutation.TestDirectory.Shadow, shadowedTestAssembly, test.Method.FullName);

            this.log.DebugFormat(
                "{0}: Running test '{1}' for mutation '{2}' in method '{3}'.",
                mutation.Id,
                test.Method.FullName,
                mutation.Description,
                mutation.Subject.Method.FullName);

            var timeout = TimeSpan.FromSeconds(this.configuration.MaxMutationRunningTimeConstantInSeconds) +
                TimeSpan.FromTicks(this.configuration.MaxMutationRunningTimeFactor * test.LongestRunningTime.Ticks);

            var result = this.executableUtils.RunProcessAndWaitForExit(processInfo, mutation.Id, timeout);
            return result;
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

        private static void ReportMutantsCreated(TestSubjectMethodRef subject, IProgress<Tuple<int, int, int>> progress, IReadOnlyCollection<Mutation> mutants, ref int testsCount, int testsFinishedCount, int testsInProgressCount)
        {
            if (progress != null)
            {
                Interlocked.Add(ref testsCount, mutants.Count * subject.AllTestMethods.Count);
                progress.Report(Tuple.Create(testsFinishedCount, testsInProgressCount, testsCount));
            }
        }

        private MethodDefinition GetMethodDef(MethodRef method)
        {
            return this.codeCache.GetAssemblyDefinition(method.Assembly).GetMethodByFullName(method.FullName);
        }
    }
}
