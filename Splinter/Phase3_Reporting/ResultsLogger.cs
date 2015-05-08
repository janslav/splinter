using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using log4net;

using Splinter.Phase2_Mutation.DTOs;

namespace Splinter.Phase3_Reporting
{
    public interface IResultsLogger
    {
        void LogResults(IEnumerable<SingleMutationTestResult> results);
    }

    public class ResultsLogger : IResultsLogger
    {
        private readonly ILog log;

        public ResultsLogger(ILog log)
        {
            this.log = log;
        }

        public void LogResults(IEnumerable<SingleMutationTestResult> results)
        {
            if (!results.Any())
            {
                this.log.Info("No results to report.");
                return;
            }

            if (0 == results.Sum(r => r.PassingTests.Count + r.FailingTests.Count))
            {
                this.log.Warn("No mutation tests run. Something probably wet wrong.");
                return;
            }

            if (0 == results.Sum(r => r.PassingTests.Count))
            {
                this.log.Warn("All mutation tests failed. Something probably wet wrong. Or all your tests are beyond perfection.");
                return;
            }

            if (0 == results.Sum(r => r.FailingTests.Count))
            {
                this.log.Warn("All mutation tests passed. Something probably wet wrong. Or all your tests are completely useless.");
            }

            //now we want to write out unkilled mutants - those with zero failed tests.
            foreach (var result in results.Where(r => r.FailingTests.Count == 0))
            {
                this.log.InfoFormat(
                    "Missed mutation: method '{0}', mutation '{1}', instruction {2}.",
                    result.Subject.FullName,
                    result.Description,
                    result.InstructionIndex);
            }

            //now we want to write out tests that killed no mutants - those which never failed.
            var allPassing = results.SelectMany(r => r.PassingTests).Distinct().ToArray();
            var allFailing = results.SelectMany(r => r.FailingTests).Distinct().ToArray();

            var neverFailing = allPassing.Except(allFailing);
            foreach (var useless in neverFailing)
            {
                this.log.InfoFormat("Never failing test: {0}", useless.FullName);
            }
        }
    }
}
