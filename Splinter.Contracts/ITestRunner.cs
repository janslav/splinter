using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

using Splinter.Contracts.DTOs;

namespace Splinter.Contracts
{
    /// <summary>
    /// Runs tests. This will be usally specific to a unit testing framework.
    /// </summary>
    public interface ITestRunner : IPlugin
    {
        /// <summary>
        /// Determines whether the specified binary contains tests.
        /// </summary>
        bool IsTestBinary(FileInfo binary);

        /// <summary>
        /// Gets the process information to run all tests from a binary.
        /// </summary>
        ProcessStartInfo GetProcessInfoToRunTests(DirectoryInfo workingDirectory, FileInfo testBinary);

        /// <summary>
        /// Gets the process information to run one test from a binary.
        /// </summary>
        ProcessStartInfo GetProcessInfoToRunTest(DirectoryInfo workingDirectory, FileInfo testBinary, string methodFullName);

        /// <summary>
        /// Extracts the test methods with additional metadata (such as test run time) using the console output of the coverage process.
        /// </summary>
        IReadOnlyCollection<TestMethodRef> ParseTestMethodsList(FileInfo testBinary, string testRunConsoleOut);
    }
}
