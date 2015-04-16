using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splinter.Contracts
{
    public interface ITestRunner
    {
        /// <summary>
        /// Returns true if the runner is available, i.e. has its binaries installed, registered, etc.
        /// </summary>
        bool IsAvailable();


    }
}
