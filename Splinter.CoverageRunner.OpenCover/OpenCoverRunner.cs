﻿namespace Splinter.CoverageRunner.OpenCover
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using log4net;

    using Microsoft.Win32;

    using Mono.Options;

    using Splinter.Contracts;
    using Splinter.Contracts.DTOs;
    using Splinter.Utils;

    /// <summary>
    /// The OpenCover coverage plugin
    /// </summary>
    public class OpenCoverRunner : TypeBasedEqualityImplementation, ICoverageRunner
    {
        private const string OpenCoverRegKey = @"SOFTWARE\OpenCover\";
        private const string OpenCoverRegKeyWow6432 = @"SOFTWARE\Wow6432Node\OpenCover\";
        private const string OpenCoverRegValue = "Path";
        private const string OpenCoverExeName = "OpenCover.Console.exe";

        private readonly ILog log;

        private readonly IProcessInvoker invoker;

        private readonly ISubjectTestMappingParser mappingParser;

        private readonly IExecutableUtils executableUtils;

        private FileInfo ncoverExe;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenCoverRunner"/> class.
        /// </summary>
        public OpenCoverRunner(ILog log, IProcessInvoker invoker, ISubjectTestMappingParser mappingParser, IExecutableUtils executableUtils)
        {
            this.log = log;
            this.invoker = invoker;
            this.mappingParser = mappingParser;
            this.executableUtils = executableUtils;
        }

        /// <summary>
        /// Sets up the command line options.
        /// </summary>
        /// <param name="options"></param>
        public void SetupCommandLineOptions(OptionSet options)
        {
        }

        /// <summary>
        /// Returns true if opencover.exe can be located.
        /// </summary>
        public bool IsReady(out string unavailableMessage)
        {
            try
            {
                var paths = new[] { this.GetOpenCoverExeInstallationPath() };

                this.ncoverExe = this.executableUtils.FindExecutable(OpenCoverExeName, paths);
                var exitCode = this.executableUtils.RunProcessAndWaitForExit(this.ncoverExe, "OpenCover Discovery", new[] { "-?" });

                // we'd love to check exitCode but it's unfortunately always 1 on a dry run, even when we do specify -?

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
            get { return "OpenCover"; }
        }

        //from OpenCover msbuild integration
        /// <summary>
        /// Returns the  path to the OpenCover tool.
        /// </summary>
        /// <returns>The full path to the OpenCover tool.</returns>
        private string GetOpenCoverExeInstallationPath()
        {
            string path = "";
            string exe = Path.GetFileName(OpenCoverExeName);

            if (string.IsNullOrEmpty(path))
            {
                if (File.Exists(exe))
                    return Path.GetDirectoryName(Path.GetFullPath(exe));

                RegistryKey key = null;

                string[] keyNames = new string[] { OpenCoverRegKey, OpenCoverRegKeyWow6432 };
                foreach (string kn in keyNames)
                {
                    key = Registry.CurrentUser.OpenSubKey(kn);
                    if (key != null)
                        break;

                    key = Registry.LocalMachine.OpenSubKey(kn);
                    if (key != null)
                        break;
                }

                if (key == null)
                {
                    throw new Exception("Could not find OpenCover installation registry key. Please install OpenCover or repair installation.");
                }

                path = (string)key.GetValue(OpenCoverRegValue);
                if (string.IsNullOrEmpty(path))
                {
                    throw new Exception("Could not find OpenCover installation path. Please repair OpenCover installation.");
                }
            }

            return Path.GetFullPath(path);
        }

        /// <summary>
        /// This performs the first "dry" run, i.e. with nonmutated subjects.
        /// We check that all tests pass, as it makes no sense to mutation-analyse a testsuite that's already broken.
        /// The most important information we derive here is the per-test method tree - mapping which test is running which method.
        /// </summary>
        public IReadOnlyCollection<TestSubjectMethodRef> DiscoverTestSubjectMapping(DirectoryInfo modelDirectory, IReadOnlyCollection<TestBinary> testsToRun)
        {
            //invoke tests and parse results
            var partialCoverages = testsToRun
                //.AsParallel()
                .Select(testBinary =>
                    {
                        string shadowDir;
                        IReadOnlyCollection<TestMethodRef> testMethods;
                        var doc = this.invoker.RunTestsAndGetOutput(this.ncoverExe, modelDirectory, testBinary.Runner, testBinary.Binary, out shadowDir, out testMethods);
                        return this.mappingParser.ParseMapping(testBinary.Runner, testBinary.Binary, modelDirectory, doc, shadowDir, testMethods);
                    });

            //one subject method can be tested by tests from several assemblies, so here we merge the lists.
            var mergedCoverages = partialCoverages
                .SelectMany(i => i)
                .GroupBy(s => s.Method)
                .Select(groupedBySubject => new
                {
                    Method = groupedBySubject.Key,
                    AllTests = groupedBySubject.SelectMany(s => s.AllTestMethods).Distinct(),
                    MappedTests = groupedBySubject.SelectMany(s => s.TestMethodsBySequencePointInstructionOffset)
                        .GroupBy(t => t.Item1)
                        .Select(groupedByOffset =>
                            new Tuple<int, IReadOnlyCollection<TestMethodRef>>(groupedByOffset.Key, groupedByOffset.SelectMany(i => i.Item2).Distinct().ToArray()))
                        .Where(t => t.Item2.Any())
                });

            return mergedCoverages.Select(c =>
                new TestSubjectMethodRef(
                    c.Method,
                    c.MappedTests.ToArray(),
                    c.AllTests.ToArray())).ToArray();
        }

        ///// <summary>
        ///// Generates the command line arguments for the OpenCover tool.
        ///// </summary>
        ///// <returns>The command line arguments for the OpenCover tool.</returns>
        //private string GenerateCommandLineCommands()
        //{
        //    CommandLineBuilder builder = new CommandLineBuilder();

        //    if (Service)
        //        builder.AppendSwitch("-service");
        //    if (Register)
        //        builder.AppendSwitch("-register:user");
        //    if (!DefaultFilters)
        //        builder.AppendSwitch("-nodefaultfilters");
        //    if (MergeByHash)
        //        builder.AppendSwitch("-mergebyhash");
        //    if (ShowUnvisited)
        //        builder.AppendSwitch("-showunvisited");
        //    if (ReturnTargetCode)
        //    {
        //        builder.AppendSwitch("-returntargetcode" + (TargetCodeOffset != 0 ? string.Format(":{0}", TargetCodeOffset) : null));
        //    }

        //    builder.AppendSwitchIfNotNull("-target:", Target);
        //    builder.AppendSwitchIfNotNull("-targetdir:", TargetWorkingDir);
        //    builder.AppendSwitchIfNotNull("-targetargs:", TargetArgs);

        //    if ((Filter != null) && (Filter.Length > 0))
        //        builder.AppendSwitchIfNotNull("-filter:", string.Join<ITaskItem>(" ", Filter));

        //    if ((ExcludeByAttribute != null) && (ExcludeByAttribute.Length > 0))
        //        builder.AppendSwitchIfNotNull("-excludebyattribute:", string.Join<ITaskItem>(";", ExcludeByAttribute));

        //    if ((ExcludeByFile != null) && (ExcludeByFile.Length > 0))
        //        builder.AppendSwitchIfNotNull("-excludebyfile:", string.Join<ITaskItem>(";", ExcludeByFile));

        //    if ((CoverByTest != null) && (CoverByTest.Length > 0))
        //        builder.AppendSwitchIfNotNull("-coverbytest:", string.Join<ITaskItem>(";", CoverByTest));

        //    builder.AppendSwitchIfNotNull("-output:", Output);

        //    return builder.ToString();
        //}
    }
}
