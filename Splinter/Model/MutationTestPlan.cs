using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splinter.Model
{
    /// <summary>
    /// This is the result of a planning optimization pass on the InitialCoverage data.
    /// We have the plan saying what mutants to create and what tests to run on them.
    /// This shall be used by the "TestRunner" component
    /// </summary>
    public class MutationTestPlan
    {
        //public MutationTestPlan(InitialCoverage initialCoverage)
        //{
        //    this.InitialCoverage = initialCoverage;
        //}

        //public InitialCoverage InitialCoverage { get; private set; }
    }
}
