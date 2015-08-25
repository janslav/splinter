namespace Splinter.CoverageRunner.OpenCover
{
    using log4net;

    using Microsoft.Practices.Unity;

    using Splinter.Contracts;
    using Splinter.Utils;

    /// <summary>
    /// Creates the OpenCoverRunner objects
    /// </summary>
    public class OpenCoverRunnerFactory : TypeBasedEqualityImplementation, IPluginFactory<ICoverageRunner>
    {
        /// <summary>
        /// Creates and returns the plugin.
        /// </summary>
        public ICoverageRunner GetPlugin(ILog log)
        {
            var container = new UnityBootstrapper(log).CreateContainer();

            var runner = container.Resolve<OpenCoverRunner>();

            return runner;
        }

        public string Name
        {
            get { return "OpenCoverRunnerFactory"; }
        }
    }
}
