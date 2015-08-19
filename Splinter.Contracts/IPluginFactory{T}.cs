using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;

namespace Splinter.Contracts
{
    /// <summary>
    /// The plugin factory.
    /// </summary>
    /// <typeparam name="T">Type of the plugin.</typeparam>
    [InheritedExport]
    public interface IPluginFactory<T> where T : IPlugin
    {
        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Creates and returns the plugin.
        /// </summary>
        T GetPlugin(log4net.ILog log);
    }
}
