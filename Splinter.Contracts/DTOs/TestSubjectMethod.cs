using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Splinter.Contracts.DTOs
{
    [DebuggerDisplay("TestSubjectMethod {Method.FullName} Tests: {TestMethods.Count}")]
    public class TestSubjectMethod
    {
        public TestSubjectMethod(Method method, ISet<Method> testMethods)
        {
            this.Method = method;
            this.TestMethods = testMethods;
        }

        public Method Method { get; private set; }

        public ISet<Method> TestMethods { get; private set; }
    }
}
