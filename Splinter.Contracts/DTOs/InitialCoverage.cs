using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splinter.Contracts.DTOs
{
    /// <summary>
    /// This represents the coverage as we sniffed it from the first run. This is created/filled by the "CoverageRunner" component.
    /// We should be able to tell which subject methods are being tested by which test.
    /// </summary>
    public class InitialCoverage
    {
        public InitialCoverage(TestsToRun tests)
        {
            this.TestsToRun = tests;
        }

        public TestsToRun TestsToRun { get; private set; }
    }
}
