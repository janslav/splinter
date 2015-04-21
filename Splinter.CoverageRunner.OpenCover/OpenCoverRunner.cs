using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;

using Splinter.Contracts;

namespace Splinter.CoverageRunner.OpenCover
{
    [Export(typeof(IPluginFactory<ICoverageRunner>))]
    public class OpenCoverRunner : ICoverageRunner, IPluginFactory<ICoverageRunner>
    {
        private log4net.ILog log;

        ICoverageRunner IPluginFactory<ICoverageRunner>.GetPlugin(log4net.ILog log)
        {
            this.log = log;
            return this;
        }

        public bool IsReady(out string unavailableMessage)
        {
            unavailableMessage = "OpenCoverRunner not implemented yet";

            return true;
        }

        public string Name
        {
            get { return "OpenCover"; }
        }
    }
}
