using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.Diagnostics;
using Microsoft.Win32;

namespace Splinter.Phase2_Mutation
{
    public interface IWindowsErrorReporting
    {
        IDisposable TurnOffErrorReporting();
    }

    public class WindowsErrorReporting : IWindowsErrorReporting
    {
        private const string ERROR_REPORTING_KEY = @"SOFTWARE\Microsoft\Windows\Windows Error Reporting";
        private const string ERROR_REPORTING_VALUE = "DontShowUI";

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

        public IDisposable TurnOffErrorReporting()
        {
            var r = new Switch();

            //debugger attached = we don't care about default debug UI
            if (!Debugger.IsAttached)
            {
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
                catch (UnauthorizedAccessException) { }
            }

            return r;
        }

        private static void RestoreErrorReporting(object errorReportingValue)
        {
            try
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
            catch (UnauthorizedAccessException) { }
        }
    }
}
