using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Mono.Options;

namespace Splinter.Contracts
{
    public interface IPlugin
    {
        string Name { get; }

        void SetupCommandLineOptions(OptionSet options);

        /// <summary>
        /// Returns true if the plugin is available, i.e. has its binaries installed, registered, etc.
        /// </summary>
        bool IsReady(out string unavailableMessage);
    }
}
