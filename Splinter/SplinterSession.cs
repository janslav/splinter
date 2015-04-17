using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Splinter.Model;
using Splinter.Contracts;

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
                this.log.Error("No test runners available.");
                return null;
            }

            var settings = new SessionSettings
            {
                TestRunners = this.CheckPluginReadyness(plugins.TestRunners, "test runner"),
                CoverageRunners = this.CheckPluginReadyness(plugins.CoverageRunners, "coverage runner"),
            };

            return settings;
        }

        private IReadOnlyCollection<T> CheckPluginReadyness<T>(IEnumerable<T> plugins, string categoryName) where T : IPlugin
        {
            var readiness = plugins.Select(tr =>
            {
                string msg;
                return new
                {
                    Runner = tr,
                    Ready = tr.IsReady(out msg),
                    Msg = msg
                };
            });

            if (!readiness.Any(tr => tr.Ready))
            {
                var msgs = string.Join(Environment.NewLine, readiness.Select(tr => tr.Runner.Name + ": " + tr.Msg));
                throw new Exception(string.Format("No {0} ready/installed:{1}{2}", categoryName, Environment.NewLine, msgs));
            }
            else
            {
                var msgs = string.Join(Environment.NewLine, readiness.Select(tr => tr.Runner.Name + ": " + tr.Msg));
                this.log.DebugFormat("Some {0} not ready/installed:{1}{2}", categoryName, Environment.NewLine, msgs);
            }

            var coverageRunnerReadiness = plugins.Select(tr =>
            {
                string msg;
                return new
                {
                    Runner = tr,
                    Ready = tr.IsReady(out msg),
                    Msg = msg
                };
            });

            return readiness.Where(tr => tr.Ready).Select(tr => tr.Runner).ToArray();
        }
    }
}
