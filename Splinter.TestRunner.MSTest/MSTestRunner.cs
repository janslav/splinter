using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;

using Splinter.Contracts;

namespace Splinter.TestRunner.MSTest
{
    [Export(typeof(IPluginFactory<ITestRunner>))]
    public class MSTestRunner : ITestRunner, IPluginFactory<ITestRunner>
    {
        private log4net.ILog log;

        //private Assembly frameworkAssembly;

        ITestRunner IPluginFactory<ITestRunner>.GetPlugin(log4net.ILog log)
        {
            this.log = log;
            return this;
        }

        public bool IsReady(out string unavailableMessage)
        {
            //try
            //{
            //    this.frameworkAssembly = Assembly.ReflectionOnlyLoadFrom("Microsoft.VisualStudio.QualityTools.UnitTestFramework");

            unavailableMessage = null;
            return true;
            //}
            //catch (Exception e)
            //{
            //    unavailableMessage = e.Message;
            //    return false;
            //}
        }

        public string Name
        {
            get { return "MsTest"; }
        }

        public bool IsTestBinary(FileInfo binary)
        {
            var assembly = Assembly.ReflectionOnlyLoadFrom(binary.FullName);

            var referencingFramework = assembly.GetReferencedAssemblies().Where(a => a.Name.Equals("Microsoft.VisualStudio.QualityTools.UnitTestFramework"));

            //if (referencingFramework.Any())
            //{
            //    var classes = assembly.GetTypes();
            //    var testClasses = classes.Where(c => c.GetCustomAttributesData().Any(a => a.AttributeType.Name.Equals("TestClass")));

            //    return testClasses.Any();
            //}

            return referencingFramework.Any();
        }
    }
}
