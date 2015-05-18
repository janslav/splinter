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
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;

namespace Splinter.Utils.Cecil
{
    /// <summary>
    /// Class representing the main module of a .NET assembly.
    /// Code mostly taken from NinjaTurtles class Module
    /// </summary>
    public interface IAssemblyCode
    {
        FileInfo AssemblyLocation { get; }

        AssemblyDefinition AssemblyDefinition { get; }

        MethodDefinition GetMethodByFullName(string fullName);

        void LoadDebugInformation();

        SequencePoint GetSequencePoint(string methodFullName, int instructionIndex);
        
        SourceFile GetSourceFile(Document reference);
    }

    public class AssemblyCode : IAssemblyCode
    {
        private static readonly object locker = new object();

        private readonly ConcurrentDictionary<string, MethodDefinition> methodsByFullName = new ConcurrentDictionary<string, MethodDefinition>();

        private readonly ConcurrentDictionary<Tuple<string, int>, SequencePoint> sequencePointsByInstruction =
            new ConcurrentDictionary<Tuple<string, int>, SequencePoint>();

        private readonly ConcurrentDictionary<byte[], SourceFile> sourceFilesByHash =
            new ConcurrentDictionary<byte[], SourceFile>();

        public AssemblyCode(FileInfo assemblyLocation)
        {
            this.AssemblyLocation = assemblyLocation;
            this.AssemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyLocation.FullName);
        }

        /// <summary>
        /// Gets the location on disk of the assembly.
        /// </summary>
        public FileInfo AssemblyLocation { get; private set; }

        /// <summary>
        /// Gets the <see cref="AssemblyDefinition" />.
        /// </summary>
        public AssemblyDefinition AssemblyDefinition { get; private set; }

        public MethodDefinition GetMethodByFullName(string fullName)
        {
            return this.methodsByFullName.GetOrAdd(
                fullName,
                n =>
                {
                    lock (locker)
                    {
                        return this.AssemblyDefinition.Modules
                            .SelectMany(m => m.Types)
                            .SelectMany(t => ListNestedTypesRecursively(t))
                            .SelectMany(t => t.Methods)
                            .Single(m => m.FullName.Equals(n));
                    }
                });
        }

        private IEnumerable<TypeDefinition> ListNestedTypesRecursively(TypeDefinition t)
        {
            if (t.NestedTypes.Count > 0)
            {
                var c = t.NestedTypes.ToList();
                c.Add(t);
                return c;
            }
            else
            {
                return new[] { t };
            }
        }

        public void LoadDebugInformation()
        {
            foreach (var module in this.AssemblyDefinition.Modules)
            {
                if (!module.HasSymbols)
                {
                    module.ReadSymbols();
                }
            }
        }

        public SequencePoint GetSequencePoint(string methodFullName, int instructionIndex)
        {
            this.LoadDebugInformation();

            return this.sequencePointsByInstruction.GetOrAdd(
                Tuple.Create(methodFullName, instructionIndex),
                t =>
                {
                    var methodRef = this.GetMethodByFullName(t.Item1);
                    var sp = CalculateNearestSequencePoint(methodRef, t.Item2);
                    return sp;
                });
        }

        internal static SequencePoint CalculateNearestSequencePoint(MethodDefinition method, int index)
        {
            var instruction = method.Body.Instructions[index];
            while ((instruction.SequencePoint == null
                    || instruction.SequencePoint.StartLine == 0xfeefee) && index > 0)
            {
                index--;
                instruction = method.Body.Instructions[index];
            }

            var sequencePoint = instruction.SequencePoint;
            return sequencePoint;
        }

        public SourceFile GetSourceFile(Document reference)
        {
            return this.sourceFilesByHash.GetOrAdd(
                reference.Hash,
                _ => new SourceFile(reference));
        }
    }
}
