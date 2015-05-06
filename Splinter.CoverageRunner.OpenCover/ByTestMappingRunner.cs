using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using log4net;

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
        InitialCoverage RunTestsAndMapMethods(FileInfo openCoverExe, TestsToRun testsToRun);
    }

    public class ByTestMappingRunner : IByTestMappingRunner
    {
        private readonly ILog log;

        public ByTestMappingRunner(ILog log)
        {
            this.log = log;
        }

        public InitialCoverage RunTestsAndMapMethods(FileInfo openCoverExe, TestsToRun testsToRun)
        {
            foreach (var testBinary in testsToRun.TestBinaries)
            {
                Run(openCoverExe, testsToRun.TestRunner, testBinary);
            }



            return null;
        }

        //.\packages\OpenCover.4.5.3723\OpenCover.Console.exe 
        //-register:user 
        //"-filter:+[Bom]* -[*BomTest]*" 
        //"-target:.\packages\NUnit.Runners.2.6.4\tools\nunit-console-x86.exe" 
        //"-targetargs:/noshadow .\BomTest\bin\Debug\BomTest.dll" 
        //-coverbytest:*\BomTest.dll

        private void Run(FileInfo openCoverExe, ITestRunner runner, FileInfo testBinary)
        {
            using (var sd = new ShadowDirectory(testBinary.Directory))
            {
                var shadowTestBinary = Path.Combine(sd.Shadow.FullName, testBinary.Name);

                var testRunnerProcessInfo = runner.GetProcessInfoToRunTests(new FileInfo(shadowTestBinary));

                var register = "-register:user";
                var target = string.Format("\"-target:{0}\"", CmdLine.EncodeArgument(testRunnerProcessInfo.FileName));
                var targetArgs = string.Format("\"-targetargs:{0}\"", CmdLine.EncodeArgument(testRunnerProcessInfo.Arguments));
                var filter = string.Format("\"-filter:+[*]* -[{0}]*\"", CmdLine.EncodeArgument(Path.GetFileNameWithoutExtension(testBinary.Name)));
                var coverbytest = string.Format("\"-coverbytest:*\\{0}\"", CmdLine.EncodeArgument(testBinary.Name));

                var openCoverProcessInfo = new ProcessStartInfo(
                    fileName: openCoverExe.FullName,
                    arguments: string.Join(" ", register, target, targetArgs, filter, coverbytest));

                var p = Process.Start(openCoverProcessInfo);
                p.WaitForExit();


            }

        }

    }
}
