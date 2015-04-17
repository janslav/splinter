using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Splinter.Contracts;

namespace Splinter.Model
{
    /// <summary>
    /// Represents the initial situation - configuration that has either gone in via cmd line or we inferred it from files that are present.
    /// Here we store what are the test subject assemblies and tester assemblies, and what test runner(s?) is being used.
    /// </summary>
    public class SessionSettings
    {
        public IReadOnlyCollection<ITestRunner> TestRunners { get; set; }

        public IReadOnlyCollection<ICoverageRunner> CoverageRunners { get; set; }
    }
}
