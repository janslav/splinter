using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Microsoft.Win32;
using Microsoft.Practices.Unity;

using log4net;

using NinjaTurtles;

using Splinter.Contracts;
using Splinter.Contracts.DTOs;

namespace Splinter.CoverageRunner.OpenCover
{
    [Export(typeof(IPluginFactory<ICoverageRunner>))]
    public class OpenCoverRunnerFactory : IPluginFactory<ICoverageRunner>
    {
        private const string OpenCoverRegKey = @"SOFTWARE\OpenCover\";
        private const string OpenCoverRegKeyWow6432 = @"SOFTWARE\Wow6432Node\OpenCover\";
        private const string OpenCoverRegValue = "Path";
        private const string OpenCoverExeName = "OpenCover.Console.exe";

        public ICoverageRunner GetPlugin(ILog log)
        {
            var container = new UnityBootstrapper(log).CreateContainer();

            var runner = container.Resolve<OpenCoverRunner>();

            return runner;
        }
    }
}
