using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Splinter.Contracts.DTOs
{
    [DebuggerDisplay("TestSubjectMethod {FullName} Tests: {TestMethods.Count}")]
    public class TestSubjectMethod : Method
    {
        public TestSubjectMethod()
        {
            this.TestMethods = new HashSet<Method>();
        }

        public ISet<Method> TestMethods { get; private set; }
    }
}
