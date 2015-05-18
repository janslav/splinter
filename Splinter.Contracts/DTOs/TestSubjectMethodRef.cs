using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Splinter.Contracts.DTOs
{
    /// <summary>
    /// Represents a test-subject method, with a list of tests that cover it.
    /// </summary>
    [DebuggerDisplay("TestSubjectMethod {Method.FullName} Tests: {TestMethods.Count}")]
    public class TestSubjectMethodRef
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestSubjectMethodRef"/> class.
        /// </summary>
        public TestSubjectMethodRef(MethodRef method, IImmutableSet<TestMethodRef> testMethods)
        {
            this.Method = method;
            this.TestMethods = testMethods;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestSubjectMethodRef"/> class.
        /// </summary>
        public TestSubjectMethodRef(MethodRef method, IEnumerable<TestMethodRef> testMethods)
        {
            this.Method = method;
            this.TestMethods = ImmutableHashSet.CreateRange(testMethods);
        }

        /// <summary>
        /// Gets the method name and location.
        /// </summary>
        public MethodRef Method { get; private set; }

        /// <summary>
        /// Gets a collection of the test methods.
        /// </summary>
        public IImmutableSet<TestMethodRef> TestMethods { get; private set; }
    }
}
