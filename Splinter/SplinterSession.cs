using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Splinter.Model;

using log4net;

namespace Splinter
{
    public interface ISplinterSession
    {

        SessionSettings Initialize(string[] args);
    }

    public class SplinterSession : ISplinterSession
    {
        ILog log;

        IPluginsContainer plugins;

        public SplinterSession(ILog log, IPluginsContainer plugins)
        {
            this.plugins = plugins;
            this.log = log;
        }

        public SessionSettings Initialize(string[] args)
        {
            if (!plugins.TestRunners.EmptyIfNull().Any())
            {
                //th
            }

            return null;
        }
    }
}
