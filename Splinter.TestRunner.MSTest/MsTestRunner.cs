namespace Splinter.TestRunner.MsTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using log4net;

    using Mono.Cecil;
    using Mono.Options;

    using Splinter.Contracts;
    using Splinter.Contracts.DTOs;
    using Splinter.Utils;
    using Splinter.Utils.Cecil;

    /// <summary>
    /// Runs tests implemented using the MsTest (visual studio unit testing) framework.
    /// </summary>
    public class MsTestRunner : TypeBasedEqualityImplementation, ITestRunner
    {
        private readonly ILog log;

        private readonly ICodeCache codeCache;

        private readonly IExecutableUtils executableUtils;

        private FileInfo msTestExe;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsTestRunner"/> class.
        /// </summary>
        public MsTestRunner(ILog log, ICodeCache codeCache, IExecutableUtils executableUtils)
        {
            this.log = log;
            this.codeCache = codeCache;
            this.executableUtils = executableUtils;
        }

        /// <summary>
        /// Sets up the command line options.
        /// </summary>
        public void SetupCommandLineOptions(OptionSet options)
        {
        }

        /// <summary>
        /// Returns true if the mstest.exe can be located.
        /// </summary>
        public bool IsReady(out string unavailableMessage)
        {
            try
            {
                var paths = this.GetMsExeSearchPaths();

                this.msTestExe = this.executableUtils.FindExecutable("mstest.exe", paths);
                var exitCode = this.executableUtils.RunProcessAndWaitForExit(this.msTestExe, "MsTest Discovery", new[] { "/help" }).ExitCode;

                if (exitCode != 0)
                {
                    unavailableMessage = "Discovery run of mstest.exe returned non-zero code. Please check the log.";
                    return false;
                }

                unavailableMessage = null;
                return true;
            }
            catch (Exception e)
            {
                unavailableMessage = e.Message;
                return false;
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get { return "MsTest"; }
        }

        /// <summary>
        /// Determines whether the specified binary contains tests.
        /// </summary>
        public bool IsTestBinary(FileInfo binary)
        {
            var assembly = AssemblyDefinition.ReadAssembly(binary.FullName);

            var referencingFramework = assembly.MainModule.AssemblyReferences.Where(a => a.Name.Equals("Microsoft.VisualStudio.QualityTools.UnitTestFramework"));

            //if (referencingFramework.Any())
            //{
            //    var classes = assembly.GetTypes();
            //    var testClasses = classes.Where(c => c.GetCustomAttributesData().Any(a => a.AttributeType.Name.Equals("TestClass")));

            //    return testClasses.Any();
            //}

            return referencingFramework.Any();
        }

        //code stolen from NinjaTurtles msbuild runner
        private IReadOnlyCollection<string> GetMsExeSearchPaths()
        {
            var searchPath = new List<string>();
            string programFilesFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86Folder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            this.AddSearchPathsForVisualStudioVersions(searchPath, programFilesFolder);
            if (!string.IsNullOrEmpty(programFilesX86Folder))
            {
                this.AddSearchPathsForVisualStudioVersions(searchPath, programFilesX86Folder);
            }

            return searchPath;
        }

        private void AddSearchPathsForVisualStudioVersions(ICollection<string> searchPath, string baseFolder)
        {
            for (int visualStudioVersion = 15; visualStudioVersion >= 10; visualStudioVersion--)
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

        /// <summary>
        /// Gets the process information to run all tests from a binary.
        /// </summary>
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
                    string.Format("/testcontainer:\"{0}\"", escapedTestBinary))
                    {
                        WorkingDirectory = workingDirectory.FullName
                    };

            return r;
        }

        /// <summary>
        /// Gets the process information to run one test from a binary.
        /// </summary>
        public ProcessStartInfo GetProcessInfoToRunTest(DirectoryInfo workingDirectory, FileInfo testBinary, uint methodMetadataToken)
        {
            var testMethodDef = this.codeCache.GetAssemblyDefinition(testBinary).GetMethodByMetaDataToken(methodMetadataToken);

            var escapedTestBinary = CmdLine.EncodeArgument(testBinary.FullName);

            var escapedTestName = CmdLine.EncodeArgument(string.Join(".",
                testMethodDef.DeclaringType.Namespace, testMethodDef.DeclaringType.Name, testMethodDef.Name));

            var r = new ProcessStartInfo(
                    this.msTestExe.FullName,
                    string.Format("/testcontainer:\"{0}\" /test:\"{1}\"", escapedTestBinary, escapedTestName))
                    {
                        WorkingDirectory = workingDirectory.FullName
                    };

            return r;
        }

        private static readonly Regex ResultsFileLineRe = new Regex(@"Results file:\s*(?<fileName>.+)", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Extracts the test methods with additional metadata (such as test run time) using the console output of the coverage process.
        /// </summary>
        public IReadOnlyCollection<TestMethodRef> ParseTestMethodsList(FileInfo testBinary, string testRunConsoleOut)
        {
            //we look for the line "Results file: filepath/of/xml/result" in the console output, then we parse the file at the path
            string resultsXmlFileName = null;

            var reader = new StringReader(testRunConsoleOut);
            string line;

            while (null != (line = reader.ReadLine()))
            {
                var match = ResultsFileLineRe.Match(line);
                if (match.Success)
                {
                    resultsXmlFileName = match.Groups["fileName"].Value;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(resultsXmlFileName))
            {
                throw new Exception("Could not extract mstest result xml file location from the process output.");
            }

            var resultsXmlFile = new FileInfo(resultsXmlFileName);

            if (!resultsXmlFile.Exists)
            {
                throw new Exception(string.Format("Could not find mstest result xml file at '{0}'.", resultsXmlFileName));
            }

            var resultsXmlDoc = XDocument.Load(resultsXmlFile.FullName);

            if ((resultsXmlDoc == null) || (resultsXmlDoc.Root == null))
            {
                throw new Exception(string.Format("Failed to read XML from '{0}'.", resultsXmlFileName));
            }

            var assemblyDef = this.codeCache.GetAssemblyDefinition(testBinary);

            var ns = resultsXmlDoc.Root.GetDefaultNamespace().NamespaceName;

            //there is a TestDefinitions element with the method/class names and then Results with the start/finish times
            var unitTestsByDefinitionId = resultsXmlDoc.Root.Element(XName.Get("TestDefinitions", ns)).Elements(XName.Get("UnitTest", ns))
                .ToDictionary(
                unitTestElement => unitTestElement.Attribute("id").Value,
                unitTestElement =>
                {
                    var testMethodElement = unitTestElement.Element(XName.Get("TestMethod", ns));
                    var classFullName = testMethodElement.Attribute("className").Value;
                    var methodName = testMethodElement.Attribute("name").Value;

                    var method = assemblyDef.GetMethodByClassAndMethodName(classFullName, methodName);

                    return Tuple.Create(method.MetadataToken.ToUInt32(), TimeSpan.Zero);
                });

            foreach (var unitTestResultElement in resultsXmlDoc.Root.Element(XName.Get("Results", ns)).Elements(XName.Get("UnitTestResult", ns)))
            {
                var id = unitTestResultElement.Attribute("testId").Value;
                var startTime = (DateTime)unitTestResultElement.Attribute("startTime");
                var endTime = (DateTime)unitTestResultElement.Attribute("endTime");
                var runTime = endTime - startTime;

                var t = unitTestsByDefinitionId[id];
                if (t.Item2 < runTime)
                {
                    unitTestsByDefinitionId[id] = Tuple.Create(t.Item1, runTime);
                }
            }

            return unitTestsByDefinitionId.Values.Select(t => new TestMethodRef(new MethodRef(testBinary, t.Item1), this, t.Item2)).ToArray();
        }
    }
}
