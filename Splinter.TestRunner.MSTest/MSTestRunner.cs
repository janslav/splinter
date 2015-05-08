using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using System.Diagnostics;

using NinjaTurtles;

using Splinter.Contracts.DTOs;
using Splinter.Contracts;
using Splinter.Utils;
using Splinter.Utils.Cecil;

namespace Splinter.TestRunner.MSTest
{
    public class MSTestRunner : ITestRunner, IPluginFactory<ITestRunner>
    {
        private log4net.ILog log;

        private FileInfo msTestExe;

        ITestRunner IPluginFactory<ITestRunner>.GetPlugin(log4net.ILog log)
        {
            this.log = log;
            return this;
        }

        public bool IsReady(out string unavailableMessage)
        {
            try
            {
                var paths = this.GetMsExeSearchPaths();

                var process = ConsoleProcessFactory.CreateProcess("mstest.exe", "", paths.ToArray());

                this.msTestExe = new FileInfo(process.StartInfo.FileName);

                if (process != null)
                {
                    process.Start();
                    process.WaitForExit();

                    unavailableMessage = null;
                    return true;
                }

                throw new Exception("MsTest.exe not found.");
            }
            catch (Exception e)
            {
                unavailableMessage = e.Message;
                return false;
            }
        }

        public string Name
        {
            get { return "MsTest"; }
        }

        public bool IsTestBinary(FileInfo binary)
        {
            var assembly = Assembly.ReflectionOnlyLoadFrom(binary.FullName);

            var referencingFramework = assembly.GetReferencedAssemblies().Where(a => a.Name.Equals("Microsoft.VisualStudio.QualityTools.UnitTestFramework"));

            //if (referencingFramework.Any())
            //{
            //    var classes = assembly.GetTypes();
            //    var testClasses = classes.Where(c => c.GetCustomAttributesData().Any(a => a.AttributeType.Name.Equals("TestClass")));

            //    return testClasses.Any();
            //}

            return referencingFramework.Any();
        }

        //code stolen from NinjaTurtles msbuild runner
        private IEnumerable<string> GetMsExeSearchPaths()
        {
            var searchPath = new List<string>();
            string programFilesFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86Folder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            AddSearchPathsForVisualStudioVersions(searchPath, programFilesFolder);
            if (!string.IsNullOrEmpty(programFilesX86Folder))
            {
                AddSearchPathsForVisualStudioVersions(searchPath, programFilesX86Folder);
            }

            return searchPath;
        }

        private void AddSearchPathsForVisualStudioVersions(ICollection<string> searchPath, string baseFolder)
        {
            for (int visualStudioVersion = 10; visualStudioVersion <= 15; visualStudioVersion++)
            {
                searchPath.Add(Path.Combine(baseFolder,
                    string.Format("Microsoft Visual Studio {0}.0\\Common7\\IDE", visualStudioVersion)));
            }
        }

        //from gallio:
        ///// <summary>
        ///// Finds the path of a particular version of MSTest.
        ///// </summary>
        ///// <param name="visualStudioVersion">The visual studio version (eg. "8.0" or "9.0").</param>
        ///// <returns>The full path of the MSTest.exe program, or null if not found.</returns>
        //public static string FindMSTestPathForVisualStudioVersion(string visualStudioVersion)
        //{
        //    string result = null;

        //    RegistryUtils.TryActionOnOpenSubKeyWithBitness(
        //        ProcessorArchitecture.None, RegistryHive.LocalMachine,
        //        @"SOFTWARE\Microsoft\VisualStudio\" + visualStudioVersion,
        //        key =>
        //        {
        //            string visualStudioInstallDir = (string)key.GetValue("InstallDir");
        //            if (visualStudioInstallDir != null)
        //            {
        //                string msTestExecutablePath = Path.Combine(visualStudioInstallDir, "MSTest.exe");
        //                if (File.Exists(msTestExecutablePath))
        //                {
        //                    result = msTestExecutablePath;
        //                    return true;
        //                }
        //            }

        //            return false;
        //        });

        //    return result;
        //}


        public ProcessStartInfo GetProcessInfoToRunTests(DirectoryInfo workingDirectory, FileInfo testBinary)
        {

            //(TestDirectory testDirectory, string testAssemblyLocation, IEnumerable<string> testsToRun)
            //testAssemblyLocation = Path.Combine(testDirectory.FullName, Path.GetFileName(testAssemblyLocation));
            //string testArguments = string.Join(" ", testsToRun.Select(t => string.Format("/test:\"{0}\"", t)));
            //string arguments = string.Format("/testcontainer:\"{0}\" {1}",
            //                                 testAssemblyLocation, testArguments);

            var escapedTestBinary = CmdLine.EncodeArgument(testBinary.FullName);

            var r = new ProcessStartInfo(
                    this.msTestExe.FullName,
                    string.Format("/noisolation /testcontainer:\"{0}\"", escapedTestBinary))
                    {
                        WorkingDirectory = workingDirectory.FullName
                    };

            return r;
        }

        public ProcessStartInfo GetProcessInfoToRunTest(DirectoryInfo workingDirectory, FileInfo testBinary, string methodFullName)
        {
            var testMethodDef = CodeCache.Instance.GetAssembly(testBinary).GetMethodByFullName(methodFullName);

            var escapedTestBinary = CmdLine.EncodeArgument(testBinary.FullName);

            var escapedTestName = CmdLine.EncodeArgument(string.Join(".",
                testMethodDef.DeclaringType.Namespace, testMethodDef.DeclaringType.Name, testMethodDef.Name));

            var r = new ProcessStartInfo(
                    this.msTestExe.FullName,
                    string.Format("/noisolation /testcontainer:\"{0}\" /test:\"{1}\"", escapedTestBinary, escapedTestName))
                    {
                        WorkingDirectory = workingDirectory.FullName
                    };

            return r;
        }
    }
}
