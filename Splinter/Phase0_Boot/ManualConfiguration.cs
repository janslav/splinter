using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Mono.Options;

namespace Splinter.Phase0_Boot
{
    /// <summary>
    /// Represents the command line options of splinter (without the plugin-specific parts)
    /// </summary>
    public class ManualConfiguration
    {
        private List<string> testBinaries = new List<string>();

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
        public static ManualConfiguration SetupCommandLineOptions(OptionSet options)
        {
            var config = new ManualConfiguration();

            options.Add("testRunner=", "The test runner engine name, such as mstest or nunit.", v => config.TestRunner = v);
            options.Add("coverageRunner=", "The test coverage engine name, such as opencover.", v => config.CoverageRunner = v);
            options.Add("workingDirectory=", "The directory containing the application being tested. Will be copied to temp locations with mutated code. When not specified, current dir is used.", v => config.WorkingDirectory = v);

            options.Add("<>", config.testBinaries.Add);

            return config;
        }
    }
}
