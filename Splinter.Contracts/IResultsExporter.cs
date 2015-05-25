using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Splinter.Contracts.DTOs;

namespace Splinter.Contracts
{
    /// <summary>
    /// The job of this plugin is to process the results of the mutation test.
    /// </summary>
    public interface IResultsExporter : IPlugin
    {
        /// <summary>
        /// Exports the results.
        /// </summary>
        void ExportResults(IReadOnlyCollection<SingleMutationTestResult> results);
    }
}
