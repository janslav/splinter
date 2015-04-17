using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

using Splinter.Contracts;

namespace Splinter
{
    public interface IPluginsContainer
    {
        IReadOnlyCollection<ICoverageRunner> CoverageRunners { get; }

        IReadOnlyCollection<ITestRunner> TestRunners { get; }
    }

    public class PluginsContainer : IPluginsContainer
    {
        [ImportMany]
        IEnumerable<Lazy<IPluginFactory<ICoverageRunner>, ICoverageRunnerMetadata>> lazyCoverageRunners = null; //assigning null to avoid compiler warning

        [ImportMany]
        IEnumerable<Lazy<IPluginFactory<ITestRunner>, ITestRunnerMetadata>> lazyTestRunners = null; //assigning null to avoid compiler warning

        public PluginsContainer(log4net.ILog log)
        {
            var catalog = new ApplicationCatalog();

            var compositionContainer = new CompositionContainer(catalog);
            compositionContainer.ComposeParts(this);

            this.CoverageRunners = this.lazyCoverageRunners.EmptyIfNull().Select(l => l.Value.GetPlugin(log)).ToArray();
            this.TestRunners = this.lazyTestRunners.EmptyIfNull().Select(l => l.Value.GetPlugin(log)).ToArray();
        }

        public IReadOnlyCollection<ICoverageRunner> CoverageRunners { get; private set; }

        public IReadOnlyCollection<ITestRunner> TestRunners { get; private set; }
    }
}
