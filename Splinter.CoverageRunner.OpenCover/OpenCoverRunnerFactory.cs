﻿using System;
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
using Splinter.Utils;

namespace Splinter.CoverageRunner.OpenCover
{
    /// <summary>
    /// Creates the OpenCoverRunner objects
    /// </summary>
    public class OpenCoverRunnerFactory : TypeBasedEqualityImplementation, IPluginFactory<ICoverageRunner>
    {
        /// <summary>
        /// Creates and returns the plugin.
        /// </summary>
        public ICoverageRunner GetPlugin(ILog log)
        {
            var container = new UnityBootstrapper(log).CreateContainer();

            var runner = container.Resolve<OpenCoverRunner>();

            return runner;
        }

        public string Name
        {
            get { return "OpenCoverRunnerFactory"; }
        }
    }
}
