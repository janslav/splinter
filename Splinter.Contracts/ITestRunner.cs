using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Splinter.Contracts
{
    public interface ITestRunner : IPlugin
    {
        bool IsTestBinary(FileInfo binary);
    }
}
