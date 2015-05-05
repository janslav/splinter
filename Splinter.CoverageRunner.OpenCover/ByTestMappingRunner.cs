using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net;

using Splinter.Contracts;
using Splinter.Contracts.DTOs;

namespace Splinter.CoverageRunner.OpenCover
{
    public interface IByTestMappingRunner
    {
        /// <summary>
        /// Implementation of ICoverageRunner.GetInitialCoverage        
        /// </summary>
        /// <param name="testsToRun"></param>
        /// <returns></returns>
        InitialCoverage RunTestsAndMapMethods(TestsToRun testsToRun);
    }

    public class ByTestMappingRunner : IByTestMappingRunner
    {
        private readonly ILog log;
        

        public ByTestMappingRunner(ILog log)
        {
            this.log = log;
        }

        public InitialCoverage RunTestsAndMapMethods(TestsToRun testsToRun)
        {
            testsToRun.TestRunner.GetProcessInfoToRunTests(testsToRun.TestBinaries);


            return null;
        }
    }
}
