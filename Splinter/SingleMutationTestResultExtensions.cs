using Mono.Cecil.Cil;
using Splinter.Contracts.DTOs;
using Splinter.Utils.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splinter
{
    public static class SingleMutationTestResultExtensions
    {
        public static CodeRegionInfo ToCodeLineInfo(this SingleMutationTestResult testResult, ICodeCache codeCache)
        {
            var a = codeCache.GetAssemblyDefinition(testResult.Subject.Assembly);
            var sp = a.GetNearestSequencePoint(testResult.Subject.FullName, testResult.InstructionOffset);
            var source = a.GetSourceFile(sp.Document);
            return new CodeRegionInfo(source, sp);
        }
    }
}
