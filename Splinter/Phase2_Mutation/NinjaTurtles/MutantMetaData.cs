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

namespace Splinter.Phase2_Mutation.NinjaTurtles
{
    /// <summary>
    /// An immutable class containing metadata of a single mutation. Will be deleted on Dispose.
    /// </summary>
    [DebuggerDisplay("MutantMetaData {Input.Subject.Method.FullName} {Description}")]
    public class MutantMetaData : IDisposable
    {
        public MutantMetaData(MutationTestSessionInput input, ShadowDirectory testDirectory, FileInfo mutant, int ilIndex, string description)
        {
            this.Input = input;
            this.Mutant = mutant;
            this.Description = description;
            this.ILIndex = ilIndex;
            this.TestDirectory = testDirectory;
        }

        /// <summary>
        /// Gets the description of the mutation test being run.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets the location of the assembly containing the model method.
        /// </summary>
        public MutationTestSessionInput Input { get; private set; }

        /// <summary>
        /// Gets the location of the assembly containing the mutated method.
        /// </summary>
        public FileInfo Mutant { get; private set; }

        /// <summary>
        /// Gets the index into the mutated method of the modified instruction.
        /// </summary>
        public int ILIndex { get; private set; }

        /// <summary>
        /// Gets the directory to which the mutated method was saved.
        /// </summary>
        public ShadowDirectory TestDirectory { get; private set; }

        //internal string GetOriginalSourceCode(int index)
        //{
        //    var sequencePoint = MethodDefinition.GetCurrentSequencePoint(index);
        //    string result = "";
        //    if (!Module.SourceFiles.ContainsKey(sequencePoint.Document.Url))
        //    {
        //        return "";
        //    }
        //    string[] sourceCode = Module.SourceFiles[sequencePoint.Document.Url];
        //    int upperBound = Math.Min(sequencePoint.EndLine + 2, sourceCode.Length);
        //    for (int line = Math.Max(sequencePoint.StartLine - 2, 1); line <= upperBound; line++)
        //    {
        //        string sourceLine = sourceCode[line - 1].Replace("\t", "    ");
        //        result += line.ToString(CultureInfo.InvariantCulture)
        //            .PadLeft(4, ' ') + ": " + sourceLine.TrimEnd(' ', '\t');
        //        if (line < upperBound) result += Environment.NewLine;
        //    }
        //    return result;
        //}

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