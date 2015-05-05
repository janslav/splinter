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
            if (!plugins.DiscoveredTestRunners.EmptyIfNull().Any())
            {
                throw new Exception("No test runners available.");
            }

            if (!plugins.DiscoveredCoverageRunners.EmptyIfNull().Any())
            {
                throw new Exception("No coverage runners available.");
            }

            var testRunners = plugins.FilterByAvailability(plugins.DiscoveredTestRunners, "test runner");
            var coverageRunners = plugins.FilterByAvailability(plugins.DiscoveredCoverageRunners, "coverage runner");

            var ttr = this.discoverer.DiscoverTestBinaries(cmdLine, testRunners);

            log.Info("Test runner: " + ttr.TestRunner.Name);
            log.Info("Test binaries: " + string.Join(", ", ttr.TestBinaries.Select(fi => fi.Name)));
        }
    }
}
