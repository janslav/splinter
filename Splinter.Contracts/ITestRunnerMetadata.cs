using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splinter.Contracts
{
    public interface ITestRunnerMetadata
    {
        /// <summary>
        /// Returns the name of the unit test toolset, such as "mstest".
        /// </summary>
        string Name { get; }
    }
}
