using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

using Microsoft.Practices.Unity;

namespace Splinter.Phase0_Boot
{
    public class UnityBootstrapper
    {
        public IUnityContainer CreateContainer()
        {
            var container = new UnityContainer();

            //registers the simple class-interface pairs like Stuff-IStuff, as nameless singletons
            container.RegisterTypes(
                AllClasses.FromLoadedAssemblies(),
                WithMappings.FromMatchingInterface,
                WithName.Default,
                WithLifetime.ContainerControlled);

            BootstrapLogging(container);

            return container;
        }

        protected virtual void BootstrapLogging(UnityContainer container)
        {
            if (ConfigurationManager.GetSection("log4net") == null)
            {
                log4net.Config.BasicConfigurator.Configure();
            }
            else
            {
                log4net.Config.XmlConfigurator.Configure();
            }

            container.RegisterInstance<log4net.ILog>(log4net.LogManager.GetLogger("namelessLogger"));
        }
    }
}
