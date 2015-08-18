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
using System.Reflection;

namespace Splinter.Utils.Cecil
{
    /// <summary>
    /// Class representinga .NET assembly.
    /// </summary>
    public interface IAssemblyCode
    {
        /// <summary>
        /// Gets the location of the assembly on disk .
        /// </summary>
        FileInfo AssemblyLocation { get; }

        /// <summary>
        /// Gets the <see cref="AssemblyDefinition" />.
        /// </summary>
        AssemblyDefinition AssemblyDefinition { get; }

        /// <summary>
        /// Gets a method definition by its full name.
        /// </summary>
        MethodDefinition GetMethodByFullName(string fullName);

        /// <summary>
        /// Gets a method definition by its class full name and method name.
        /// </summary>
        MethodDefinition GetMethodByClassAndMethodName(string classFullName, string methodName);

        /// <summary>
        /// Loads debug information.
        /// </summary>
        void LoadDebugInformation();

        /// <summary>
        /// Gets the sequence point (line of code) of the specified instruction.
        /// </summary>
        SequencePoint GetNearestSequencePoint(string methodFullName, int instructionOffset);

        /// <summary>
        /// Gets the offset of the first instruction of the nearest sequence point (line of code) of the specified instruction.
        /// </summary>
        int GetNearestSequencePointInstructionOffset(string methodFullName, int instructionOffset);

        /// <summary>
        /// Gets the source file loaded using the specified Document instance.
        /// </summary>
        SourceFile GetSourceFile(Document reference);
    }

    /// <summary>
    /// Class representinga .NET assembly.
    /// Code mostly taken from NinjaTurtles class Module
    /// </summary>
    public class AssemblyCode : IAssemblyCode
    {
        private static readonly object locker = new object();

        private readonly ConcurrentDictionary<string, MethodDefinition> methodsByFullName = new ConcurrentDictionary<string, MethodDefinition>();

        private readonly ConcurrentDictionary<string, TypeDefinition> classesByFullName = new ConcurrentDictionary<string, TypeDefinition>();

        private readonly ConcurrentDictionary<Tuple<string, int>, Tuple<int, SequencePoint>> sequencePointsByInstruction =
            new ConcurrentDictionary<Tuple<string, int>, Tuple<int, SequencePoint>>();

        private readonly ConcurrentDictionary<byte[], SourceFile> sourceFilesByHash =
            new ConcurrentDictionary<byte[], SourceFile>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyCode"/> class.
        /// </summary>
        public AssemblyCode(FileInfo assemblyLocation)
        {
            this.AssemblyLocation = assemblyLocation;
            this.AssemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyLocation.FullName);
        }

        /// <summary>
        /// Gets the location of the assembly on disk .
        /// </summary>
        public FileInfo AssemblyLocation { get; private set; }

        /// <summary>
        /// Gets the <see cref="AssemblyDefinition" />.
        /// </summary>
        public AssemblyDefinition AssemblyDefinition { get; private set; }

        /// <summary>
        /// Gets the method definition by its full name.
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        public MethodDefinition GetMethodByFullName(string fullName)
        {
            return this.methodsByFullName.GetOrAdd(
                string.Intern(fullName),
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

        /// <summary>
        /// Gets a method definition by its class full name and method name.
        /// </summary>
        public MethodDefinition GetMethodByClassAndMethodName(string classFullName, string methodName)
        {
            var type = this.classesByFullName.GetOrAdd(
                string.Intern(classFullName),
                n =>
                {
                    var nameWithNameSpaceOnly = n.Split(',')[0];
                    lock (locker)
                    {
                        return this.AssemblyDefinition.Modules
                            .SelectMany(m => m.Types)
                            .SelectMany(t => ListNestedTypesRecursively(t))
                            .Single(t => t.FullName.Equals(nameWithNameSpaceOnly));
                    }
                });

            return type.Methods.Single(m => m.Name.Equals(methodName));
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

        /// <summary>
        /// Loads debug information.
        /// </summary>
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

        /// <summary>
        /// Gets the sequence point (line of code) of the specified instruction.
        /// </summary>
        public SequencePoint GetNearestSequencePoint(string methodFullName, int instructionOffset)
        {
            var indexAndSp = GetNearestSequencePointWithIndex(methodFullName, instructionOffset);

            return indexAndSp.Item2;
        }

        /// <summary>
        /// Gets the offset of the first instruction of the nearest sequence point (line of code) of the specified instruction.
        /// </summary>
        public int GetNearestSequencePointInstructionOffset(string methodFullName, int instructionOffset)
        {
            var indexAndSp = GetNearestSequencePointWithIndex(methodFullName, instructionOffset);

            return indexAndSp.Item1;
        }

        private Tuple<int, SequencePoint> GetNearestSequencePointWithIndex(string methodFullName, int instructionOffset)
        {
            var indexAndSp = this.sequencePointsByInstruction.GetOrAdd(
                Tuple.Create(string.Intern(methodFullName), instructionOffset),
                t =>
                {
                    lock (locker)
                    {
                        this.LoadDebugInformation();

                        var method = this.GetMethodByFullName(t.Item1);
                        var instructions = method.Body.Instructions.ToList();

                        var offset = t.Item2;
                        var index = instructions.IndexOf(instructions.Single(i => i.Offset == offset));
                        var instruction = instructions[index];
                        while ((instruction.SequencePoint == null
                                || instruction.SequencePoint.StartLine == 0xfeefee) && index > 0)
                        {
                            index--;
                            instruction = instructions[index];
                        }

                        return Tuple.Create(instruction.Offset, instruction.SequencePoint);
                    }
                });

            return indexAndSp;
        }

        /// <summary>
        /// Gets the source file loaded using the specified Document instance.
        /// </summary>
        public SourceFile GetSourceFile(Document reference)
        {
            return this.sourceFilesByHash.GetOrAdd(
                reference.Hash,
                _ => new SourceFile(reference));
        }
    }
}
