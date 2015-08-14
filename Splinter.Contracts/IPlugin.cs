using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Mono.Options;

namespace Splinter.Contracts
{
    /// <summary>
    /// The base interface of all plugins
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Sets up command line options specific to this plugin to splinter.
        /// </summary>
        void SetupCommandLineOptions(OptionSet options);

        /// <summary>
        /// Returns true if the plugin is available, i.e. has its binaries installed, registered, etc.
        /// </summary>
        bool IsReady(out string unavailableMessage);
    }
}
