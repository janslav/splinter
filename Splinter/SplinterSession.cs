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

            ICoverageRunner coverageRunner = null;
            if (!string.IsNullOrWhiteSpace(cmdLine.CoverageRunner))
            {
                coverageRunner = plugins.DiscoveredCoverageRunners.SingleOrDefault(cr => string.Equals(cmdLine.CoverageRunner, cr.Name, StringComparison.OrdinalIgnoreCase));
                if (coverageRunner == null)
                {
                    throw new Exception(string.Format("Coverage runner '{0}' not known.", cmdLine.CoverageRunner));
                }
            }

            var testRunners = plugins.FilterByAvailability(plugins.DiscoveredTestRunners, "test runner");
            var ttr = this.discoverer.DiscoverTestBinaries(cmdLine, testRunners);

            this.log.Info("Test runner: " + ttr.TestRunner.Name);
            this.log.Info("Test binaries: " + string.Join(", ", ttr.TestBinaries.Select(fi => fi.Name)));

            var coverageRunners = plugins.FilterByAvailability(plugins.DiscoveredCoverageRunners, "coverage runner");
            if (coverageRunner != null)
            {
                if (!coverageRunners.Contains(coverageRunner))
                {
                    throw new Exception(string.Format("Coverage runner '{0}' not ready (see above).", cmdLine.CoverageRunner));
                }
            }
            else if (coverageRunners.Count == 1)
            {
                coverageRunner = coverageRunners.Single();
            }
            else
            {
                coverageRunner = coverageRunners.First();
                this.log.Debug("Multiple coverage runners available and none picked manually, picking the first one.");
            }

            this.log.Info("Coverage runner: " + coverageRunner.Name);

            coverageRunner.GetInitialCoverage(ttr);
        }
    }
}
