namespace Splinter.TestRunner.MsTest
{
    using log4net;

    using Microsoft.Practices.Unity;

    using Splinter.Contracts;
    using Splinter.Utils;

    /// <summary>
    /// Produces the mstest runner plugin
    /// </summary>
    public class MsTestRunnerFactory : TypeBasedEqualityImplementation, IPluginFactory<ITestRunner>
    {
        /// <summary>
        /// Creates and returns the plugin.
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        ITestRunner IPluginFactory<ITestRunner>.GetPlugin(ILog log)
        {
            var container = new UnityBootstrapper(log).CreateContainer();

            var runner = container.Resolve<MsTestRunner>();

            return runner;
        }

        public string Name
        {
            get { return "MsTestRunnerFactory"; }
        }
    }
}
