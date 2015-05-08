﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Splinter.Contracts.DTOs
{
    [DebuggerDisplay("TestSubjectMethod {Method.FullName} Tests: {TestMethods.Count}")]
    public class TestSubjectMethodRef
    {
        public TestSubjectMethodRef(MethodRef method, IImmutableSet<TestMethodRef> testMethods)
        {
            this.Method = method;
            this.TestMethods = testMethods;
        }

        public TestSubjectMethodRef(MethodRef method, IEnumerable<TestMethodRef> testMethods)
        {
            this.Method = method;
            this.TestMethods = ImmutableHashSet.CreateRange(testMethods);
        }

        public MethodRef Method { get; private set; }

        public IImmutableSet<TestMethodRef> TestMethods { get; private set; }
    }
}
