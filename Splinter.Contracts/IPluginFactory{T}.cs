using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splinter.Contracts
{
    public interface IPluginFactory<T> where T : IPlugin
    {
        T GetPlugin(log4net.ILog log);
    }
}
