﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Splinter.Contracts.DTOs;

namespace Splinter.Contracts
{
    /// <summary>
    /// Coverage plugin. Its job is to find the mapping between tests and their subjects.
    /// </summary>
    public interface ICoverageRunner : IPlugin
    {
        /// <summary>
        /// This is supposed to perform the first "dry" run, i.e. with nonmutated subjects.
        /// We check all tests pass, as it makes no sense to mutation-analyse a testsuite that's already broken.
        /// The most important information we derive here is the per-test method tree - mapping which test is running which method.
        /// </summary>
        IReadOnlyCollection<TestSubjectMethodRef> DiscoverTestSubjectMapping(DirectoryInfo modelDirectory, IReadOnlyCollection<TestBinary> testsToRun);
    }
}
