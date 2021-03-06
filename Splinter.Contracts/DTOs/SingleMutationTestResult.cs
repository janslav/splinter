﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            int instructionOffset,
            string description,
            IReadOnlyCollection<MethodRef> passingTests,
            IReadOnlyCollection<MethodRef> failingTests,
            IReadOnlyCollection<MethodRef> timeoutedTests,
            IReadOnlyCollection<MethodRef> testsNotRun)
        {
            this.Subject = subject;
            this.InstructionOffset = instructionOffset;
            this.MutationDescription = description;
            this.PassingTests = ImmutableHashSet.CreateRange(passingTests);
            this.FailingTests = ImmutableHashSet.CreateRange(failingTests);
            this.TimeoutedTests = ImmutableHashSet.CreateRange(timeoutedTests);
            this.NotRunTests = ImmutableHashSet.CreateRange(testsNotRun);
        }

        /// <summary>
        /// Gets the subject method.
        /// </summary>
        public MethodRef Subject { get; private set; }

        /// <summary>
        /// Gets the offset of the instruction that has been mutated
        /// </summary>
        public int InstructionOffset { get; private set; }

        /// <summary>
        /// Gets the mutation description.
        /// </summary>>
        public string MutationDescription { get; private set; }

        /// <summary>
        /// Gets the tests that didn't kill this mutation.
        /// </summary>
        public IImmutableSet<MethodRef> PassingTests { get; private set; }

        /// <summary>
        /// Gets the tests that did kill this mutation.
        /// </summary>
        public IImmutableSet<MethodRef> FailingTests { get; private set; }

        /// <summary>
        /// Gets the tests that were not run against this mutation.
        /// </summary>
        public IImmutableSet<MethodRef> NotRunTests { get; private set; }

        /// <summary>
        /// Gets the tests that took too long to run so were cancelled.
        /// </summary>
        public IImmutableSet<MethodRef> TimeoutedTests { get; private set; }
    }
}
