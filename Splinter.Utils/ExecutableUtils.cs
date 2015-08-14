﻿#region Copyright & licence

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
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using log4net;

namespace Splinter.Utils
{
    /// <summary>
    /// Utility code for launching of processes
    /// </summary>
    public interface IExecutableUtils
    {
        /// <summary>
        /// Looks for an executable at specified locations and in the PATH environment variable
        /// </summary>
        FileInfo FindExecutable(string exeName, IEnumerable<string> additionalSearchLocations = null);

        /// <summary>
        /// Runs a process with arguments and waits for it to exit.
        /// </summary>
        ProcessRunResult RunProcessAndWaitForExit(FileInfo executable, string opertionId, IEnumerable<string> arguments = null, DirectoryInfo workingDirectory = null, TimeSpan timeOut = new TimeSpan());

        /// <summary>
        /// Runs a process with no arguments and waits for it to exit.
        /// </summary>
        ProcessRunResult RunProcessAndWaitForExit(ProcessStartInfo startInfo, string operationId, TimeSpan timeOut = new TimeSpan());
    }

    public class ProcessRunResult
    {
        public ProcessRunResult(int exitCode, string consoleOut, bool timedOut = false)
        {
            this.ExitCode = exitCode;
            this.ConsoleOut = consoleOut;
            this.TimedOut = timedOut;
        }

        public bool TimedOut { get; private set; }

        public int ExitCode { get; private set; }

        public string ConsoleOut { get; private set; }
    }

    /// <summary>
    /// Utility code for launching of processes
    /// Some code taken from NinjaTurtles
    /// </summary>
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

        /// <summary>
        /// Runs a process with arguments and waits for it to exit.
        /// </summary>
        public ProcessRunResult RunProcessAndWaitForExit(FileInfo executable, string operationId, IEnumerable<string> arguments, DirectoryInfo workingDirectory, TimeSpan timeOut)
        {
            var r = new ProcessStartInfo(
                    executable.FullName,
                    string.Join(" ", arguments.EmptyIfNull().Select(a => "\"" + CmdLine.EncodeArgument(a) + "\"")))
            {
                WorkingDirectory = workingDirectory == null ? Environment.CurrentDirectory : workingDirectory.FullName,
            };

            return this.RunProcessAndWaitForExit(r, operationId, timeOut);
        }

        /// <summary>
        /// Runs a process with no arguments and waits for it to exit.
        /// </summary>
        public ProcessRunResult RunProcessAndWaitForExit(ProcessStartInfo startInfo, string operationId, TimeSpan timeOut)
        {
            this.log.DebugFormat("{0}: Starting process '{1}' with arguments '{2}'.", operationId, startInfo.FileName, startInfo.Arguments);

            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;

            var sbLock = new object();
            var sb = new StringBuilder();
            var rememberConsoleLine = new Action<string>(s =>
            {
                lock (sbLock)
                {
                    sb.AppendLine(s);
                }
            });

            using (var p = new Process { StartInfo = startInfo })
            {
                p.EnableRaisingEvents = true;

                p.OutputDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        this.log.Debug(operationId + ": " + e.Data);
                        rememberConsoleLine(e.Data);
                    }
                };
                p.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        this.log.Warn(operationId + ": " + e.Data);
                        rememberConsoleLine(e.Data);
                    }
                };

                p.Start();

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                if (timeOut > new TimeSpan())
                {
                    if (!p.WaitForExit((int)timeOut.TotalMilliseconds))
                    {
                        try
                        {
                            p.Kill();
                            this.log.DebugFormat("{0}: Process run timed out and was killed.", operationId);
                        }
                        catch (Exception e)
                        {
                            this.log.Warn(string.Format("{0}: Process run timed out and kill attempt failed.", operationId), e);
                        }

                        return new ProcessRunResult(-1, sb.ToString(), true);
                    }
                }
                else
                {
                    p.WaitForExit();
                }

                this.log.DebugFormat("{0}: Process exited.", operationId);

                return new ProcessRunResult(p.ExitCode, sb.ToString());
            }
        }
    }
}
