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
    /// <summary>
    /// Represents the input for mutations creation.
    /// </summary>
    [DebuggerDisplay("MutationTestSessionInput {Subject.Method.FullName} Tests: {Subject.TestMethods.Count}")]
    public class MutationTestSessionInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MutationTestSessionInput"/> class.
        /// </summary>
        public MutationTestSessionInput(DirectoryInfo modelDirectory, TestSubjectMethodRef subject)
        {
            this.ModelDirectory = modelDirectory;
            this.Subject = subject;
        }

        /// <summary>
        /// Gets the model directory.
        /// </summary>
        public DirectoryInfo ModelDirectory { get; private set; }

        /// <summary>
        /// Gets the subject.
        /// </summary>
        public TestSubjectMethodRef Subject { get; private set; }
    }
}
