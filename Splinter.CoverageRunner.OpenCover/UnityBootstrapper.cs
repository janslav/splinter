namespace Splinter.CoverageRunner.OpenCover
{
    using System.Linq;

    using log4net;

    using Microsoft.Practices.Unity;

    /// <summary>
    /// Bootstraps Unity
    /// </summary>
    public class UnityBootstrapper
    {
        private readonly ILog log;

        public UnityBootstrapper(ILog log)
        {
            this.log = log;
        }

        /// <summary>
        /// Creates and initializes the container.
        /// </summary>
        /// <returns></returns>
        public IUnityContainer CreateContainer()
        {
            var container = new UnityContainer();

            //registers the simple class-interface pairs like Stuff-IStuff, as nameless singletons
            container.RegisterTypes(
                AllClasses.FromLoadedAssemblies(),
                WithMappings.FromMatchingInterface,
                WithName.Default,
                WithLifetime.ContainerControlled);

            this.BootstrapLogging(container);

            return container;
        }

        /// <summary>
        /// Bootstraps the logging.
        /// </summary>
        /// <param name="container">The container.</param>
        protected virtual void BootstrapLogging(UnityContainer container)
        {
            container.RegisterInstance(this.log);
        }
    }
}
