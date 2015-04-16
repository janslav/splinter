using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;

using Splinter.Contracts;

namespace Splinter
{
    interface IPluginsContainer
    {
        IReadOnlyCollection<ICoverageRunner> CoverageRunners { get; }

        IReadOnlyCollection<ICoverageRunner> TestRunners { get; }
    }

    public class PluginsContainer : IPluginsContainer
    {


        public IReadOnlyCollection<ICoverageRunner> CoverageRunners
        {
            get { throw new NotImplementedException(); }
        }

        public IReadOnlyCollection<ICoverageRunner> TestRunners
        {
            get { throw new NotImplementedException(); }
        }
    }
}
