using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Splinter.Phase0_Boot;
using Mono.Options;
using Moq;
using Splinter.Phase1_Discovery;
using System.Collections.Immutable;
using Splinter.Contracts;

namespace Splinter.Test
{
    [TestClass]
    public class ManualConfigurationTest
    {
        [TestMethod]
        public void ParseEmptyCmdLine()
        {
            var os = new OptionSet();

            var pluginContainerMock = new Mock<IPluginsContainer>();
            pluginContainerMock.SetupGet(c => c.DiscoveredCoverageRunners).Returns(ImmutableHashSet<ICoverageRunner>.Empty);
            pluginContainerMock.SetupGet(c => c.DiscoveredTestRunners).Returns(ImmutableHashSet<ITestRunner>.Empty);
            pluginContainerMock.SetupGet(c => c.DiscoveredTestOrderingStrategyFactories).Returns(ImmutableHashSet<IPluginFactory<IMutationTestsOrderingStrategy>>.Empty);

            var cmdLine = CmdLineConfiguration.SetupCommandLineOptions(os, pluginContainerMock.Object);

            os.Parse(new string[0]);

            cmdLine.TestBinaries.Should().BeEmpty();
        }

        [TestMethod, ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
        public void SpecifyingBinariesWithoutRunnerIsInvalid()
        {
            var os = new OptionSet();

            var pluginContainerMock = new Mock<IPluginsContainer>();
            pluginContainerMock.SetupGet(c => c.DiscoveredCoverageRunners).Returns(ImmutableHashSet<ICoverageRunner>.Empty);
            pluginContainerMock.SetupGet(c => c.DiscoveredTestRunners).Returns(ImmutableHashSet<ITestRunner>.Empty);
            pluginContainerMock.SetupGet(c => c.DiscoveredTestOrderingStrategyFactories).Returns(ImmutableHashSet<IPluginFactory<IMutationTestsOrderingStrategy>>.Empty);

            var cmdLine = CmdLineConfiguration.SetupCommandLineOptions(os, pluginContainerMock.Object);

            os.Parse(new[] { "testlib.dll", "secondtest.dll" });

            cmdLine.Validate();
        }

        [TestMethod]
        public void ParseTestFilesNames()
        {
            var os = new OptionSet();

            var pluginContainerMock = new Mock<IPluginsContainer>();
            pluginContainerMock.SetupGet(c => c.DiscoveredCoverageRunners).Returns(ImmutableHashSet<ICoverageRunner>.Empty);
            pluginContainerMock.SetupGet(c => c.DiscoveredTestRunners).Returns(ImmutableHashSet<ITestRunner>.Empty);
            pluginContainerMock.SetupGet(c => c.DiscoveredTestOrderingStrategyFactories).Returns(ImmutableHashSet<IPluginFactory<IMutationTestsOrderingStrategy>>.Empty);

            var cmdLine = CmdLineConfiguration.SetupCommandLineOptions(os, pluginContainerMock.Object);

            os.Parse(new[] { "--testRunner=mstest", "testlib.dll", "secondtest.dll" });

            cmdLine.Validate();

            cmdLine.TestRunner.ShouldBeEquivalentTo("mstest");
            cmdLine.TestBinaries.Should().Contain(new[] { "testlib.dll", "secondtest.dll" });
        }
    }
}
