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
    /// Represents a row (SequencePoint) within a test-subject method, with a list of tests that cover it.
    /// </summary>
    [DebuggerDisplay("TestSubjectMethod {Method.FullName} Tests: {TestMethods.Count}")]
    public class TestSubjectMethodRef
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestSubjectMethodRef"/> class.
        /// </summary>
        public TestSubjectMethodRef(
            MethodRef method,
            IReadOnlyCollection<Tuple<int, IReadOnlyCollection<TestMethodRef>>> testMethodsBySequencePointInstructionIndex,
            IReadOnlyCollection<TestMethodRef> allTestMethods)
        {
            this.Method = method;
            this.TestMethodsBySequencePointInstructionIndex = ImmutableDictionary.CreateRange(
                testMethodsBySequencePointInstructionIndex.Select(kvp =>
                    new KeyValuePair<int, IImmutableSet<TestMethodRef>>(kvp.Item1, ImmutableHashSet.CreateRange(kvp.Item2))));
            this.AllTestMethods = ImmutableHashSet.CreateRange(allTestMethods);
        }

        /// <summary>
        /// Gets the method name and location.
        /// </summary>
        public MethodRef Method { get; private set; }

        /// <summary>
        /// Gets the mapping of test methods to sequence points within this subject method.
        /// The sequence points are represented by the ordinal number of their first instruction.
        /// </summary>
        public IImmutableDictionary<int, IImmutableSet<TestMethodRef>> TestMethodsBySequencePointInstructionIndex { get; private set; }

        /// <summary>
        /// Gets a collection of the test methods.
        /// </summary>
        public IImmutableSet<TestMethodRef> AllTestMethods { get; private set; }
    }
}
