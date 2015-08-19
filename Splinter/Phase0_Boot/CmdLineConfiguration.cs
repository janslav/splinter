using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Mono.Options;

using Splinter.Phase1_Discovery;

namespace Splinter.Phase0_Boot
{
    /// <summary>
    /// Represents the command line options of splinter (without the plugin-specific parts)
    /// </summary>
    public class CmdLineConfiguration
    {
        private List<string> testBinaries = new List<string>();

        public CmdLineConfiguration()
        {
            this.DetectUnusedTest = false;
        }

        /// <summary>
        /// Gets the test runner name.
        /// </summary>
        public string TestRunner { get; private set; }

        /// <summary>
        /// Gets the coverage runner name.
        /// </summary>
        public string CoverageRunner { get; private set; }

        /// <summary>
        /// Gets the working directory path.
        /// </summary>
        public string WorkingDirectory { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to detect unused test.
        /// </summary>
        public bool DetectUnusedTest { get; private set; }

        /// <summary>
        /// Gets the name of the selected test ordering strategy
        /// </summary>
        public string TestOrderingStrategy { get; set; }

        /// <summary>
        /// Gets the test binaries.
        /// </summary>
        public IReadOnlyCollection<string> TestBinaries { get { return this.testBinaries; } }

        /// <summary>
        /// Validates this instance.
        /// </summary>
        public void Validate()
        {
            if (this.TestBinaries.Any())
            {
                if (string.IsNullOrWhiteSpace(this.TestRunner))
                {
                    throw new Exception("If test binaries are specified, then the runner must be specified as well.");
                }
            }
        }

        /// <summary>
        /// Sets up the command line options.
        /// </summary>
        public static CmdLineConfiguration SetupCommandLineOptions(OptionSet options, IPluginsContainer plugins)
        {
            var config = new CmdLineConfiguration();

            var testRunnerPlugins = string.Join(", ", plugins.DiscoveredTestRunners.Select(tr => tr.Name));
            var coveragePlugins = string.Join(", ", plugins.DiscoveredCoverageRunners.Select(tr => tr.Name));
            var testOrderingStrategies = string.Join(", ", plugins.DiscoveredTestOrderingStrategyFactories.Select(tr => tr.Name));

            options.Add("testRunner=", "The test runner engine name. Available: " + testRunnerPlugins, v => config.TestRunner = v);
            options.Add("coverageRunner=", "The test coverage engine name. Available: " + coveragePlugins, v => config.CoverageRunner = v);
            options.Add("testOrderingStrategy=", "Strategies for ordering of tests that are run against mutants. Available: " + testOrderingStrategies, v => config.TestOrderingStrategy = v);
            options.Add("workingDirectory=", "The directory containing the testing and tested code. Default: current dir.", v => config.WorkingDirectory = v);
            options.Add("detectUnusedTests", "Detect tests that don't contribute to killing mutants. Default: false", v => config.DetectUnusedTest = !string.IsNullOrWhiteSpace(v));

            options.Add("<>", config.testBinaries.Add);

            return config;
        }
    }
}
