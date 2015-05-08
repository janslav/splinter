﻿using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Splinter.Phase2_Mutation.NinjaTurtles
{
    static class MutantExtensions
    {
        internal static SequencePoint GetCurrentSequencePoint(this MethodDefinition _method, int index)
        {
            var instruction = _method.Body.Instructions[index];
            while ((instruction.SequencePoint == null
                    || instruction.SequencePoint.StartLine == 0xfeefee) && index > 0)
            {
                index--;
                instruction = _method.Body.Instructions[index];
            }
            var sequencePoint = instruction.SequencePoint;
            return sequencePoint;
        }

        

        internal static string GetOriginalSourceFileName(this MethodDefinition _method, int index)
        {
            var sequencePoint = _method.GetCurrentSequencePoint(index);
            return Path.GetFileName(sequencePoint.Document.Url);
        }
    }
}