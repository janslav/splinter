using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Splinter.Phase0_Boot
{
    /// <summary>
    /// This represents the splinter config section in app.settings file.
    /// It contains "fine tuning" properties, such that are typically not suited for being passed as command line options.
    /// </summary>
    public class SplinterConfigurationSection : ConfigurationSection
    {
        const string _MaxMutationRunningTimeConstantInSeconds = "maxMutationRunningTimeConstantInSeconds";

        const string _MaxMutationRunningTimeFactor = "maxMutationRunningTimeFactor";

        /// <summary>
        /// Gets the maximum mutation running time constant in seconds.
        /// The total max time allowed for a mutation run is MaxMutationRunningTimeConstantInSeconds + (originalTime * MaxMutationRunningTimeFactor)
        /// </summary>
        [ConfigurationProperty(_MaxMutationRunningTimeConstantInSeconds, DefaultValue = 30)]
        public int MaxMutationRunningTimeConstantInSeconds
        {
            get
            {
                return Convert.ToInt32(this[_MaxMutationRunningTimeConstantInSeconds]);
            }
        }

        /// <summary>
        /// Gets the maximum mutation running time factor.
        /// The total max time allowed for a mutation run is MaxMutationRunningTimeConstantInSeconds + (originalTime * MaxMutationRunningTimeFactor)
        /// </summary>
        [ConfigurationProperty(_MaxMutationRunningTimeFactor, DefaultValue = 5)]
        public int MaxMutationRunningTimeFactor
        {
            get
            {
                return Convert.ToInt32(this[_MaxMutationRunningTimeFactor]);
            }
        }

        public static SplinterConfigurationSection GetAppConfigSection()
        {
            return ConfigurationManager.GetSection("splinter") as SplinterConfigurationSection;
        }
    }
}
