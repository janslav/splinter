using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Splinter.Contracts.DTOs
{
    [DebuggerDisplay("TestSubjectMethod {Method.FullName} Tests: {TestMethods.Count}")]
    public class TestSubjectMethod
    {
        public TestSubjectMethod(Method method, IImmutableSet<Method> testMethods)
        {
            this.Method = method;
            this.TestMethods = testMethods;
        }

        public TestSubjectMethod(Method method, IEnumerable<Method> testMethods)
        {
            this.Method = method;
            this.TestMethods = ImmutableHashSet.CreateRange(testMethods);
        }

        public Method Method { get; private set; }

        public IImmutableSet<Method> TestMethods { get; private set; }
    }
}
