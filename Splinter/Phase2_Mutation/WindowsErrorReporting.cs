using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.Diagnostics;
using Microsoft.Win32;

using log4net;

namespace Splinter.Phase2_Mutation
{
    /// <summary>
    /// Used to switch windows "want to debug?" dialog in Windows.
    /// </summary>
    public interface IWindowsErrorReporting
    {
        /// <summary>
        /// Turns off the error reporting.
        /// </summary>
        /// <returns>An object that, when disposed, resets the error reporting to the state it was before.</returns>
        IDisposable TurnOffErrorReporting();
    }

    /// <summary>
    /// Used to switch windows "want to debug?" dialog in Windows.
    /// </summary>
    public class WindowsErrorReporting : IWindowsErrorReporting
    {
        private const string ERROR_REPORTING_KEY = @"SOFTWARE\Microsoft\Windows\Windows Error Reporting";
        private const string ERROR_REPORTING_VALUE = "DontShowUI";

        private readonly ILog log;

        public WindowsErrorReporting(ILog log)
        {
            this.log = log;
        }

        private class Switch : IDisposable
        {
            internal object originalValue;

            internal bool runRestore;

            public void Dispose()
            {
                if (this.runRestore)
                {
                    RestoreErrorReporting(this.originalValue);
                }
            }
        }

        /// <summary>
        /// Turns off the error reporting.
        /// </summary>
        /// <returns>
        /// An object that, when disposed, resets the error reporting to the state it was before.
        /// </returns>
        public IDisposable TurnOffErrorReporting()
        {
            var r = new Switch();

            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(
                    ERROR_REPORTING_KEY,
                    RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    if (key != null)
                    {
                        r.originalValue = key.GetValue(ERROR_REPORTING_VALUE, null);
                        key.SetValue(ERROR_REPORTING_VALUE, 1, RegistryValueKind.DWord);
                        r.runRestore = true;
                    }
                }
            }
            catch (Exception e) 
            {
                this.log.Warn("Exception while trying to switch off the Windows Debug dialog.", e);
            }

            return r;
        }

        private static void RestoreErrorReporting(object errorReportingValue)
        {
            using (var key = Registry.LocalMachine.OpenSubKey(
                ERROR_REPORTING_KEY,
                RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                if (key == null)
                {
                    return;
                }

                if (errorReportingValue == null)
                {
                    key.DeleteValue(ERROR_REPORTING_VALUE);
                }
                else
                {
                    key.SetValue(ERROR_REPORTING_VALUE, errorReportingValue, RegistryValueKind.DWord);
                }
            }
        }
    }
}
