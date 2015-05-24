using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Splinter.Contracts;
using Splinter.Contracts.DTOs;

namespace Splinter.Contracts.DTOs
{
    /// <summary>
    /// Describes the result of test runs on top of a single mutation
    /// </summary>
    public class SingleMutationTestResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SingleMutationTestResult"/> class.
        /// </summary>
        public SingleMutationTestResult(
            MethodRef subject,
            int instructionIndex,
            string description,
            IReadOnlyCollection<MethodRef> passingTests,
            IReadOnlyCollection<MethodRef> failingTests,
            IReadOnlyCollection<MethodRef> testsNotRun)
        {
            this.Subject = subject;
            this.InstructionIndex = instructionIndex;
            this.MutationDescription = description;
            this.PassingTests = passingTests;
            this.FailingTests = failingTests;
            this.NotRunTests = testsNotRun;
        }

        /// <summary>
        /// Gets the subject method.
        /// </summary>
        public MethodRef Subject { get; private set; }

        /// <summary>
        /// Gets the index of the instruction.
        /// </summary>
        public int InstructionIndex { get; private set; }

        /// <summary>
        /// Gets the mutation description.
        /// </summary>>
        public string MutationDescription { get; private set; }

        /// <summary>
        /// Gets the tests that didn't kill this mutation.
        /// </summary>
        public IReadOnlyCollection<MethodRef> PassingTests { get; private set; }

        /// <summary>
        /// Gets the tests that did kill this mutation.
        /// </summary>
        public IReadOnlyCollection<MethodRef> FailingTests { get; private set; }

        /// <summary>
        /// Gets the tests that were not run against this mutation.
        /// </summary>
        public IReadOnlyCollection<MethodRef> NotRunTests { get; private set; }
    }
}
