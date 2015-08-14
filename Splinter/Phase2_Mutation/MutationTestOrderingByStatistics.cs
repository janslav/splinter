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
using Splinter.Phase2_Mutation.DTOs;

namespace Splinter.Phase2_Mutation
{
    /// <summary>
    /// Keeps statistics about already run test to optimize the order in which to run the future ones.
    /// The idea of this strategy is such that tests that have killed the most mutants so far should be the first to try out.
    /// </summary>
    public class MutationTestOrderingByStatistics : IMutationTestsOrderingStrategy
    {
        private readonly ConcurrentDictionary<MethodRef, int> testsByScore = new ConcurrentDictionary<MethodRef, int>();

        /// <summary>
        /// Returns the tests that belong to the specified mutation in the best orders for running.
        /// </summary>
        public IEnumerable<TestMethodRef> OrderTestsForRunning(Mutation mutation)
        {
            //the score can change during the enumeration so we recalculate it for every yield
            var list = mutation.Input.Subject.TestMethods.ToList();

            while (list.Count > 0)
            {
                var top = list.Max(i =>
                {
                    int score;
                    testsByScore.TryGetValue(i.Method, out score);
                    return new TestScoreComparable { score = score, method = i };
                });

                yield return top.method;

                list.Remove(top.method);
            }
        }

        private struct TestScoreComparable : IComparable<TestScoreComparable>
        {
            internal int score;

            internal TestMethodRef method;

            public int CompareTo(TestScoreComparable other)
            {
                return this.score.CompareTo(other.score);
            }
        }

        /// <summary>
        /// Notifies the statistics object that a test failed, when run against the specified mutation
        /// </summary>
        public void NotifyTestFailed(Mutation mutation, TestMethodRef test)
        {
            //failed test is a good thing. Means the mutant got killed.
            //for that the test gets a +2 score.
            this.testsByScore.AddOrUpdate(test.Method, 2, (_, score) => score + 2);
        }

        /// <summary>
        /// Notifies the statistics object that a test pased, when run against the specified mutation
        /// </summary>
        public void NotifyTestPassed(Mutation mutation, TestMethodRef test)
        {
            //passed test is not good news but it's not that bad either.
            //for that the test gets a -1 score.
            this.testsByScore.AddOrUpdate(test.Method, -1, (_, score) => score - 1);
        }

        /// <summary>
        /// Notifies this object that a test timed out, when run against the specified mutation. 
        /// This probably means the test caused an infinite loop or somethin similarly juicy.
        /// </summary>
        public void NotifyTestTimedOut(Mutation mutation, TestMethodRef test)
        {
            //a timed out test can be considered "failed", more or less, for mutation killing purposes, but it's bad for performance.
            //for that the test gets a -10 score.
            this.testsByScore.AddOrUpdate(test.Method, -10, (_, score) => score - 10);
        }
    }
}
