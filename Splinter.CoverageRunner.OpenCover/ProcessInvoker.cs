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
        XDocument RunTestsAndGetOutput(FileInfo openCoverExe, DirectoryInfo modelDirectory, ITestRunner testRunner, FileInfo testBinary, out string shadowDirFullName);
    }

    public class ProcessInvoker : IProcessInvoker
    {
        public const string outputFileName = "opencover.results.xml";

        private readonly ILog log;

        private readonly IExecutableUtils executableUtils;

        public ProcessInvoker(ILog log, IExecutableUtils executableUtils)
        {
            this.log = log;
            this.executableUtils = executableUtils;
        }

        public XDocument RunTestsAndGetOutput(FileInfo openCoverExe, DirectoryInfo modelDirectory, ITestRunner testRunner, FileInfo testBinary, out string shadowDirFullName)
        {
            var runnerName = testRunner.Name;

            using (var sd = new ShadowDirectory(this.log, modelDirectory))
            {
                try
                {
                    this.log.InfoFormat("Running tests contained in '{0}', to extract test-subject mapping.", testBinary.Name);

                    var openCoverProcessInfo = this.CreateOpenCoverStartInfo(openCoverExe, testRunner, testBinary, sd);
                    var exitCode = this.executableUtils.RunProcessAndWaitForExit(openCoverProcessInfo, sd.ShadowId);

                    if (exitCode != 0)
                    {
                        //TODO: no logs to check atm
                        throw new Exception("Test runner exitcode != 0. Check the logs.");
                    }

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

        private ProcessStartInfo CreateOpenCoverStartInfo(FileInfo openCoverExe, ITestRunner testRunner, FileInfo testBinary, ShadowDirectory sd)
        {

            var shadowTestBinary = sd.GetEquivalentShadowPath(testBinary);

            var testRunnerProcessInfo = testRunner.GetProcessInfoToRunTests(sd.Shadow, shadowTestBinary);

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
