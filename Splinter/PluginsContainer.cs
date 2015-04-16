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
        IReadOnlyDictionary<string, ICoverageRunner> CoverageRunners { get; }

        IReadOnlyDictionary<string, ITestRunner> TestRunners { get; }
    }

    public class PluginsContainer : IPluginsContainer
    {
        [ImportMany]
        IEnumerable<Lazy<ICoverageRunner, ICoverageRunnerMetadata>> lazyCoverageRunners = null; //assigning null to avoid compiler warning

        [ImportMany]
        IEnumerable<Lazy<ITestRunner, ITestRunnerMetadata>> lazyTestRunners = null; //assigning null to avoid compiler warning

        public PluginsContainer()
        {
            var catalog = new ApplicationCatalog();

            var compositionContainer = new CompositionContainer(catalog);
            compositionContainer.ComposeParts(this);

            this.CoverageRunners = this.lazyCoverageRunners.ToDictionary(l => l.Metadata.Name, l => l.Value);
            this.TestRunners = this.lazyTestRunners.ToDictionary(l => l.Metadata.Name, l => l.Value);
        }

        public IReadOnlyDictionary<string, ICoverageRunner> CoverageRunners { get; private set; }

        public IReadOnlyDictionary<string, ITestRunner> TestRunners { get; private set; }
    }
}
