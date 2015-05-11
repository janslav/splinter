using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

using Splinter.Contracts;
using Splinter.Contracts.DTOs;

using log4net;

using Splinter.Phase0_Boot;

namespace Splinter.Phase1_Discovery
{
    /// <summary>
    /// Performs the discovery of test binaries that are to be run.
    /// </summary>
    public interface ITestsDiscoverer
    {
        /// <summary>
        /// Discovers test binaries that are to be run, and the related test runner.
        /// Uses manually specified configuration, if any.
        /// </summary>
        /// <param name="cmdLine">Manual configuration via command line.</param>
        /// <param name="testRunners">Available test runners.</param>
        /// <returns>The test runner implementations and the related tests</returns>
        IReadOnlyCollection<TestBinary> DiscoverTestBinaries(ManualConfiguration cmdLine, DirectoryInfo modelDirectory, IReadOnlyCollection<ITestRunner> testRunners);
    }

    public class TestsDiscoverer : ITestsDiscoverer
    {
        ILog log;

        public TestsDiscoverer(ILog log)
        {
            this.log = log;
        }

        public IReadOnlyCollection<TestBinary> DiscoverTestBinaries(ManualConfiguration cmdLine, DirectoryInfo modelDirectory, IReadOnlyCollection<ITestRunner> testRunners)
        {
            IEnumerable<Tuple<string, FileInfo>> testBinariesWithRunner;

            var selectedTestRunner = SelectTestRunnerIfManuallySpecified(cmdLine, testRunners);

            if (cmdLine.TestBinaries.EmptyIfNull().Any())
            {
                //when particular binaries are specified, the runner is also already specified. This fact should be validated already at this point.
                testBinariesWithRunner = this.DiscoverManuallySpecifiedBinaries(cmdLine).Select(fi => Tuple.Create(cmdLine.TestRunner, fi));
            }
            else
            {
                //if the user specified a particular runner, we don't use any other
                var allowedTestRunners = testRunners;
                if (selectedTestRunner != null)
                {
                    allowedTestRunners = new[] { selectedTestRunner };
                }

                testBinariesWithRunner = this.AutoDiscoverBinaries(modelDirectory, allowedTestRunners);

                if (!testBinariesWithRunner.EmptyIfNull().Any())
                {
                    throw new Exception("No test binaries found");
                }
            }

            var ttr = testBinariesWithRunner.Select(i =>
                new TestBinary(testRunners.Single(r => r.Name.Equals(i.Item1, StringComparison.OrdinalIgnoreCase)), i.Item2));

            return ttr.ToArray();
        }

        private ITestRunner SelectTestRunnerIfManuallySpecified(ManualConfiguration cmdLine, IReadOnlyCollection<ITestRunner> testRunners)
        {
            ITestRunner selectedTestRunner = null;

            if (!string.IsNullOrWhiteSpace(cmdLine.TestRunner))
            {
                selectedTestRunner = testRunners.SingleOrDefault(r => r.Name.Equals(cmdLine.TestRunner, StringComparison.OrdinalIgnoreCase));
                if (selectedTestRunner == null)
                {
                    throw new Exception(string.Format("Test runner '{0}' not found.", cmdLine.TestRunner));
                }
            }

            return selectedTestRunner;
        }

        private IEnumerable<FileInfo> DiscoverManuallySpecifiedBinaries(ManualConfiguration cmdLine)
        {
            foreach (var path in cmdLine.TestBinaries)
            {
                var fi = new FileInfo(path);

                //currently I'm only aware of OpenCover needing the pdbs for connecting unittests and their subjects, 
                //but chances are any coverage engine would need it as well (if we ever get to implement anything else)
                if (!PdbFileExists(fi))
                {
                    throw new Exception(string.Format("Test binary '{0}' doesn't have its pdb file present.", fi.FullName));
                }

                if (fi.Exists)
                {
                    yield return fi;
                }
                else
                {
                    throw new Exception(string.Format("Test binary '{0}' doesn't exist.", fi.FullName));
                }
            }
        }

        private IEnumerable<Tuple<string, FileInfo>> AutoDiscoverBinaries(DirectoryInfo modelDirectory, IReadOnlyCollection<ITestRunner> allowedTestRunners)
        {
            foreach (var fi in modelDirectory.EnumerateFiles("*.dll", SearchOption.AllDirectories))
            {
                if (!PdbFileExists(fi))
                {
                    continue;
                }

                foreach (var testRunner in allowedTestRunners)
                {
                    if (testRunner.IsTestBinary(fi))
                    {
                        yield return Tuple.Create(testRunner.Name, fi);
                    }
                }
            }
        }

        private static bool PdbFileExists(FileInfo assemblyFile)
        {
            var pdbPath = Path.ChangeExtension(assemblyFile.FullName, "pdb");
            var pdbExists = File.Exists(pdbPath);
            return pdbExists;
        }
    }
}
