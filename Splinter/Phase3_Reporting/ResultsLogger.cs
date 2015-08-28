﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using log4net;

using Splinter.Contracts;
using Splinter.Contracts.DTOs;
using Splinter.Utils;
using Splinter.Utils.Cecil;

namespace Splinter.Phase3_Reporting
{
    /// <summary>
    /// Factory for the ResultsLogger "plugin"
    /// </summary>
    public class ResultsLoggerFactory : TypeBasedEqualityImplementation, IPluginFactory<IResultsExporter>
    {
        /// <summary>
        /// Creates and returns the plugin.
        /// </summary>
        public IResultsExporter GetPlugin(ILog log)
        {
            return new ResultsLogger(log, CodeCache.Instance);
        }

        public string Name
        {
            get { return "ResultsLoggerFactory"; }
        }
    }

    /// <summary>
    /// Used to output splinter run results to console and log file.
    /// </summary>
    public class ResultsLogger : TypeBasedEqualityImplementation, IResultsExporter
    {
        private readonly ILog log;

        private readonly ICodeCache codeCache;

        public ResultsLogger(ILog log, ICodeCache codeCache)
        {
            this.log = log;
            this.codeCache = codeCache;
        }

        /// <summary>
        /// Exports the results to logs and console.
        /// </summary>
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
                this.log.Warn("No mutation tests run. Something probably went wrong.");
                return;
            }

            if (0 == realRunsResults.Sum(r => r.PassingTests.Count))
            {
                this.log.Warn("All mutation tests failed. Something probably went wrong. Or all your tests are beyond perfection.");
                return;
            }

            if (0 == realRunsResults.Sum(r => r.FailingTests.Count))
            {
                this.log.Warn("All mutation tests passed. Something probably went wrong. Or all your tests are completely useless.");
            }

            var survivingMutants = realRunsResults.Where(r => r.FailingTests.Count == 0).ToArray();

            //now we want to write out unkilled mutants - those with zero failed tests.
            foreach (var result in survivingMutants)
            {
                this.log.WarnFormat(
                    "Missed mutation: method '{0}', mutation '{1}'.",
                    result.Subject.FullName,
                    result.MutationDescription);

                //this.RenderCodeLine(result);
            }

            var allPassing = results.SelectMany(r => r.PassingTests).Distinct().ToArray();
            var allFailing = results.SelectMany(r => r.FailingTests).Distinct().ToArray();
            var allTimeouted = results.SelectMany(r => r.TimeoutedTests).Distinct().ToArray();

            //now we want to write out tests that killed no mutants - those which never failed. Including those for which there were no mutants.
            var neverFailing = allPassing.Except(allFailing).ToArray();

            var testsNotGivenChanceToFail = realRunsResults.SelectMany(r => r.NotRunTests).Intersect(neverFailing).Distinct().ToArray();
            if (testsNotGivenChanceToFail.Length > 0)
            {
                this.log.WarnFormat("Some tests were not run against all mutations. The 'never failing' test list is thus not to be trusted. "
                    + "For a complete list, run Splinter with -detectUnusedTests switch.");
            }

            foreach (var useless in neverFailing.Except(testsNotGivenChanceToFail))
            {
                this.log.WarnFormat("Never failing test: {0}", useless.FullName);
            }

            foreach (var timeouted in allTimeouted)
            {
                this.log.WarnFormat("Timeouted test: {0}", timeouted.FullName);
            }

            this.log.Info("= = = = = = = = = = = = = = = = = = = = = = = = = = = = = = = =");

            this.log.InfoFormat(
                "Out of {1} mutants, {2} survived.{0}That's {3:0.0}% 'coverage-coverage'.",
                Environment.NewLine,
                realRunsResults.Length,
                survivingMutants.Length,
                100.0 - ((survivingMutants.Length * 100.0) / realRunsResults.Length));

            var uniqueTestsCount = allPassing.Union(allFailing).Union(allTimeouted).Count();

            this.log.InfoFormat(
                "Out of {1} tests, {2} didn't contribute to killing mutants.{0}That's {3:0.0}% usefulness.",
                Environment.NewLine,
                uniqueTestsCount,
                neverFailing.Length,
                100.0 - ((neverFailing.Length * 100.0) / uniqueTestsCount));
        }

        private void RenderCodeLine(SingleMutationTestResult result)
        {
            var a = this.codeCache.GetAssemblyDefinition(result.Subject.Assembly);
            var sp = a.GetNearestSequencePoint(result.Subject.FullName, result.InstructionOffset);
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
        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get { return "ResultsLogger"; }
        }

        /// <summary>
        /// Sets up the command line options.
        /// </summary>
        /// <param name="options"></param>
        public void SetupCommandLineOptions(Mono.Options.OptionSet options)
        {
        }

        /// <summary>
        /// Returns true if the plugin is available, i.e. has its binaries installed, registered, etc.
        /// </summary>
        /// <param name="unavailableMessage"></param>
        /// <returns></returns>
        public bool IsReady(out string unavailableMessage)
        {
            unavailableMessage = null;
            return true;
        }
        #endregion
    }
}
