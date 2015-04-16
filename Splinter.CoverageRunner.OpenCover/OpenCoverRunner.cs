using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;

using Splinter.Contracts;

namespace Splinter.CoverageRunner.OpenCover
{
    [Export(typeof(ICoverageRunner))]
    [ExportMetadata("Name", "OpenCover")]
    public class OpenCoverRunner : ICoverageRunner
    {
        public bool IsAvailable()
        {
            throw new NotImplementedException("not there yet");
        }
    }
}
