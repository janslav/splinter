using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

using Microsoft.Practices.Unity;

using Splinter.Utils.Cecil;

namespace Splinter.TestRunner.MsTest
{
    /// <summary>
    /// Bootstraps unity
    /// </summary>
    public class UnityBootstrapper
    {
        private log4net.ILog log;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityBootstrapper"/> class.
        /// </summary>
        public UnityBootstrapper(log4net.ILog log)
        {
            this.log = log;
        }

        /// <summary>
        /// Creates and initializes the container.
        /// </summary>
        public IUnityContainer CreateContainer()
        {
            var container = new UnityContainer();

            //registers the simple class-interface pairs like Stuff-IStuff, as nameless singletons
            container.RegisterTypes(
                AllClasses.FromLoadedAssemblies(),
                WithMappings.FromMatchingInterface,
                WithName.Default,
                WithLifetime.ContainerControlled);

            container.RegisterInstance<ICodeCache>(CodeCache.Instance);

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
