using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using System.Diagnostics;

using Microsoft.Practices.Unity;

using Splinter.Contracts.DTOs;
using Splinter.Contracts;
using Splinter.Utils;
using Splinter.Utils.Cecil;

namespace Splinter.TestRunner.MsTest
{
    /// <summary>
    /// Produces the mstest runner plugin
    /// </summary>
    public class MSTestRunnerFactory : IPluginFactory<ITestRunner>
    {
        /// <summary>
        /// Creates and returns the plugin.
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        ITestRunner IPluginFactory<ITestRunner>.GetPlugin(log4net.ILog log)
        {
            var container = new UnityBootstrapper(log).CreateContainer();

            var runner = container.Resolve<MSTestRunner>();

            return runner;
        }
    }
}
