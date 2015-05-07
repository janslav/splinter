using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using clipr;

namespace Splinter.Phase0_Boot
{
    [ApplicationInfo(Description = "Splinter is a mutation analysis runner.")]
    public class ManualConfiguration
    {
        //[NamedArgument('v', "verbose", Action = ParseAction.Count,
        //    Description = "Increase the verbosity of the output.")]
        //public int Verbosity { get; set; }

        [NamedArgument("testRunner", 
            Description = "The test runner engine name, such as mstest or nunit.")]
        public string TestRunner { get; set; }

        [NamedArgument("coverageRunner",
            Description = "The test coverage engine name, such as opencover.")]
        public string CoverageRunner { get; set; }

        //[PositionalArgument(0, MetaVar = "OUT",
        //    Description = "Output file.")]
        //public string OutputFile { get; set; }

        //[PositionalArgument(1, MetaVar = "N",
        //    NumArgs = 1,
        //    Constraint = NumArgsConstraint.AtLeast,
        //    Description = "Numbers to sum.")]
        //public List<int> Numbers { get; set; }

        [PositionalArgument(0,
            //MetaVar = "TestBinary",
            NumArgs = 0,
            Constraint = NumArgsConstraint.AtLeast,
            Description = "Path(s) to assembl(ies) containing tests.")]
        public IEnumerable<string> TestBinaries { get; set; }

        [PostParse]
        public void Validate()
        {
            if (this.TestBinaries.Any())
            {
                if (string.IsNullOrWhiteSpace(this.TestRunner))
                {
                    throw new Exception("If test binaries are specified, then the runner must be specified as well.");
                }
            }
        }
        
    }
}
