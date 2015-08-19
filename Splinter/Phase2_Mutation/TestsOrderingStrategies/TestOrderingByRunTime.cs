using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

using Splinter;
using Splinter.Contracts;
using Splinter.Contracts.DTOs;
using Splinter.Phase2_Mutation.NinjaTurtles;
using Splinter.Phase2_Mutation.NinjaTurtles.Turtles;
using Splinter.Utils.Cecil;

namespace Splinter.Phase2_Mutation.TestsOrderingStrategies
{
    /// <summary>
    /// Creates an instance of the "ByRunTime" IMutationTestsOrderingStrategy
    /// </summary>
    public class TestOrderingByRunTimePluginFactory : IPluginFactory<IMutationTestsOrderingStrategy>
    {
        public IMutationTestsOrderingStrategy GetPlugin(log4net.ILog log)
        {
            return new TestOrderingByRunTime();
        }

        public string Name
        {
            get { return "ByRunTime"; }
        }
    }

    /// <summary>
    /// Orders tests by the average time they ran so far, favoring the fastest ones.
    /// </summary>
    public class TestOrderingByRunTime : IMutationTestsOrderingStrategy
    {
        private readonly ConcurrentDictionary<MethodRef, Tuple<int, long>> testsByScore = new ConcurrentDictionary<MethodRef, Tuple<int, long>>();

        /// <summary>
        /// Returns the tests that belong to the specified mutation in the best orders for running.
        /// </summary>
        public IEnumerable<TestMethodRef> OrderTestsForRunning(IEnumerable<TestMethodRef> testMethods)
        {
            //the score can change during the enumeration so we recalculate it for every yield
            var list = testMethods.ToList();

            while (list.Count > 0)
            {
                var top = list.Min(i =>
                {
                    Tuple<int, long> tuple;
                    if (testsByScore.TryGetValue(i.Method, out tuple))
                    {
                        return new TestScoreComparable { averageTicks = tuple.Item2 / tuple.Item1, method = i };
                    }
                    else
                    {
                        return new TestScoreComparable { averageTicks = long.MaxValue, method = i };
                    }
                });

                yield return top.method;

                list.Remove(top.method);
            }
        }

        private struct TestScoreComparable : IComparable<TestScoreComparable>
        {
            internal long averageTicks;

            internal TestMethodRef method;

            public int CompareTo(TestScoreComparable other)
            {
                return this.averageTicks.CompareTo(other.averageTicks);
            }
        }

        /// <summary>
        /// Notifies the statistics object that a test failed, when run against the specified mutation
        /// </summary>
        public void NotifyTestFailed(Mutation mutation, TestMethodRef test, TimeSpan testRunTime)
        {
            AppendTestRunTime(test, testRunTime);
        }

        /// <summary>
        /// Notifies the statistics object that a test pased, when run against the specified mutation
        /// </summary>
        public void NotifyTestPassed(Mutation mutation, TestMethodRef test, TimeSpan testRunTime)
        {
            AppendTestRunTime(test, testRunTime);
        }

        /// <summary>
        /// Notifies this object that a test timed out, when run against the specified mutation. 
        /// This probably means the test caused an infinite loop or somethin similarly juicy.
        /// </summary>
        public void NotifyTestTimedOut(Mutation mutation, TestMethodRef test, TimeSpan testRunTime)
        {
            AppendTestRunTime(test, testRunTime);
        }

        private void AppendTestRunTime(TestMethodRef test, TimeSpan testRunTime)
        {
            this.testsByScore.AddOrUpdate(
                test.Method,
                Tuple.Create(1, testRunTime.Ticks),
                (_, tuple) => Tuple.Create(tuple.Item1 + 1, tuple.Item2 + testRunTime.Ticks));
        }

        #region IPlugin implementation
        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get { return "TestOrderingByRunTime"; }
        }

        /// <summary>
        /// Sets up the command line options.
        /// </summary>
        /// <param name="options"></param>
        public void SetupCommandLineOptions(Mono.Options.OptionSet options)
        {
        }

        /// <summary>
        /// Returns true if the plugin is available, i.e. has its binaries installed, registered, etc.
        /// </summary>
        /// <param name="unavailableMessage"></param>
        /// <returns></returns>
        public bool IsReady(out string unavailableMessage)
        {
            unavailableMessage = null;
            return true;
        }
        #endregion
    }
}
