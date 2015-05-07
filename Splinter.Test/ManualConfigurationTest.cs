using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssert;

using clipr;
using clipr.Core;
using clipr.Usage;

using Splinter.Phase0_Boot;

namespace Splinter.Test
{
    [TestClass]
    public class ManualConfigurationTest
    {
        [TestMethod, ExpectedException(typeof(ParserExit))]
        public void HelpRequestThrowsException()
        {
            //here we're actually "testing" clipr, but the actual point is to have a place to check how the generated help looks like.
            var opts = new ManualConfiguration();
            var help = new AutomaticHelpGenerator<ManualConfiguration>();
            var helpText = help.GetUsage();

            var parser = new CliParser<ManualConfiguration>(opts, help);

            parser.Parse(new[] { "--help" });
        }

        [TestMethod]
        public void ParseEmptyCmdLine()
        {
            var cmdLine = CliParser.Parse<ManualConfiguration>(new string[0]);
            cmdLine.TestBinaries.ShouldContainAllInOrder(new string[0]);
        }

        [TestMethod, ExpectedException(typeof(Exception), AllowDerivedTypes=true)]
        public void SpecifyingBinariesWithoutRunnerIsInvalid()
        {
            var cmdLine = CliParser.Parse<ManualConfiguration>(new[] { "testlib.dll", "secondtest.dll" });
        }

        [TestMethod]
        public void ParseTestFilesNames()
        {
            var cmdLine = CliParser.Parse<ManualConfiguration>(new[] { "--testRunner", "mstest", "testlib.dll", "secondtest.dll" });

            cmdLine.TestRunner.ShouldBeEqualTo("mstest");
            cmdLine.TestBinaries.ShouldContainAllInOrder(new[] { "testlib.dll", "secondtest.dll" });
        }
    }
}
