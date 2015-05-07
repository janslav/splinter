﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Microsoft.Win32;

using log4net;

using NinjaTurtles;

using Splinter.Contracts;
using Splinter.Contracts.DTOs;

namespace Splinter.CoverageRunner.OpenCover
{
    public class OpenCoverRunner : ICoverageRunner
    {
        private const string OpenCoverRegKey = @"SOFTWARE\OpenCover\";
        private const string OpenCoverRegKeyWow6432 = @"SOFTWARE\Wow6432Node\OpenCover\";
        private const string OpenCoverRegValue = "Path";
        private const string OpenCoverExeName = "OpenCover.Console.exe";

        private readonly ILog log;

        private readonly IProcessInvoker invoker;

        private readonly ISubjectTestMappingParser mappingParser;

        private FileInfo ncoverExe;

        public OpenCoverRunner(ILog log, IProcessInvoker invoker, ISubjectTestMappingParser mappingParser)
        {
            this.log = log;
            this.invoker = invoker;
            this.mappingParser = mappingParser;
        }

        public bool IsReady(out string unavailableMessage)
        {
            try
            {
                var paths = new[] { this.GetOpenCoverExeInstallationPath() };
                var process = ConsoleProcessFactory.CreateProcess(OpenCoverExeName, "", paths);

                if (process != null)
                {
                    process.Start();
                    process.WaitForExit();

                    this.ncoverExe = new FileInfo(process.StartInfo.FileName);

                    unavailableMessage = null;
                    return true;
                }

                throw new Exception(OpenCoverExeName + " not found.");
            }
            catch (Exception e)
            {
                unavailableMessage = e.Message;
                return false;
            }
        }

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

        public IReadOnlyCollection<TestSubjectMethodRef> GetInitialCoverage(TestsToRun testsToRun)
        {
            //invoke tests and parse results
            var partialCoverages = testsToRun.TestBinaries
                .AsParallel()
                .Select(testBinary =>
                    {
                        string shadowDir;
                        var doc = this.invoker.RunTestsAndGetOutput(this.ncoverExe, testsToRun.TestRunner, testBinary, out shadowDir);
                        return this.mappingParser.ParseMapping(testBinary, doc, shadowDir);
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
