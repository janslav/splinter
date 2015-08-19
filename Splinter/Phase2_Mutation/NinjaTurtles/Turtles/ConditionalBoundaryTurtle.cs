﻿#region Copyright & licence

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
    /// <summary>
    /// An implementation of <see cref="IMethodTurtle"/> that changes
    /// whether or not equality is included in comparison operators, so
    /// for example <see cref="OpCodes.Clt" /> is replaced by a combination
    /// of <see cref="OpCodes.Cgt" /> and a comparison with zero to give the
    /// effect of a &lt;= operator.
    /// </summary>
    public class ConditionalBoundaryTurtle : MethodTurtleBase
    {
        public ConditionalBoundaryTurtle(log4net.ILog log)
            : base(log)
        {
        }

        /// <summary>
        /// Gets a description of the current turtle.
        /// </summary>
        public override string Description
        {
            get { return "Toggling inclusivity of comparison operators <, <=, > and >=."; }
        }

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
                if (instruction.OpCode == OpCodes.Clt
                    || instruction.OpCode == OpCodes.Cgt)
                {
                    var originalCode = instruction.OpCode.Code;

                    var loadZero = method.Body.GetILProcessor().Create(OpCodes.Ldc_I4_0);
                    var compareEqual = method.Body.GetILProcessor().Create(OpCodes.Ceq);

                    method.Body.Instructions.Insert(index + 1, compareEqual);
                    method.Body.Instructions.Insert(index + 1, loadZero);

                    instruction.OpCode = instruction.OpCode == OpCodes.Clt ? OpCodes.Cgt : OpCodes.Clt;

                    var description = string.Format("{0:x4}: {1} => not {2}", originalOffsets[index], originalCode, instruction.OpCode.Code);
                    yield return this.SaveMutantToDisk(modelDirectory, subject, assemblyBeingMutated, originalOffsets[index], description);

                    instruction.OpCode = instruction.OpCode == OpCodes.Clt ? OpCodes.Cgt : OpCodes.Clt;

                    method.Body.Instructions.Remove(compareEqual);
                    method.Body.Instructions.Remove(loadZero);
                }
            }
        }
    }
}