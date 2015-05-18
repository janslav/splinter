using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using log4net;

using Splinter.Phase2_Mutation.DTOs;
using Splinter.Contracts;
using Splinter.Contracts.DTOs;
using Splinter.Utils.Cecil;

namespace Splinter.Phase3_Reporting
{
    public class ResultsLoggerFactory : IPluginFactory<IResultsExporter>
    {
        public IResultsExporter GetPlugin(ILog log)
        {
            return new ResultsLogger(log, CodeCache.Instance);
        }
    }

    public class ResultsLogger : IResultsExporter
    {
        private readonly ILog log;

        private readonly ICodeCache codeCache;

        public ResultsLogger(ILog log, ICodeCache codeCache)
        {
            this.log = log;
            this.codeCache = codeCache;
        }

        public void ExportResults(IReadOnlyCollection<SingleMutationTestResult> results)
        {
            if (!results.Any())
            {
                this.log.Info("No results to report.");
                return;
            }

            var realRunsResults = results.Where(r => !string.IsNullOrWhiteSpace(r.MutationDescription)).ToArray();

            var testsCount = realRunsResults.Sum(r => r.FailingTests.Count + r.PassingTests.Count);

            this.log.InfoFormat("Number of mutations: {0}", realRunsResults.Length);
            this.log.InfoFormat("Number of mutation tests run: {0}", testsCount);


            if (0 == testsCount)
            {
                this.log.Warn("No mutation tests run. Something probably wet wrong.");
                return;
            }

            if (0 == realRunsResults.Sum(r => r.PassingTests.Count))
            {
                this.log.Warn("All mutation tests failed. Something probably wet wrong. Or all your tests are beyond perfection.");
                return;
            }

            if (0 == realRunsResults.Sum(r => r.FailingTests.Count))
            {
                this.log.Warn("All mutation tests passed. Something probably wet wrong. Or all your tests are completely useless.");
            }

            var survivingMutants = realRunsResults.Where(r => r.FailingTests.Count == 0).ToArray();

            //now we want to write out unkilled mutants - those with zero failed tests.
            foreach (var result in survivingMutants)
            {
                this.log.WarnFormat(
                    "Missed mutation: method '{0}', mutation '{1}', instruction {2}.",
                    result.Subject.FullName,
                    result.MutationDescription,
                    result.InstructionIndex);

                //this.RenderCodeLine(result);
            }

            //now we want to write out tests that killed no mutants - those which never failed. Including those for which there were no mutants.
            var allPassing = results.SelectMany(r => r.PassingTests).Distinct().ToArray();
            var allFailing = results.SelectMany(r => r.FailingTests).Distinct().ToArray();

            var neverFailing = allPassing.Except(allFailing).ToArray();
            foreach (var useless in neverFailing)
            {
                this.log.WarnFormat("Never failing test: {0}", useless.FullName);
            }

            this.log.Info("= = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = =");

            this.log.InfoFormat(
                "Out of {1} mutants, {2} survived.{0}That's {3:0.0}% 'coverage-coverage'.",
                Environment.NewLine,
                realRunsResults.Length,
                survivingMutants.Length,
                100.0 - ((survivingMutants.Length * 100.0) / realRunsResults.Length));

            var uniqueTests = allPassing.Union(allFailing).Count();

            this.log.InfoFormat(
                "Out of {1} tests, {2} didn't contribute to killing mutants.{0}That's {3:0.0}% usefulness.",
                Environment.NewLine,
                uniqueTests,
                neverFailing.Length,
                100.0 - ((neverFailing.Length * 100.0) / uniqueTests));
        }

        private void RenderCodeLine(SingleMutationTestResult result)
        {
            var a = this.codeCache.GetAssembly(result.Subject.Assembly);
            var sp = a.GetSequencePoint(result.Subject.FullName, result.InstructionIndex);
            var source = a.GetSourceFile(sp.Document);
            string line = source.Lines[sp.StartLine];

            string beginning = line.Substring(0, sp.StartColumn - 1);
            string highlight;
            string ending;

            if (sp.StartLine == sp.EndLine)
            {
                highlight = line.Substring(sp.StartColumn - 1, sp.EndColumn - sp.StartColumn);
                ending = line.Substring(sp.EndColumn - 1, line.Length - (sp.EndColumn - 1));
            }
            else
            {
                highlight = line.Substring(sp.StartColumn - 1);
                ending = "";
            }

            var color = Console.BackgroundColor;
            Console.Write(beginning);
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Write(highlight);
            Console.BackgroundColor = color;
            Console.WriteLine(ending);
        }

        #region IPlugin implementation
        public string Name
        {
            get { return "ResultsLogger"; }
        }

        public void SetupCommandLineOptions(Mono.Options.OptionSet options)
        {
        }

        public bool IsReady(out string unavailableMessage)
        {
            unavailableMessage = null;
            return true;
        }
        #endregion
    }
}
