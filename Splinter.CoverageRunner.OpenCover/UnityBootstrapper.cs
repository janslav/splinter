using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

using Microsoft.Practices.Unity;

namespace Splinter
{
    public class UnityBootstrapper
    {
        private log4net.ILog log;

        public UnityBootstrapper(log4net.ILog log)
        {
            this.log = log;
        }

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

        protected virtual void BootstrapLogging(UnityContainer container)
        {
            container.RegisterInstance(this.log);
        }
    }
}
