using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

using Splinter;
using Splinter.Contracts;
using Splinter.Contracts.DTOs;

namespace Splinter.Contracts
{
    /// <summary>
    /// Optimizes the order in which to run tests
    /// </summary>
    public interface IMutationTestsOrderingStrategy : IPlugin
    {
        /// <summary>
        /// Returns the tests that belong to the specified mutation in the best orders for running.
        /// </summary>
        IEnumerable<TestMethodRef> OrderTestsForRunning(IEnumerable<TestMethodRef> testMethods);

        /// <summary>
        /// Notifies this object that a test failed, when run against the specified mutation. I.e. the mutant was killed.
        /// </summary>
        void NotifyTestFailed(Mutation mutation, TestMethodRef test);

        /// <summary>
        /// Notifies this object that a test passed, when run against the specified mutation. I.e. the mutant was not killed.
        /// </summary>
        void NotifyTestPassed(Mutation mutation, TestMethodRef test);

        /// <summary>
        /// Notifies this object that a test timed out, when run against the specified mutation. 
        /// This probably means the test caused an infinite loop or somethin similarly juicy.
        /// </summary>
        void NotifyTestTimedOut(Mutation mutation, TestMethodRef test);
    }
}
