using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Practices.Unity;

using Splinter.Phase0_Boot;

namespace Splinter
{
    class Program
    {
        static void Main(string[] args)
        {
            var iocContainer = new UnityBootstrapper().CreateContainer();

            var log = iocContainer.Resolve<log4net.ILog>();

            log.Debug("Splinter starting.");

            AppDomain.CurrentDomain.UnhandledException += (_, e) => log.Fatal("Unhandled exception outside the main thread.", e.ExceptionObject as Exception);

            try
            {
                var cmdLine = clipr.CliParser.Parse<ManualConfiguration>(args); //new [] {"-h"}

                var splinterSession = iocContainer.Resolve<ISplinterSession>();
                splinterSession.Start(cmdLine);
            }
            catch (clipr.Core.ParserExit)
            {
                //this means we wrote out help. I think.
            }
            catch (clipr.ParseException e)
            {
                RenderFatalException("Error while parsing command line arguments: ", log, e);
            }
            catch (clipr.ArgumentIntegrityException e)
            {
                RenderFatalException("Error while parsing command line arguments: ", log, e);
            }
            catch (Exception e)
            {
                RenderFatalException("Fatal exception:", log, e);
            }
            finally
            {
                log.Debug("Splinter terminating.");
            }
        }

        private static void RenderFatalException(string message, log4net.ILog log, Exception e)
        {
            if (log.IsDebugEnabled)
            {
                log.Fatal(message, e);
            }
            else
            {
                log.Fatal(message + e.Message);
            }
        }
    }
}
