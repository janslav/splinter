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
    /// <summary>
    /// Runs the OpenCover process
    /// </summary>
    public interface IProcessInvoker
    {
        /// <summary>
        /// Runs opencover.exe and reads its xml output
        /// </summary>
        XDocument RunTestsAndGetOutput(FileInfo openCoverExe, DirectoryInfo modelDirectory, ITestRunner testRunner, FileInfo testBinary, out string shadowDirFullName);
    }

    /// <summary>
    /// Runs the OpenCover process
    /// </summary>
    public class ProcessInvoker : IProcessInvoker
    {
        public const string outputFileName = "opencover.results.xml";

        private readonly ILog log;

        private readonly IExecutableUtils executableUtils;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessInvoker"/> class.
        /// </summary>
        public ProcessInvoker(ILog log, IExecutableUtils executableUtils)
        {
            this.log = log;
            this.executableUtils = executableUtils;
        }

        /// <summary>
        /// Runs opencover.exe and reads its xml output
        /// </summary>
        public XDocument RunTestsAndGetOutput(FileInfo openCoverExe, DirectoryInfo modelDirectory, ITestRunner testRunner, FileInfo testBinary, out string shadowDirFullName)
        {
            var runnerName = testRunner.Name;
            const string operationId = "OpenCover";

            using (var sd = new ShadowDirectory(this.log, modelDirectory, operationId))
            {
                try
                {
                    this.log.InfoFormat("Running tests contained in '{0}', to extract test-subject mapping.", testBinary.Name);

                    var openCoverProcessInfo = this.CreateOpenCoverStartInfo(openCoverExe, testRunner, testBinary, sd);
                    var exitCode = this.executableUtils.RunProcessAndWaitForExit(openCoverProcessInfo, operationId);

                    if (exitCode != 0)
                    {
                        //TODO: parse the usual suspects (some tests failed, no tests run)
                        throw new Exception("OpenCover runner exitcode != 0. Check the logs.");
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
