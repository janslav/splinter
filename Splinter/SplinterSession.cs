using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

using Splinter.Contracts;
using Splinter.Model;

using log4net;

namespace Splinter
{
    public interface ISplinterSession
    {
        void Start(ManualConfiguration cmdLine);
    }

    public class SplinterSession : ISplinterSession
    {
        ILog log;

        IPluginsContainer plugins;

        ITestsDiscoverer discoverer;

        public SplinterSession(ILog log, IPluginsContainer plugins, ITestsDiscoverer discoverer)
        {
            this.plugins = plugins;
            this.discoverer = discoverer;
            this.log = log;
        }

        public void Start(ManualConfiguration cmdLine)
        {
            if (!plugins.TestRunners.EmptyIfNull().Any())
            {
                throw new Exception("No test runners available.");
            }

            if (!plugins.CoverageRunners.EmptyIfNull().Any())
            {
                throw new Exception("No coverage runners available.");
            }

            var testRunners = this.CheckPluginReadyness(plugins.TestRunners, "test runner");
            var coverageRunners = this.CheckPluginReadyness(plugins.CoverageRunners, "coverage runner");

            var ttr = this.discoverer.DiscoverTestBinaries(cmdLine, testRunners);

            log.Info("Test runner: " + ttr.TestRunner.Name);
            log.Info("Test binaries: " + string.Join(", ", ttr.TestBinaries.Select(fi => fi.Name)));
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
                var notReady = readiness.Where(r => !r.Ready);
                if (notReady.Any())
                {
                    var msgs = string.Join(Environment.NewLine, notReady.Select(tr => tr.Runner.Name + ": " + tr.Msg));
                    this.log.DebugFormat("Some {0} not ready/installed:{1}{2}", categoryName, Environment.NewLine, msgs);
                }
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
