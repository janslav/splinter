using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;

namespace Splinter.Contracts
{
    [InheritedExport]
    public interface IPluginFactory<T> where T : IPlugin
    {
        T GetPlugin(log4net.ILog log);
    }
}
