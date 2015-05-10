#region Copyright & licence

// This file is part of NinjaTurtles.
// 
// NinjaTurtles is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// NinjaTurtles is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with NinjaTurtles.  If not, see <http://www.gnu.org/licenses/>.
// 
// Copyright (C) 2012-14 David Musgrove and others.

#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using log4net;

namespace Splinter.Utils
{
    public interface IExecutableUtils
    {
        /// <summary>
        /// Looks for an executable at specified locations and in the PATH environment variable
        /// </summary>
        FileInfo FindExecutable(string exeName, IEnumerable<string> additionalSearchLocations = null);

        int RunProcessAndWaitForExit(FileInfo executable, IEnumerable<string> arguments = null, DirectoryInfo workingDirectory = null);

        int RunProcessAndWaitForExit(ProcessStartInfo startInfo);
    }

    // Code mostly taken from NinjaTurtles class Module
    public class ExecutableUtils : IExecutableUtils
    {
        private readonly ILog log;

        public ExecutableUtils(ILog log)
        {
            this.log = log;
        }

        public FileInfo FindExecutable(string exeName, IEnumerable<string> additionalSearchLocations)
        {
            var searchPath = additionalSearchLocations.EmptyIfNull().ToList();
            string environmentSearchPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            searchPath.AddRange(environmentSearchPath.Split(';'));

            foreach (string folder in searchPath)
            {
                var fullExePath = new FileInfo(Path.Combine(folder, exeName));
                if (fullExePath.Exists)
                {
                    return fullExePath;
                }
            }

            throw new Exception(string.Format("Couldn't find '{0}' executable.", exeName));
        }

        public int RunProcessAndWaitForExit(FileInfo executable, IEnumerable<string> arguments, DirectoryInfo workingDirectory)
        {
            var r = new ProcessStartInfo(
                    executable.FullName,
                    string.Join(" ", arguments.EmptyIfNull().Select(a => "\"" + CmdLine.EncodeArgument(a) + "\"")))
            {
                WorkingDirectory = workingDirectory == null ? Environment.CurrentDirectory : workingDirectory.FullName,
            };

            return this.RunProcessAndWaitForExit(r);
        }

        private static int counter;

        public int RunProcessAndWaitForExit(ProcessStartInfo startInfo)
        {
            counter++;
            var pid = "#" + counter + "#: ";

            this.log.DebugFormat("{0}Starting process '{1}' with arguments '{2}'.", pid, startInfo.FileName, startInfo.Arguments);

            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;

            using (var p = new Process { StartInfo = startInfo })
            {
                p.EnableRaisingEvents = true;

                p.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) this.log.Debug(pid + e.Data); };
                p.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) this.log.Warn(pid + e.Data); };

                p.Start();

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                p.WaitForExit();

                this.log.DebugFormat("{0}Process exited.", pid);

                return p.ExitCode;
            }
        }
    }
}
