using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Practices.Unity;

namespace Splinter
{
    class Program
    {
        static void Main(string[] args)
        {
            var iocContainer = UnityBootstrapper.CreateContainer();

            var log = iocContainer.Resolve<log4net.ILog>();
            
            log.Debug("Splinter starting.");

            AppDomain.CurrentDomain.UnhandledException += (_, e) => log.Fatal("Unhandled exception outside the main thread.", e.ExceptionObject as Exception);

            try
            {
                var splinterSession = iocContainer.Resolve<ISplinterSession>();

                var sessionSettings = splinterSession.Initialize(args);

            }
            catch (Exception e)
            {
                log.Fatal("Unhandled exception in the main thread.", e);
            }
            finally
            {
                log.Debug("Splinter terminating.");
            }
        }
    }
}
