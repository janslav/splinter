using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Splinter.Utils;

namespace Splinter.Utils.Cecil
{
    public interface ICodeCache
    {
        IAssemblyCode GetAssembly(FileInfo location);
    }

    public class CodeCache : ICodeCache
    {
        public static CodeCache Instance = new CodeCache();

        private readonly ConcurrentDictionary<FileInfo, AssemblyCode> assemblies =
            new ConcurrentDictionary<FileInfo, AssemblyCode>(new FileSystemInfoComparer());


        public IAssemblyCode GetAssembly(FileInfo location)
        {
            return this.assemblies.GetOrAdd(location, l => new AssemblyCode(l));
        }
    }
}
