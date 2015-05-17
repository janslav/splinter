using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Splinter.Contracts.DTOs;

namespace Splinter.Contracts
{
    public interface IResultsExporter : IPlugin
    {
        void ExportResults(IReadOnlyCollection<SingleMutationTestResult> results);
    }
}
