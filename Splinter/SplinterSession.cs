using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

using Splinter.Contracts;
using Splinter.Phase0_Boot;
using Splinter.Phase1_Discovery;
using Splinter.Phase2_Mutation;
using Splinter.Phase2_Mutation.DTOs;
using Splinter.Phase3_Reporting;

using log4net;

namespace Splinter
{
    public interface ISplinterSession
    {
        void Run(ManualConfiguration cmdLine);
    }

    public class SplinterSession : ISplinterSession
    {
        private readonly ILog log;

        private readonly IPluginsContainer plugins;

        private readonly ITestsDiscoverer discoverer;

        private readonly IMutationTestSession mutation;

        private readonly IResultsLogger resultsLogger;

        public SplinterSession(ILog log, IPluginsContainer plugins, ITestsDiscoverer discoverer, IMutationTestSession mutation, IResultsLogger resultsLogger)
        {
            this.plugins = plugins;
            this.discoverer = discoverer;
            this.log = log;
            this.mutation = mutation;
            this.resultsLogger = resultsLogger;
        }

        public void Run(ManualConfiguration cmdLine)
        {
            //Phase 0: configuration / plugins discovery
            if (!this.plugins.DiscoveredTestRunners.EmptyIfNull().Any())
            {
                throw new Exception("No test runners available.");
            }

            if (!this.plugins.DiscoveredCoverageRunners.EmptyIfNull().Any())
            {
                throw new Exception("No coverage runners available.");
            }

            var modelDirectory = new DirectoryInfo(
                !string.IsNullOrWhiteSpace(cmdLine.WorkingDirectory) ? cmdLine.WorkingDirectory : Environment.CurrentDirectory);
            this.log.DebugFormat("Operation root directory is '{0}'", modelDirectory.FullName);

            var testRunners = this.plugins.FilterByAvailability(this.plugins.DiscoveredTestRunners, "test runner");
            var coverageRunner = this.PickCoverageRunner(cmdLine);

            this.log.Info("Coverage runner: " + coverageRunner.Name);

            //Phase 1: find tests and run them to see who tests what
            var testBinaries = this.discoverer.DiscoverTestBinaries(cmdLine, modelDirectory, testRunners);
            this.log.Info("Test binaries: " + string.Join(", ", testBinaries.Select(fi => fi.Runner.Name)));

            var testedMethods = coverageRunner.GetInitialCoverage(modelDirectory, testBinaries);

            var subjectAssemblies = testedMethods.Select(tm => tm.Method.Assembly.Name).Distinct(StringComparer.OrdinalIgnoreCase);
            var testMethodsCount = testedMethods.SelectMany(tm => tm.TestMethods).Distinct().Count();

            this.log.Info("Covered subject code assemblies: " + Environment.NewLine + string.Join(Environment.NewLine, subjectAssemblies));
            this.log.Info("Number of unique subject methods: " + testedMethods.Count);
            this.log.Info("Number of unique test methods: " + testMethodsCount);

            //Phase 2 - mutate away!
            this.log.Info("Starting mutation runs.");
            var mutationResults = testedMethods.AsParallel().SelectMany(subject =>
                this.mutation.Run(new MutationTestSessionInput(modelDirectory, subject))).ToArray();
            this.log.Info("Mutation runs finished.");

            //Phase 3 - output results
            this.resultsLogger.LogResults(mutationResults);
        }

        private ICoverageRunner PickCoverageRunner(ManualConfiguration cmdLine)
        {
            ICoverageRunner coverageRunner = null;
            if (!string.IsNullOrWhiteSpace(cmdLine.CoverageRunner))
            {
                coverageRunner = this.plugins.DiscoveredCoverageRunners.SingleOrDefault(cr => string.Equals(cmdLine.CoverageRunner, cr.Name, StringComparison.OrdinalIgnoreCase));
                if (coverageRunner == null)
                {
                    throw new Exception(string.Format("Coverage runner '{0}' not known.", cmdLine.CoverageRunner));
                }
            }

            var coverageRunners = this.plugins.FilterByAvailability(this.plugins.DiscoveredCoverageRunners, "coverage runner");
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

            return coverageRunner;
        }
    }
}
