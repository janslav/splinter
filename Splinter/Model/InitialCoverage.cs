using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splinter.Model
{
    /// <summary>
    /// This represents the coverage as we sniffed it from the first run. This is created/filled by the "CoverageRunner" component.
    /// We should be able to tell which subject methods are being tested by which test.
    /// </summary>
    public class InitialCoverage
    {
        public InitialCoverage(SessionSettings settings)
        {
            this.SessionSettings = settings;
        }

        SessionSettings SessionSettings { get; private set; }
    }
}
