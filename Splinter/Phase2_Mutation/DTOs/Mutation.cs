#region Copyright & licence

// This file is part of NinjaTurtles.
// 
// NinjaTurtles is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// NinjaTurtles is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with NinjaTurtles.  If not, see <http://www.gnu.org/licenses/>.
// 
// Copyright (C) 2012-14 David Musgrove and others.

#endregion

using System;
using System.IO;
using System.Globalization;
using System.Diagnostics;

using Mono.Cecil;
using Mono.Cecil.Rocks;
using Splinter.Phase2_Mutation.NinjaTurtles;

using Splinter.Utils;
using Splinter.Contracts.DTOs;

namespace Splinter.Phase2_Mutation.DTOs
{
    /// <summary>
    /// An immutable class containing metadata of a single mutation. Directory will be deleted on Dispose.
    /// An equivalent of this class in NinjaTurtles is called MutantMetaData
    /// </summary>
    [DebuggerDisplay("Mutation {Input.Subject.Method.FullName} {Description}")]
    public class Mutation : IDisposable
    {
        public Mutation(string id, DirectoryInfo modelDirectory, TestSubjectMethodRef subject, ShadowDirectory testDirectory, FileInfo mutant, int instructionOffset, string description)
        {
            this.ModelDirectory = modelDirectory;
            this.Subject = subject;
            this.Mutant = mutant;
            this.Description = description;
            this.InstructionOffset = instructionOffset;
            this.TestDirectory = testDirectory;
            this.Id = id;
        }

        /// <summary>
        /// Gets the description of the mutation test being run.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Identifies this mutation in the logs
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets the directory that is being shadowed.
        /// </summary>
        public DirectoryInfo ModelDirectory  { get; private set; }

        /// <summary>
        /// Gets the subject method.
        /// </summary>
        public TestSubjectMethodRef Subject { get; private set; }

        /// <summary>
        /// Gets the location of the assembly containing the mutated method.
        /// </summary>
        public FileInfo Mutant { get; private set; }

        /// <summary>
        /// Gets the index into the mutated method of the modified instruction.
        /// </summary>
        public int InstructionOffset { get; private set; }

        /// <summary>
        /// Gets the directory to which the mutated method was saved.
        /// </summary>
        public ShadowDirectory TestDirectory { get; private set; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            var s = this.TestDirectory;
            if (s != null)
            {
                s.Dispose();
                s = null;
            }
        }
    }
}