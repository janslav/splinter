using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Diagnostics;
using System.Security.Cryptography;

using log4net;

using Splinter.Contracts;
using Splinter.Contracts.DTOs;
using Splinter.Utils;

using SplinterMethod = Splinter.Contracts.DTOs.Method;

namespace Splinter.CoverageRunner.OpenCover
{
    public interface ISubjectTestMappingRunner
    {
        /// <summary>
        /// Implementation of ICoverageRunner.GetInitialCoverage
        /// </summary>
        IReadOnlyCollection<TestSubjectMethod> RunTestsAndMapMethods(FileInfo openCoverExe, TestsToRun testsToRun);
    }

    public class SubjectTestMappingRunner : ISubjectTestMappingRunner
    {
        const string outputFileName = "opencover.results.xml";

        private readonly ILog log;

        public SubjectTestMappingRunner(ILog log)
        {
            this.log = log;
        }

        public IReadOnlyCollection<TestSubjectMethod> RunTestsAndMapMethods(FileInfo openCoverExe, TestsToRun testsToRun)
        {
            var partialCoverages = testsToRun.TestBinaries
                .AsParallel()
                .Select(testBinary =>
                {
                    var runnerName = testsToRun.TestRunner.Name;

                    using (var sd = new ShadowDirectory(testBinary.Directory))
                    {
                        try
                        {
                            this.log.InfoFormat("Running tests contained in '{0}', to extract test-subject mapping.", testBinary.Name);

                            var openCoverProcessInfo = this.CreateOpenCoverStartInfo(openCoverExe, testsToRun, testBinary, sd);

                            this.RunOpenCoverProcess(runnerName, openCoverProcessInfo);

                            return this.ExtractMapping(testBinary, sd.Shadow);
                        }
                        catch (Exception e)
                        {
                            this.log.Error(string.Format("Something went wrong while running tests from '{0}'.", testBinary.Name), e);
                            throw;
                        }
                        finally
                        {
                            this.log.InfoFormat("Done running tests contained in '{0}'.", testBinary.Name);
                        }
                    }
                });

            //one subject method can be tested by tests from several assemblies, so here we merge the lists.
            var dict = new ConcurrentDictionary<SplinterMethod, IImmutableSet<SplinterMethod>>();

            partialCoverages.SelectMany(i => i)
                .ForAll(subject =>
                    dict.AddOrUpdate(
                        subject.Method,
                        subject.TestMethods,
                        (_, existing) => existing.Union(subject.TestMethods)));

            return dict.Select(kvp => new TestSubjectMethod(kvp.Key, kvp.Value)).ToArray();
        }

        private void RunOpenCoverProcess(string runnerName, ProcessStartInfo openCoverProcessInfo)
        {
            using (var p = Process.Start(openCoverProcessInfo))
            {
                //TODO: redirecting doesn't work
                p.OutputDataReceived += (_, e) => this.log.Debug(runnerName + ": " + e.Data);
                p.ErrorDataReceived += (_, e) => this.log.Warn(runnerName + ": " + e.Data);

                p.WaitForExit();

                if (p.ExitCode != 0)
                {
                    //TODO: no logs to check atm
                    throw new Exception("Test runner exitcode != 0. Check the logs.");
                }
            }
        }

