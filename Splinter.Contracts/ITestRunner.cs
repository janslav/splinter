using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace Splinter.Contracts
{
    public interface ITestRunner : IPlugin
    {
        bool IsTestBinary(FileInfo binary);

        ProcessStartInfo GetProcessInfoToRunTests(FileInfo testBinaries);
    }
}
