using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Splinter.Contracts.DTOs
{
    [DebuggerDisplay("TestMethodRef {Method.FullName} Runner: {testRunner.Name}")]
    public class TestMethodRef
    {
        public TestMethodRef(MethodRef method, ITestRunner testRunner)
        {
            this.Method = method;
            this.Runner = testRunner;
        }

        public MethodRef Method { get; private set; }

        public ITestRunner Runner { get; private set; }
    }
}
