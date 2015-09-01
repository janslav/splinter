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

namespace Splinter.Utils.Cecil
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

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
        /// Gets a method definition by its class full name and method name.
        /// </summary>
        MethodDefinition GetMethodByClassAndMethodName(string classFullName, string methodName);

        /// <summary>
        /// Gets the method definition by its metadata token number.
        /// </summary>
        MethodDefinition GetMethodByMetaDataToken(uint token);

        /// <summary>
        /// Loads debug information.
        /// </summary>
        void LoadDebugInformation();

        /// <summary>
        /// Gets the sequence point (line of code) of the specified instruction.
        /// </summary>
        SequencePoint GetNearestSequencePoint(uint methodMetadataToken, int instructionOffset);

        /// <summary>
        /// Gets the offset of the first instruction of the nearest sequence point (line of code) of the specified instruction.
        /// </summary>
        int GetNearestSequencePointInstructionOffset(uint methodMetadataToken, int instructionOffset);

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
        private static readonly object Locker = new object();

        private readonly ConcurrentDictionary<string, MethodDefinition> methodsByFullName = new ConcurrentDictionary<string, MethodDefinition>();

        private readonly ConcurrentDictionary<uint, MethodDefinition> methodsByMetaDataToken = new ConcurrentDictionary<uint, MethodDefinition>();

        private readonly ConcurrentDictionary<string, TypeDefinition> classesByFullName = new ConcurrentDictionary<string, TypeDefinition>();

        private readonly ConcurrentDictionary<Tuple<uint, int>, Tuple<int, SequencePoint>> sequencePointsByInstruction =
            new ConcurrentDictionary<Tuple<uint, int>, Tuple<int, SequencePoint>>();

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
        /// Gets the method definition by its metadata token number.
        /// </summary>
        public MethodDefinition GetMethodByMetaDataToken(uint token)
        {
            return this.methodsByMetaDataToken.GetOrAdd(
                token,
                n =>
                {
                    lock (Locker)
                    {
                        var allMethods = this.AssemblyDefinition.Modules
                            .SelectMany(m => m.Types)
                            .SelectMany(ListNestedTypesRecursively)
                            .SelectMany(t => t.Methods);

                        var mt = new MetadataToken(n);

                        return allMethods.Single(m => m.MetadataToken.Equals(mt));
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
                    lock (Locker)
                    {
                        return this.AssemblyDefinition.Modules
                            .SelectMany(m => m.Types)
                            .SelectMany(ListNestedTypesRecursively)
                            .Single(t => t.FullName.Equals(nameWithNameSpaceOnly));
                    }
                });

            return type.Methods.Single(m => m.Name.Equals(methodName));
        }

        private static IEnumerable<TypeDefinition> ListNestedTypesRecursively(TypeDefinition t)
        {
            foreach (var nested in t.NestedTypes)
            {
                foreach (var nestedNested in ListNestedTypesRecursively(nested))
                {
                    yield return nestedNested;
                }
            }

            yield return t;
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
        public SequencePoint GetNearestSequencePoint(uint methodMetadataToken, int instructionOffset)
        {
            var indexAndSp = this.GetNearestSequencePointWithIndex(methodMetadataToken, instructionOffset);

            return indexAndSp.Item2;
        }

        /// <summary>
        /// Gets the offset of the first instruction of the nearest sequence point (line of code) of the specified instruction.
        /// </summary>
        public int GetNearestSequencePointInstructionOffset(uint methodMetadataToken, int instructionOffset)
        {
            var indexAndSp = this.GetNearestSequencePointWithIndex(methodMetadataToken, instructionOffset);

            return indexAndSp.Item1;
        }

        private Tuple<int, SequencePoint> GetNearestSequencePointWithIndex(uint methodMetadataToken, int instructionOffset)
        {
            var indexAndSp = this.sequencePointsByInstruction.GetOrAdd(
                Tuple.Create(methodMetadataToken, instructionOffset),
                t =>
                {
                    lock (Locker)
                    {
                        this.LoadDebugInformation();

                        var method = this.GetMethodByMetaDataToken(t.Item1);
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
