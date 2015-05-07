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
        XDocument RunTestsAndGetOutput(FileInfo openCoverExe, ITestRunner testRunner, FileInfo testBinary, out string shadowDirFullName);
    }

    public class ProcessInvoker : IProcessInvoker
    {
        public const string outputFileName = "opencover.results.xml";

        private readonly ILog log;

        public ProcessInvoker(ILog log)
        {
            this.log = log;
        }

        public XDocument RunTestsAndGetOutput(FileInfo openCoverExe, ITestRunner testRunner, FileInfo testBinary, out string shadowDirFullName)
        {
            var runnerName = testRunner.Name;

            using (var sd = new ShadowDirectory(testBinary.Directory))
            {
                try
                {
                    this.log.InfoFormat("Running tests contained in '{0}', to extract test-subject mapping.", testBinary.Name);

                    var openCoverProcessInfo = this.CreateOpenCoverStartInfo(openCoverExe, testRunner, testBinary, sd);

                    this.RunOpenCoverProcess(runnerName, openCoverProcessInfo);

                    //not using DirectoryInfo as the out value because the directory won't exist by the time
                    shadowDirFullName = sd.Shadow.FullName;
                    var resultsXml = XDocument.Load(Path.Combine(shadowDirFullName, outputFileName));
                    return resultsXml;
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

        private ProcessStartInfo CreateOpenCoverStartInfo(FileInfo openCoverExe, ITestRunner testRunner, FileInfo testBinary, ShadowDirectory sd)
        {
            var shadowTestBinary = Path.Combine(sd.Shadow.FullName, testBinary.Name);

            var testRunnerProcessInfo = testRunner.GetProcessInfoToRunTests(new FileInfo(shadowTestBinary));

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
