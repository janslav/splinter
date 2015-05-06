using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Splinter.Contracts.DTOs
{
    /// <summary>
    /// Represents the initial situation - configuration that has either gone in via cmd line or we inferred it from files that are present.
    /// Here we store what are the test subject assemblies and tester assemblies, and what test runner(s?) is being used.
    /// </summary>
    public class TestsToRun
    {
        public TestsToRun(ITestRunner runner, IReadOnlyCollection<FileInfo> binaries)
        {
            this.TestRunner = runner;
            this.TestBinaries = binaries;
        }

        public ITestRunner TestRunner { get; private set; }

        public IReadOnlyCollection<FileInfo> TestBinaries { get; private set; }
    }
}
