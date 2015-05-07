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

namespace Splinter.CoverageRunner.OpenCover
{
    public interface IProcessInvoker
    {
        /// <summary>
        /// Implementation of ICoverageRunner.GetInitialCoverage
        /// </summary>
        IReadOnlyCollection<TestSubjectMethodRef> RunTestsAndMapMethods(FileInfo openCoverExe, TestsToRun testsToRun);
    }

    public class ProcessInvoker : IProcessInvoker
    {
        public const string outputFileName = "opencover.results.xml";

        private readonly ILog log;

        public ProcessInvoker(ILog log)
        {
            this.log = log;
        }

        public IReadOnlyCollection<TestSubjectMethodRef> RunTestsAndMapMethods(FileInfo openCoverExe, TestsToRun testsToRun)
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
            var dict = new ConcurrentDictionary<MethodRef, IImmutableSet<MethodRef>>();

            partialCoverages.SelectMany(i => i)
                .ForAll(subject =>
                    dict.AddOrUpdate(
                        subject.Method,
                        subject.TestMethods,
                        (_, existing) => existing.Union(subject.TestMethods)));

            return dict.Select(kvp => new TestSubjectMethodRef(kvp.Key, kvp.Value)).ToArray();
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
    }
}
