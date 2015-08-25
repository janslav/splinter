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
    /// <summary>
    /// Caches loaded IAssemblyCode instances
    /// </summary>
    public interface ICodeCache
    {
        /// <summary>
        /// Gets the assembly definition.
        /// </summary>
        IAssemblyCode GetAssemblyDefinition(FileInfo location);
    }

    /// <summary>
    /// Caches loaded IAssemblyCode instances
    /// </summary>
    public class CodeCache : ICodeCache
    {
        public static readonly CodeCache Instance = new CodeCache();

        private readonly ConcurrentDictionary<FileInfo, AssemblyCode> assemblies =
            new ConcurrentDictionary<FileInfo, AssemblyCode>(new FileSystemInfoComparer());

        /// <summary>
        /// Gets the assembly definition.
        /// </summary>
        public IAssemblyCode GetAssemblyDefinition(FileInfo location)
        {
            return this.assemblies.GetOrAdd(location, l => new AssemblyCode(l));
        }
    }
}
