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
    public interface IModuleCache
    {
        Module GetAssembly(FileInfo location);
    }

    public class ModuleCache : IModuleCache
    {
        private readonly ConcurrentDictionary<FileInfo, Module> assemblies =
            new ConcurrentDictionary<FileInfo, Module>(new FileSystemInfoComparer());


        public Module GetAssembly(FileInfo location)
        {
            return this.assemblies.GetOrAdd(location, l => new Module(l));
        }
    }
}
