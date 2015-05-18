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
        /// <summary>
        /// Initializes a new instance of the <see cref="TestBinary"/> class.
        /// </summary>
        public TestBinary(ITestRunner runner, FileInfo binary)
        {
            this.Runner = runner;
            this.Binary = binary;
        }

        /// <summary>
        /// Gets the runner for tests in this binary.
        /// </summary>
        public ITestRunner Runner { get; private set; }

        /// <summary>
        /// Gets the location of the binary.
        /// </summary>
        public FileInfo Binary { get; private set; }
    }
}
