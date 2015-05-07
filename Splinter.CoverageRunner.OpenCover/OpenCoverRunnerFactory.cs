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

using Splinter.Contracts;
using Splinter.Contracts.DTOs;

namespace Splinter.CoverageRunner.OpenCover
{
    public class OpenCoverRunnerFactory : IPluginFactory<ICoverageRunner>
    {
        public ICoverageRunner GetPlugin(ILog log)
        {
            var container = new UnityBootstrapper(log).CreateContainer();

            var runner = container.Resolve<OpenCoverRunner>();

            return runner;
        }
    }
}
