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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ComponentModel.Composition;
using System.Threading;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;
using Mono.Cecil.Rocks;

using Splinter.Utils;
using Splinter.Utils.Cecil;
using Splinter.Phase2_Mutation.DTOs;

using log4net;

namespace Splinter.Phase2_Mutation.NinjaTurtles.Turtles
{
    /// <summary>
    /// An <b>interface</b> defining basic functionality for a turtle that
    /// operates on the IL of a method body.
    /// </summary>
    [InheritedExport]
    public interface IMethodTurtle
    {
        /// <summary>
        /// Returns a collection of detailed descriptions
        /// of mutations, having first carried out the mutation in question and
        /// saved the modified assembly under test to disk.
        /// </summary>
        IReadOnlyCollection<Mutation> TryCreateMutants(MutationTestSessionInput input);

        /// <summary>
        /// Gets a description of this turtle.
        /// </summary>
        string Description { get; }
    }

    /// <summary>
    /// An abstract base class for implementations of
    /// <see cref="IMethodTurtle" />.
    /// </summary>
    public abstract class MethodTurtleBase : IMethodTurtle
    {
        private readonly ILog log;

        private static int counter;

        public MethodTurtleBase(ILog log)
        {
            this.log = log;
        }

        public IReadOnlyCollection<Mutation> TryCreateMutants(MutationTestSessionInput input)
        {

            var ret = MutateMethod(input);

            //ret = ret.Concat(MutateEnumerableGenerators(method, module));

            //ret = ret.Concat(MutateClosures(method, module));

            //ret = ret.Concat(MutateAnonymousDelegates(method, module));

            return ret.ToArray();
        }

        //private IEnumerable<MutantMetaData> MutateEnumerableGenerators(MethodDefinition method, MutatedAssembly module)
        //{
        //    var nestedType =
        //        method.DeclaringType.NestedTypes.FirstOrDefault(
        //            t => t.Name.StartsWith(string.Format("<{0}>", method.Name))
        //            && t.Interfaces.Any(i => i.Name == "IEnumerable`1"));
        //    if (nestedType == null)
        //        return Enumerable.Empty<MutantMetaData>();

        //    var nestedMethod = nestedType.Methods.First(m => m.Name == "MoveNext");
        //    var originalOffsets = nestedMethod.Body.Instructions.Select(i => i.Offset).ToArray();
        //    return MutateMethod(nestedMethod, module, originalOffsets);
        //}

        //private IEnumerable<MutantMetaData> MutateClosures(MethodDefinition method, MutatedAssembly module)
        //{
        //    var ret = Enumerable.Empty<MutantMetaData>();

        //    var nestedType =
        //        method.DeclaringType.NestedTypes.FirstOrDefault(
        //            t => t.Name.StartsWith("<>c__DisplayClass")
        //                && t.Methods.Any(m => m.Name.StartsWith(string.Format("<{0}>", method.Name)))
        //            );
        //    if (nestedType == null)
        //        return ret;

        //    var closureMethods = nestedType.Methods.Where(m => m.Name.StartsWith(string.Format("<{0}>", method.Name)));
        //    foreach (var closureMethod in closureMethods) { 
        //        var originalOffsets = closureMethod.Body.Instructions.Select(i => i.Offset).ToArray();
        //        ret = ret.Concat(MutateMethod(closureMethod, module, originalOffsets));
        //    }

        //    return ret;
        //}

        //private IEnumerable<MutantMetaData> MutateAnonymousDelegates(MethodDefinition method, MutatedAssembly module)
        //{
        //    var delegateMethods = method.DeclaringType.Methods.Where(m => m.Name.StartsWith(string.Format("<{0}>", method.Name)));

        //    var ret = Enumerable.Empty<MutantMetaData>();
        //    foreach (var delegateMethod in delegateMethods)
        //    {
        //        var originalOffsets = delegateMethod.Body.Instructions.Select(i => i.Offset).ToArray();
        //        ret = ret.Concat(MutateMethod(delegateMethod, module, originalOffsets));
        //    }

        //    return ret;
        //}

        private IEnumerable<Mutation> MutateMethod(MutationTestSessionInput input)
        {
            var methodName = input.Subject.Method.FullName;
            var assemblyToMutate = new AssemblyCode(input.Subject.Method.Assembly);
            assemblyToMutate.LoadDebugInformation();

            var methodToMutate = assemblyToMutate.GetMethodByFullName(methodName);

            int[] originalOffsets = methodToMutate.Body.Instructions.Select(i => i.Offset).ToArray();

            //leave as a yield-return, so that we don't optimize macros again until we stop enumerating.
            methodToMutate.Body.SimplifyMacros();
            foreach (var mutation in this.TryToCreateMutations(input, assemblyToMutate.AssemblyDefinition, methodToMutate, originalOffsets))
            {
                yield return mutation;
            }
            methodToMutate.Body.OptimizeMacros();
        }

        /// <summary>
        /// Gets a description of the current turtle.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Performs the actual code mutations, returning each with
        /// <c>yield</c> for the calling code to use.
        /// </summary>
        /// <remarks>
        /// Implementing classes should yield the result obtained by calling
        /// the <see mref="SaveMutantToDisk" /> method.
        /// </remarks>
        protected abstract IEnumerable<Mutation> TryToCreateMutations(MutationTestSessionInput input, AssemblyDefinition assemblyToMutate, MethodDefinition method, int[] originalOffsets);

        /// <summary>
        /// A helper method that copies the test folder, and saves the mutated
        /// assembly under test into it before returning an instance of
        /// <see cref="Mutation" />.
        /// </summary>
        /// <param name="index">
        /// The index of the (first) IL instruction at which the mutation was
        /// applied.
        /// </param>
        protected Mutation SaveMutantToDisk(MutationTestSessionInput input, AssemblyDefinition mutant, int index, string description)
        {
            var i = Interlocked.Increment(ref counter);
            var mutationId = string.Format("Mutation{0:0000}:", i);

            this.log.DebugFormat(
                "{0}Creating mutation of method '{1}' from assembly '{2}: {3}.'",
                mutationId,
                input.Subject.Method.FullName,
                input.Subject.Method.Assembly.Name,
                description);

            var shadow = new ShadowDirectory(this.log, input.ModelDirectory, mutationId);

            var shadowedPath = shadow.GetEquivalentShadowPath(input.Subject.Method.Assembly);

            mutant.Write(shadowedPath.FullName);

            return new Mutation(mutationId, input, shadow, shadowedPath, index, description);
        }
    }
}
