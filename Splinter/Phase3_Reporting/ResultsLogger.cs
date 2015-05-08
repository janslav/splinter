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
        void LogResults(IReadOnlyCollection<SingleMutationTestResult> results);
    }

    public class ResultsLogger : IResultsLogger
    {
        private readonly ILog log;

        public ResultsLogger(ILog log)
        {
            this.log = log;
        }

        public void LogResults(IReadOnlyCollection<SingleMutationTestResult> results)
        {
            if (!results.Any())
            {
                this.log.Info("No results to report.");
                return;
            }

            var testsCount = results.Sum(r => r.FailingTests.Count + r.PassingTests.Count);

            this.log.InfoFormat("Number of mutations: {0}", results.Count);
            this.log.InfoFormat("Number of mutation tests run: {0}", testsCount);


            if (0 == testsCount)
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

            var survivingMutants = results.Where(r => r.FailingTests.Count == 0 && !string.IsNullOrWhiteSpace(r.Description)).ToArray();

            //now we want to write out unkilled mutants - those with zero failed tests.
            foreach (var result in survivingMutants)
            {
                this.log.WarnFormat(
                    "Missed mutation: method '{0}', mutation '{1}', instruction {2}.",
                    result.Subject.FullName,
                    result.Description,
                    result.InstructionIndex);
            }

            //now we want to write out tests that killed no mutants - those which never failed.
            var allPassing = results.SelectMany(r => r.PassingTests).Distinct().ToArray();
            var allFailing = results.SelectMany(r => r.FailingTests).Distinct().ToArray();

            var neverFailing = allPassing.Except(allFailing).ToArray();
            foreach (var useless in neverFailing)
            {
                this.log.WarnFormat("Never failing test: {0}", useless.FullName);
            }

            this.log.Info("= = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = =");

            this.log.InfoFormat(
                "Out of {0} mutants, {1} survived. That's {2:0.0}% 'coverage-coverage'.",
                results.Count,
                survivingMutants.Length,
                100.0 - ((survivingMutants.Length * 100.0) / results.Count));

            var uniqueTests = allPassing.Union(allFailing).Count();

            this.log.InfoFormat(
                "Out of {0} tests, {1} didn't contribute to killing mutants. That's {2:0.0}% usefulness.",
                uniqueTests,
                neverFailing.Length,
                100.0 - ((neverFailing.Length * 100.0) / uniqueTests));
        }
    }
}
