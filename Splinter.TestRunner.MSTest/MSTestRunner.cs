using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;

using Splinter.Contracts;

namespace Splinter.TestRunner.MSTest
{
    [Export(typeof(ITestRunner))]
    [ExportMetadata("Name", "MsTest")]
    public class MSTestRunner : ITestRunner
    {
        public bool IsAvailable()
        {
            throw new NotImplementedException("not there yet");
        }
    }
}
