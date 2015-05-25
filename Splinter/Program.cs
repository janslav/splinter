using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Practices.Unity;

using Splinter.Phase0_Boot;

using Mono.Options;

namespace Splinter
{
    class Program
    {
        static int Main(string[] args)
        {
            var iocContainer = new UnityBootstrapper().CreateContainer();

            var log = iocContainer.Resolve<log4net.ILog>();

            log.Debug("Splinter starting.");

            AppDomain.CurrentDomain.UnhandledException += (_, e) => log.Fatal("Unhandled exception outside the main thread.", e.ExceptionObject as Exception);

            var os = new OptionSet();

            try
            {
                var splinterSession = iocContainer.Resolve<ISplinterSession>();

                var cmdLine = splinterSession.SetupCommandLineOptions(os);

                bool showHelp = false;
                os.Add("h|?|help", "Show this message and exit.", (string v) => showHelp = v != null);

                os.Parse(args);

                cmdLine.Validate();

                if (showHelp)
                {
                    ShowHelp(os);
                }
                else
                {
                    splinterSession.Run(cmdLine);
                }

                return 0;
            }
            catch (OptionException e)
            {
                //can this even happen when we have a default option defined? who knows :)
                ShowHelp(os, e.Message);
                return -1;
            }
            catch (Exception e)
            {
                RenderFatalException("Fatal exception:", log, e);
                ShowHelp(os);
                return 1;
            }
            finally
            {
                log.Debug("Splinter terminating.");
            }
        }

        static void ShowHelp(OptionSet p, string msg = null)
        {
            Console.WriteLine("Splinter is a mutation analysis runner.");

            if (!string.IsNullOrWhiteSpace(msg))
            {
                Console.WriteLine();
                Console.WriteLine(msg);
            }

            Console.WriteLine();
            Console.WriteLine("Usage: splinter [OPTIONS] [test1.dll] [test2.dll] ...");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);

            Console.WriteLine("  ...                        Path(s) to assembl(ies) containing tests.");
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
