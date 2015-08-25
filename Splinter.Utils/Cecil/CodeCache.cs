namespace Splinter.Utils.Cecil
{
    using System.Collections.Concurrent;
    using System.IO;

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
