using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace XButtonMapper
{
    internal static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "XButtonMapper";

        internal static bool IsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (key == null)
                    {
                        return false;
                    }

                    string currentValue = key.GetValue(ValueName) as string;
                    return string.Equals(
                        currentValue,
                        BuildCommandLine(Application.ExecutablePath),
                        StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }

        internal static bool TrySetEnabled(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    if (key == null)
                    {
                        return false;
                    }

                    if (enabled)
                    {
                        key.SetValue(
                            ValueName,
                            BuildCommandLine(Application.ExecutablePath),
                            RegistryValueKind.String);
                    }
                    else
                    {
                        key.DeleteValue(ValueName, false);
                    }
                }

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (System.Security.SecurityException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        internal static string BuildCommandLine(string executablePath)
        {
            return "\"" + executablePath + "\"";
        }
    }
}
