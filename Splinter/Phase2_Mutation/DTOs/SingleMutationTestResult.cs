using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Splinter.Contracts;
using Splinter.Contracts.DTOs;

namespace Splinter.Phase2_Mutation.DTOs
{

    public class SingleMutationTestResult
    {
        public SingleMutationTestResult(
            MethodRef subject,
            int instructionIndex,
            string description,
            IReadOnlyCollection<MethodRef> passingTests,
            IReadOnlyCollection<MethodRef> failingTests)
        {
            this.Subject = subject;
            this.InstructionIndex = instructionIndex;
            this.Description = description;
            this.PassingTests = passingTests;
            this.FailingTests = failingTests;
        }

        public MethodRef Subject { get; private set; }

        public int InstructionIndex { get; private set; }

        public string Description { get; private set; }

        public IReadOnlyCollection<MethodRef> PassingTests { get; private set; }

        public IReadOnlyCollection<MethodRef> FailingTests { get; private set; }
    }
}
