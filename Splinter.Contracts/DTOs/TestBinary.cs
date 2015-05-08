using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace Splinter.Contracts.DTOs
{
    /// <summary>
    /// Represents an assembly that has been found to contain test methods, with associated test runner implementation
    /// </summary>
    [DebuggerDisplay("TestBinary {Binary.Name}")]
    public class TestBinary
    {
        public TestBinary(ITestRunner runner, FileInfo binary)
        {
            this.Runner = runner;
            this.Binary = binary;
        }

        public ITestRunner Runner { get; private set; }

        public FileInfo Binary { get; private set; }
    }
}
