using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;

namespace XButtonMapper
{
    internal enum ButtonAction
    {
        Copy,
        Paste,
        PassThrough,
        CopyWithInsert
    }

    internal sealed class AppSettings
    {
        private const string ApplicationDirectoryName = "XButtonMapper";
        private const string SettingsFileName = "settings.ini";

        internal AppSettings()
        {
            BackAction = ButtonAction.Copy;
            ForwardAction = ButtonAction.Paste;
        }

        internal ButtonAction BackAction { get; set; }

        internal ButtonAction ForwardAction { get; set; }

        internal static string SettingsPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    ApplicationDirectoryName,
                    SettingsFileName);
            }
        }

        internal static AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return new AppSettings();
                }

                return ParseLines(File.ReadAllLines(SettingsPath, Encoding.UTF8));
            }
            catch (IOException)
            {
                return new AppSettings();
            }
            catch (UnauthorizedAccessException)
            {
                return new AppSettings();
            }
            catch (SecurityException)
            {
                return new AppSettings();
            }
        }

        internal static AppSettings ParseLines(IEnumerable<string> lines)
        {
            AppSettings settings = new AppSettings();
            if (lines == null)
            {
                return settings;
            }

            foreach (string rawLine in lines)
            {
                string line = rawLine == null ? string.Empty : rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal))
                {
                    continue;
                }

                int separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, separatorIndex).Trim();
                string value = line.Substring(separatorIndex + 1).Trim();
                ButtonAction parsedAction;
                if (!TryParseAction(value, out parsedAction))
                {
                    continue;
                }

                if (string.Equals(key, "BackAction", StringComparison.OrdinalIgnoreCase))
                {
                    settings.BackAction = parsedAction;
                }
                else if (string.Equals(key, "ForwardAction", StringComparison.OrdinalIgnoreCase))
                {
                    settings.ForwardAction = parsedAction;
                }
            }

            return settings;
        }

        internal bool TrySave()
        {
            string directory = Path.GetDirectoryName(SettingsPath);
            string temporaryPath = SettingsPath + ".tmp";

            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string[] lines = new string[]
                {
                    "# XButtonMapper settings",
                    "BackAction=" + BackAction,
                    "ForwardAction=" + ForwardAction
                };

                File.WriteAllLines(temporaryPath, lines, new UTF8Encoding(false));

                if (File.Exists(SettingsPath))
                {
                    try
                    {
                        File.Replace(temporaryPath, SettingsPath, null);
                    }
                    catch (IOException)
                    {
                        File.Copy(temporaryPath, SettingsPath, true);
                        File.Delete(temporaryPath);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(temporaryPath, SettingsPath, true);
                        File.Delete(temporaryPath);
                    }
                }
                else
                {
                    File.Move(temporaryPath, SettingsPath);
                }

                return true;
            }
            catch (IOException)
            {
                TryDeleteTemporaryFile(temporaryPath);
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                TryDeleteTemporaryFile(temporaryPath);
                return false;
            }
            catch (SecurityException)
            {
                TryDeleteTemporaryFile(temporaryPath);
                return false;
            }
        }

        private static bool TryParseAction(string value, out ButtonAction action)
        {
            if (Enum.TryParse<ButtonAction>(value, true, out action) && Enum.IsDefined(typeof(ButtonAction), action))
            {
                return true;
            }

            action = ButtonAction.PassThrough;
            return false;
        }

        private static void TryDeleteTemporaryFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }
    }
}
