﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

using log4net;

using Splinter.Contracts;

namespace Splinter.Phase1_Discovery
{
    /// <summary>
    /// Discovers and lists the splinter plugins
    /// </summary>
    public interface IPluginsContainer
    {
        /// <summary>
        /// Gets the discovered coverage runners.
        /// </summary>
        IImmutableSet<ICoverageRunner> DiscoveredCoverageRunners { get; }

        /// <summary>
        /// Gets the discovered coverage test ordering strategies.
        /// </summary>
        IImmutableSet<IPluginFactory<IMutationTestsOrderingStrategy>> DiscoveredTestOrderingStrategyFactories { get; }

        /// <summary>
        /// Gets the discovered test runners.
        /// </summary>
        IImmutableSet<ITestRunner> DiscoveredTestRunners { get; }

        /// <summary>
        /// Gets the discovered result exporters.
        /// </summary>
        IImmutableSet<IResultsExporter> DiscoveredResultExporters { get; }

        /// <summary>
        /// Filters the specified plugins by availability.
        /// Logs warnings for ones that are not available.
        /// </summary>
        IReadOnlyCollection<T> FilterByAvailability<T>(IReadOnlyCollection<T> plugins) where T : IPlugin;
    }

    /// <summary>
    /// Discovers and lists the splinter plugins
    /// </summary>
    public class PluginsContainer : IPluginsContainer
    {
        [ImportMany]
        private IEnumerable<Lazy<IPluginFactory<ICoverageRunner>>> lazyCoverageRunners = null; //assigning null to avoid compiler warning

        [ImportMany]
        private IEnumerable<Lazy<IPluginFactory<ITestRunner>>> lazyTestRunners = null; //assigning null to avoid compiler warning

        [ImportMany]
        private IEnumerable<Lazy<IPluginFactory<IResultsExporter>>> lazyResultExporters = null; //assigning null to avoid compiler warning

        [ImportMany]
        private IEnumerable<Lazy<IPluginFactory<IMutationTestsOrderingStrategy>>> lazyTestOrderingStrategies = null; //assigning null to avoid compiler warning

        private readonly ILog log;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginsContainer"/> class.
        /// </summary>
        public PluginsContainer(ILog log)
        {
            this.log = log;

            var catalog = new ApplicationCatalog();

            var compositionContainer = new CompositionContainer(catalog);
            compositionContainer.ComposeParts(this);

            this.DiscoveredCoverageRunners = ImmutableHashSet.CreateRange(this.lazyCoverageRunners.EmptyIfNull().Select(l => l.Value.GetPlugin(log)));
            this.DiscoveredTestRunners = ImmutableHashSet.CreateRange(this.lazyTestRunners.EmptyIfNull().Select(l => l.Value.GetPlugin(log)));
            this.DiscoveredResultExporters = ImmutableHashSet.CreateRange(this.lazyResultExporters.EmptyIfNull().Select(l => l.Value.GetPlugin(log)));
            this.DiscoveredTestOrderingStrategyFactories = ImmutableHashSet.CreateRange(this.lazyTestOrderingStrategies.EmptyIfNull().Select(l => l.Value));
        }

        /// <summary>
        /// Gets the discovered coverage runners.
        /// </summary>
        public IImmutableSet<ICoverageRunner> DiscoveredCoverageRunners { get; private set; }

        /// <summary>
        /// Gets the discovered test runners.
        /// </summary>
        public IImmutableSet<ITestRunner> DiscoveredTestRunners { get; private set; }

        /// <summary>
        /// Gets the discovered result exporters.
        /// </summary>
        public IImmutableSet<IResultsExporter> DiscoveredResultExporters { get; private set; }

        /// <summary>
        /// Gets the discovered coverage test ordering strategies.
        /// </summary>
        public IImmutableSet<IPluginFactory<IMutationTestsOrderingStrategy>> DiscoveredTestOrderingStrategyFactories { get; private set; }

        /// <summary>
        /// Filters the specified plugins by availability.
        /// Logs warnings for ones that are not available.
        /// </summary>
        public IReadOnlyCollection<T> FilterByAvailability<T>(IReadOnlyCollection<T> plugins) where T : IPlugin
        {
            var pluginType = typeof(T).Name;

            var readiness = plugins.Select(tr =>
            {
                string msg;
                return new
                {
                    Runner = tr,
                    Ready = tr.IsReady(out msg),
                    Msg = msg
                };
            }).ToArray();

            if (!readiness.Any(tr => tr.Ready))
            {
                var msgs = string.Join(Environment.NewLine, readiness.Select(tr => tr.Runner.Name + ": " + tr.Msg));
                throw new Exception(string.Format("No plugins of type '{0}' ready/installed:{1}{2}", pluginType, Environment.NewLine, msgs));
            }
            else
            {
                var notReady = readiness.Where(r => !r.Ready);
                if (notReady.Any())
                {
                    var msgs = string.Join(Environment.NewLine, notReady.Select(tr => tr.Runner.Name + ": " + tr.Msg));
                    this.log.DebugFormat("Some plugins of type '{0}' not ready/installed:{1}{2}", pluginType, Environment.NewLine, msgs);
                }
            }

            return readiness.Where(tr => tr.Ready).Select(tr => tr.Runner).ToArray();
        }
    }
}
