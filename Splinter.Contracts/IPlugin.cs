using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splinter.Contracts
{
    public interface IPlugin
    {
        /// <summary>
        /// Returns true if the plugin is available, i.e. has its binaries installed, registered, etc.
        /// </summary>
        bool IsReady(out string unavailableMessage);

        string Name { get; }
    }
}
