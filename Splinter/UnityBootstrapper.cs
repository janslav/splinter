using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Practices.Unity;

namespace Splinter
{
    class UnityBootstrapper
    {
        internal static IUnityContainer CreateContainer()
        {
            var container = new UnityContainer();

            return container;
        }
    }
}
