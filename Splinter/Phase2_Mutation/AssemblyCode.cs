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

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;

namespace Splinter.Phase2_Mutation
{
    /// <summary>
    /// Class representing the main module of a .NET assembly.
    /// </summary>
    public class AssemblyCode
    {
        private static readonly object locker = new object();

        private readonly ConcurrentDictionary<string, MethodDefinition> methodsByFullName = new ConcurrentDictionary<string, MethodDefinition>();

        public AssemblyCode(FileInfo assemblyLocation)
        {
            AssemblyLocation = assemblyLocation;
            AssemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyLocation.FullName);
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
                        return this.AssemblyDefinition.Modules.SelectMany(m => m.Types).SelectMany(t => t.Methods)
                            .Single(m => m.FullName.Equals(n));
                    }
                });
        }

    }
}
