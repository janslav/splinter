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
    public interface ITestRunner : IPlugin
    {
        bool IsTestBinary(FileInfo binary);

        ProcessStartInfo GetProcessInfoToRunTests(DirectoryInfo workingDirectory, FileInfo testBinary);

        ProcessStartInfo GetProcessInfoToRunTest(DirectoryInfo workingDirectory, FileInfo testBinary, string methodFullName);
    }
}
