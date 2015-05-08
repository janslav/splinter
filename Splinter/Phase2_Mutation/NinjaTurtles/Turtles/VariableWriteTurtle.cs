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

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Mono.Cecil;
using Mono.Cecil.Cil;

using Splinter.Phase2_Mutation.DTOs;

namespace Splinter.Phase2_Mutation.NinjaTurtles.Turtles
{
    /// <summary>
    /// An implementation of <see cref="IMethodTurtle" /> that identifies local
    /// variables of the same type, and permutes any assignments to them. For
    /// example, if two <see cref="Int32" /> variables <c>a</c> and <c>b</c>
    /// exist, then an assignment to <c>a</c> will be replaced by one to
    /// <c>b</c>, and vice versa.
    /// </summary>
    public class VariableWriteTurtle : MethodTurtleBase
    {
        /// <summary>
        /// Gets a description of the current turtle.
        /// </summary>
        public override string Description
        {
            get { return "Replacing writes to local variables and fields."; }
        }

        /// <summary>
        /// Performs the actual code mutations, returning each with
        /// <c>yield</c> for the calling code to use.
        /// </summary>
        protected override IEnumerable<Mutation> TryCreateMutants(MutationTestSessionInput input, AssemblyDefinition assemblyBeingMutated, MethodDefinition method, int[] originalOffsets)
        {
            var variablesByType = GroupVariablesByType(method);
            PopulateOperandsInVariables(method, variablesByType);

            foreach (var keyValuePair in variablesByType.Where(kv => kv.Value.Count > 1))
            {
                var variables = keyValuePair.Value.ToList();
                for (int index = 0; index < method.Body.Instructions.Count; index++)
                {
                    var instruction = method.Body.Instructions[index];
                    if (instruction.IsPartOfCompilerGeneratedDispose()) continue;

                    int oldIndex = -1;
                    if (instruction.OpCode == OpCodes.Stloc)
                    {
                        int variableIndex = ((VariableReference)instruction.Operand).Index;
                        oldIndex = variables.FindIndex(v => v.Type == VariableType.Local && v.Index == variableIndex);
                    }
                    if (instruction.OpCode == OpCodes.Stfld)
                    {
                        string fieldName = ((FieldReference)instruction.Operand).Name;
                        oldIndex = variables.FindIndex(v => v.Type == VariableType.Field && v.Name == fieldName);
                    }

                    if (oldIndex < 0) continue;

                    // Skip if could be initialising to zero, as in reality
                    // this has already been done by the CLR.
                    if (instruction.Previous.OpCode == OpCodes.Ldc_I4
                        && (int)instruction.Previous.Operand == 0)
                    {
                        continue;
                    }

                    OpCode originalOpCode = instruction.OpCode;
                    object originalOperand = instruction.Operand;
                    var originalVariable = variables[oldIndex];

                    for (int newIndex = 0; newIndex < variables.Count; newIndex++)
                    {
                        if (newIndex == oldIndex) continue;
                        var variable = variables[newIndex];
                        if (variable.Operand == null) continue;

                        instruction.OpCode = variable.GetWriteOpCode();
                        instruction.Operand = variable.Operand;

                        var description =
                            string.Format(
                                "{0:x4}: write substitution {1}.{2} => {1}.{3}",
                                originalOffsets[index],
                                keyValuePair.Key.Name,
                                originalVariable.Name,
                                variable.Name);

                        yield return this.SaveMutantToDisk(input, assemblyBeingMutated, index, description);

                    }
                    instruction.OpCode = originalOpCode;
                    instruction.Operand = originalOperand;
                }
            }
        }

        private static IDictionary<TypeReference, IList<Variable>> GroupVariablesByType(MethodDefinition method)
        {
            IDictionary<TypeReference, IList<Variable>> variables = new Dictionary<TypeReference, IList<Variable>>();
            foreach (var variable in method.Body.Variables)
            {
                var type = variable.VariableType;
                if (!variables.ContainsKey(type))
                {
                    variables.Add(type, new List<Variable>());
                }
                variables[type].Add(new Variable(VariableType.Local, variable.Index, variable.Name));
            }
            foreach (var field in method.DeclaringType.Fields)
            {
                if (field.Name == "<>1__state") continue;
                var type = field.FieldType;
                if (!variables.ContainsKey(type))
                {
                    variables.Add(type, new List<Variable>());
                }
                variables[type].Add(new Variable(VariableType.Field, -1, field.Name));
            }
            return variables;
        }

        private static void PopulateOperandsInVariables(MethodDefinition method, IDictionary<TypeReference, IList<Variable>> variables)
        {
            foreach (var instruction in method.Body.Instructions)
            {
                if (instruction.OpCode == OpCodes.Ldloc)
                {
                    var variableDefinition = (VariableReference)instruction.Operand;
                    int index = variableDefinition.Index;
                    if (!variables.ContainsKey(variableDefinition.VariableType)) continue;
                    var variable =
                        variables[variableDefinition.VariableType]
                            .SingleOrDefault(v => v.Type == VariableType.Local && v.Index == index);
                    if (variable != null)
                    {
                        variable.Operand = instruction.Operand;
                    }
                }
                if (instruction.OpCode == OpCodes.Ldfld)
                {
                    var fieldDefinition = (FieldReference)instruction.Operand;
                    string name = fieldDefinition.Name;
                    if (!variables.ContainsKey(fieldDefinition.FieldType)) continue;
                    var variable =
                        variables[fieldDefinition.FieldType]
                            .SingleOrDefault(v => v.Type == VariableType.Field && v.Name == name);
                    if (variable != null)
                    {
                        variable.Operand = instruction.Operand;
                    }
                }
            }
        }
    }
}
