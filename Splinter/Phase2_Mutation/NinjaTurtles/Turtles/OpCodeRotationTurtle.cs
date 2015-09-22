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

using System.Collections.Generic;
using System.IO;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

using Splinter.Contracts.DTOs;

namespace Splinter.Phase2_Mutation.NinjaTurtles.Turtles
{
    using Splinter.Utils.Cecil;

    /// <summary>
    /// An abstract base class for implementations of
    /// <see cref="IMethodTurtle" /> that operator by replacing a number of
    /// IL OpCodes with a list of replacements in turn.
    /// </summary>
    /// <remarks>
    /// Classes extending this one only need to set the value of the
    /// <see fref="_opCodes" /> field to an appropriate dictionary of source
    /// and target OpCodes.
    /// </remarks>
    public abstract class OpCodeRotationTurtle : MethodTurtleBase
    {
        public OpCodeRotationTurtle(log4net.ILog log, ICodeCache codeCache)
            : base(log, codeCache)
        {
        }

        /// <summary>
        /// An <see cref="IDictionary{K,V}" /> containing source OpCodes as
        /// keys, and <see cref="IEnumerable{T}" />s of OpCodes as each key's
        /// possible replacements.
        /// </summary>
        protected IDictionary<OpCode, IEnumerable<OpCode>> opCodes;

        /// <summary>
        /// Performs the actual code mutations, returning each with
        /// <c>yield</c> for the calling code to use.
        /// </summary>
        protected override IEnumerable<Mutation> TryToCreateMutations(
            DirectoryInfo modelDirectory,
            TestSubjectMethodRef subject,
            AssemblyDefinition assemblyBeingMutated,
            MethodDefinition method,
            IReadOnlyList<int> originalOffsets,
            IReadOnlyCollection<int> instructionOffsetsToMutate)
        {
            for (int index = 0; index < method.Body.Instructions.Count; index++)
            {
                if (!instructionOffsetsToMutate.Contains(originalOffsets[index]))
                {
                    continue;
                }

                var instruction = method.Body.Instructions[index];
                if (this.opCodes.ContainsKey(instruction.OpCode))
                {
                    if (instruction.IsMeaninglessUnconditionalBranch()) continue;

                    var originalOpCode = instruction.OpCode;

                    foreach (var opCode in this.opCodes[originalOpCode])
                    {
                        instruction.OpCode = opCode;
                        var description = string.Format("{0:x4}: {1} => {2}", originalOffsets[index], originalOpCode.Code, opCode.Code);
                        Mutation mutation = this.SaveMutantToDisk(modelDirectory, subject, assemblyBeingMutated, originalOffsets[index], description);
                        yield return mutation;
                    }

                    instruction.OpCode = originalOpCode;
                }
            }
        }
    }
}
