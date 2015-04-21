﻿using System;
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
        /// <returns></returns>
        TestsToRun DiscoverTestBinaries(ManualConfiguration cmdLine, IReadOnlyCollection<ITestRunner> testRunners);
    }

    public class TestsDiscoverer : ITestsDiscoverer
    {
        ILog log;

        public TestsDiscoverer(ILog log)
        {
            this.log = log;
        }

        public TestsToRun DiscoverTestBinaries(ManualConfiguration cmdLine, IReadOnlyCollection<ITestRunner> testRunners)
        {
            var testBinariesByRunner = new Dictionary<string, List<FileInfo>>();
            ITestRunner selectedTestRunner = null;

            if (!string.IsNullOrWhiteSpace(cmdLine.TestRunner))
            {
                selectedTestRunner = testRunners.SingleOrDefault(r => r.Name.Equals(cmdLine.TestRunner, StringComparison.OrdinalIgnoreCase));
                if (selectedTestRunner == null)
                {
                    throw new Exception(string.Format("Test runner '{0}' not found.", cmdLine.TestRunner));
                }
            }

            if (cmdLine.TestBinaries.EmptyIfNull().Any())
            {
                var list = new List<FileInfo>();
                //when particular binaries are specified, the runner is also already specified. This fact is validated already at this point.
                testBinariesByRunner[cmdLine.TestRunner] = list;

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
                        list.Add(fi);
                    }
                    else
                    {
                        throw new Exception(string.Format("Test binary '{0}' doesn't exist.", fi.FullName));
                    }
                }
            }
            else
            {
                //if the user specified a particular runner, we don't use any other
                var allowedTestRunners = testRunners;
                if (selectedTestRunner != null)
                {
                    allowedTestRunners = new[] { selectedTestRunner };
                }

                foreach (var fi in new DirectoryInfo(Environment.CurrentDirectory).EnumerateFiles("*.dll"))
                {
                    if (!PdbFileExists(fi))
                    {
                        continue;
                    }

                    foreach (var testRunner in allowedTestRunners)
                    {
                        if (testRunner.IsTestBinary(fi))
                        {
                            List<FileInfo> list;
                            if (!testBinariesByRunner.TryGetValue(testRunner.Name, out list))
                            {
                                list = new List<FileInfo>();
                                testBinariesByRunner.Add(testRunner.Name, list);
                            }

                            list.Add(fi);
                        }
                    }
                }

                if (testBinariesByRunner.Count == 0)
                {
                    throw new Exception("No test binaries found");
                }
                else if (testBinariesByRunner.Count > 1)
                {
                    var found = string.Join(Environment.NewLine, testBinariesByRunner.Select(p => p.Key + ": " + string.Join(", ", p.Value.Select(fi => fi.Name))));

                    throw new Exception("More than one type of testrunner-compatible test binaries found during autodiscovery, please specify the runner and/or the binaries explicitly: " + found);
                }

                selectedTestRunner = testRunners.Single(r => r.Name.Equals(testBinariesByRunner.Keys.Single(), StringComparison.OrdinalIgnoreCase));
            }

            var ttr = new TestsToRun
            {
                TestBinaries = testBinariesByRunner.Values.Single(),
                TestRunner = selectedTestRunner,
            };

            return ttr;
        }

        private static bool PdbFileExists(FileInfo assemblyFile)
        {
            var pdbPath = Path.ChangeExtension(assemblyFile.FullName, "pdb");
            var pdbExists = File.Exists(pdbPath);
            return pdbExists;
        }
    }
}