        private ProcessStartInfo CreateOpenCoverStartInfo(FileInfo openCoverExe, TestsToRun testsToRun, FileInfo testBinary, ShadowDirectory sd)
        {
            var shadowTestBinary = Path.Combine(sd.Shadow.FullName, testBinary.Name);

            var testRunnerProcessInfo = testsToRun.TestRunner.GetProcessInfoToRunTests(new FileInfo(shadowTestBinary));

            var staticArgs = "-register:user -returntargetcode -mergebyhash -output:" + outputFileName; // -log:All
            var target = string.Format("\"-target:{0}\"", CmdLine.EncodeArgument(testRunnerProcessInfo.FileName));
            var targetArgs = string.Format("\"-targetargs:{0}\"", CmdLine.EncodeArgument(testRunnerProcessInfo.Arguments));
            var filter = string.Format("\"-filter:+[*]* -[{0}]*\"", CmdLine.EncodeArgument(Path.GetFileNameWithoutExtension(testBinary.Name)));
            var coverbytest = string.Format("\"-coverbytest:*\\{0}\"", CmdLine.EncodeArgument(testBinary.Name));

            var openCoverProcessInfo = new ProcessStartInfo(
                fileName: openCoverExe.FullName,
                arguments: string.Join(" ", staticArgs, target, targetArgs, filter, coverbytest))
            {
                WorkingDirectory = sd.Shadow.FullName,
                //UseShellExecute = false,
                CreateNoWindow = true,
                //RedirectStandardOutput = true,
                //RedirectStandardError = true
            };
            return openCoverProcessInfo;
        }

        private IReadOnlyCollection<TestSubjectMethod> ExtractMapping(FileInfo testBinary, DirectoryInfo shadowDir)
        {
            var resultsXmlFile = new FileInfo(Path.Combine(shadowDir.FullName, outputFileName));

            var resultsXml = XDocument.Load(resultsXmlFile.FullName);
            var session = resultsXml.Root;

            var testBinaryHash = this.HashFile(testBinary);

            var originalDir = testBinary.DirectoryName;

            var results = new List<TestSubjectMethod>();


            var testMethods = new Dictionary<uint, SplinterMethod>();

            var testModule = session.Element("Modules").Elements("Module")
                .Single(m => testBinaryHash.SequenceEqual(HashFromString(m.Attribute("hash").Value)));

            foreach (var trackedMethodEl in testModule.Element("TrackedMethods").Elements("TrackedMethod"))
            {
                var method = new SplinterMethod(testBinary, trackedMethodEl.Attribute("name").Value);

                testMethods.Add((uint) trackedMethodEl.Attribute("uid"), method);
            }

            foreach (var moduleEl in session.Element("Modules").Elements("Module"))
            {
                var shadowAssembly = new FileInfo(moduleEl.Element("FullName").Value);
                if (shadowAssembly.FullName.StartsWith(shadowDir.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    var relativePath = shadowAssembly.FullName.Substring(shadowDir.FullName.Length);
                    //the file path from the original directory is the one we care about
                    var originalAssembly = new FileInfo(Path.Combine(originalDir, relativePath));

                    foreach (var classEl in moduleEl.Element("Classes").Elements("Class"))
                    {
                        foreach (var metodEl in classEl.Element("Methods").Elements("Method"))
                        {
                            var list = new HashSet<SplinterMethod>();

                            foreach (var trackedMethodRefEl in metodEl.Descendants("TrackedMethodRef"))
                            {
                                SplinterMethod testMethod;
                                if (testMethods.TryGetValue((uint) trackedMethodRefEl.Attribute("uid"), out testMethod))
                                {
                                    list.Add(testMethod);
                                }
                            }

                            if (list.Count > 0)
                            {
                                var subjectMethod = new SplinterMethod(originalAssembly, metodEl.Element("Name").Value);
                                var subject = new TestSubjectMethod(subjectMethod, list);
                                results.Add(subject);
                            }
                        }
                    }
                }
            }

            return results;
        }

        private byte[] HashFile(FileInfo file)
        {
            using (var sr = file.OpenRead())
            {
                using (var prov = new SHA1CryptoServiceProvider())
                {
                    return prov.ComputeHash(sr);
                }
            }
        }

        private byte[] HashFromString(string dashDelimitedHexNumbers)
        {
            return dashDelimitedHexNumbers.Split('-')
                .Select(ch => byte.Parse(ch, System.Globalization.NumberStyles.HexNumber))
                .ToArray();
        }
    }
}
