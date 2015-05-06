using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Security.Cryptography;

using log4net;

using OpenCover.Framework;
using OpenCover.Framework.Model;

using Splinter.Contracts;
using Splinter.Contracts.DTOs;
using Splinter.Utils;

namespace Splinter.CoverageRunner.OpenCover
{
    public interface IByTestMappingRunner
    {
        /// <summary>
        /// Implementation of ICoverageRunner.GetInitialCoverage
        /// </summary>
        IReadOnlyCollection<TestSubjectMethod> RunTestsAndMapMethods(FileInfo openCoverExe, TestsToRun testsToRun);
    }

    public class ByTestMappingRunner : IByTestMappingRunner
    {
        const string outputFileName = "opencover.results.xml";

        private readonly ILog log;

        public ByTestMappingRunner(ILog log)
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

            var coverage = partialCoverages.SelectMany(i => i);

            return coverage.ToArray();
        }

        private void RunOpenCoverProcess(string runnerName, ProcessStartInfo openCoverProcessInfo)
        {
            using (var p = Process.Start(openCoverProcessInfo))
            {
                //TODO: redirecting doesn't work, probably because opencover isn't redirecting
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

            var serializer = new XmlSerializer(
                typeof(CoverageSession),
                new[] { typeof(Module), typeof(global::OpenCover.Framework.Model.File), typeof(Class) });

            var testBinaryHash = this.HashFile(testBinary);

            var originalDir = testBinary.DirectoryName;

            var results = new List<TestSubjectMethod>();

            using (var fs = resultsXmlFile.OpenRead())
            {
                using (var reader = new StreamReader(fs, new UTF8Encoding()))
                {
                    var session = (CoverageSession)serializer.Deserialize(reader);

                    var testMethods = new Dictionary<uint, Splinter.Contracts.DTOs.Method>();

                    var testModule = session.Modules.Single(m => testBinaryHash.SequenceEqual(HashFromString(m.ModuleHash)));

                    foreach (var trackedMethod in testModule.TrackedMethods)
                    {
                        var method = new Splinter.Contracts.DTOs.Method
                        {
                            Assembly = testBinary,
                            FullName = trackedMethod.Name,
                        };

                        testMethods.Add(trackedMethod.UniqueId, method); //uid
                    }

                    foreach (var module in session.Modules)
                    {
                        var shadowAssembly = new FileInfo(module.FullName);
                        if (shadowAssembly.FullName.StartsWith(shadowDir.FullName, StringComparison.OrdinalIgnoreCase))
                        {
                            var relativePath = shadowAssembly.FullName.Substring(shadowDir.FullName.Length);
                            //the file path from the original directory is the one we care about
                            var originalAssembly = new FileInfo(Path.Combine(originalDir, relativePath));

                            foreach (var cl in module.Classes)
                            {
                                foreach (var m in cl.Methods)
                                {
                                    var subject = new TestSubjectMethod
                                    {
                                        Assembly = originalAssembly,
                                        FullName = m.Name,
                                    };

                                    foreach (var trackedMethodRef in m.MethodPoint.TrackedMethodRefs.EmptyIfNull())
                                    {
                                        Splinter.Contracts.DTOs.Method testMethod;
                                        if (testMethods.TryGetValue(trackedMethodRef.UniqueId, out testMethod))
                                        {
                                            subject.TestMethods.Add(testMethod);
                                        }
                                    }

                                    if (subject.TestMethods.Count > 0)
                                    {
                                        results.Add(subject);
                                    }
                                }
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
