using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Splinter.Utils;
using Splinter.Phase2_Mutation.NinjaTurtles;

namespace Splinter.Phase2_Mutation
{
    public interface ICodeCache
    {
        AssemblyCode GetAssembly(FileInfo location);
    }

    public class CodeCache : ICodeCache
    {
        private readonly ConcurrentDictionary<FileInfo, AssemblyCode> assemblies =
            new ConcurrentDictionary<FileInfo, AssemblyCode>(new FileSystemInfoComparer());


        public AssemblyCode GetAssembly(FileInfo location)
        {
            return this.assemblies.GetOrAdd(location, l => new AssemblyCode(l));
        }
    }
}
