using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splinter.Contracts
{
    public interface ICoverageRunnerMetadata
    {
        /// <summary>
        /// Returns the name of the coverage engine, such as "opencover".
        /// </summary>
        string Name { get; }
    }
}
