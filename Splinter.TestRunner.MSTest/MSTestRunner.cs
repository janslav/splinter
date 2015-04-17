using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;

using Splinter.Contracts;

namespace Splinter.TestRunner.MSTest
{
    [Export(typeof(IPluginFactory<ITestRunner>))]
    public class MSTestRunner : ITestRunner, IPluginFactory<ITestRunner>
    {
        private log4net.ILog log;

        ITestRunner IPluginFactory<ITestRunner>.GetPlugin(log4net.ILog log)
        {
            this.log = log;
            return this;
        }

        public bool IsAvailable()
        {
            throw new NotImplementedException("not there yet");
        }

        public bool IsReady(out string unavailableMessage)
        {
            unavailableMessage = "MSTestRunner not implemented yet";

            return false;
        }

        public string Name
        {
            get { return "MsTest"; }
        }
    }
}
