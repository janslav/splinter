using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

using Microsoft.Practices.Unity;

namespace Splinter.CoverageRunner.OpenCover
{
    /// <summary>
    /// Bootstraps Unity
    /// </summary>
    public class UnityBootstrapper
    {
        private log4net.ILog log;

        public UnityBootstrapper(log4net.ILog log)
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

            var registrations = container.Registrations.Select(r => new { r.RegisteredType, r.MappedToType });

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
