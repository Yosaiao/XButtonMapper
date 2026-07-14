using System;

namespace XButtonMapper
{
    internal static class SelfTest
    {
        internal static int Run()
        {
            try
            {
                string layoutError = NativeMethods.GetLayoutValidationError();
                if (layoutError != null)
                {
                    return 10;
                }

                AppSettings parsed = AppSettings.ParseLines(new string[]
                {
                    "BackAction=Paste",
                    "ForwardAction=PassThrough"
                });

                if (parsed.BackAction != ButtonAction.Paste || parsed.ForwardAction != ButtonAction.PassThrough)
                {
                    return 11;
                }

                AppSettings invalid = AppSettings.ParseLines(new string[]
                {
                    "BackAction=Unknown",
                    "ForwardAction=Copy"
                });

                if (invalid.BackAction != ButtonAction.Copy || invalid.ForwardAction != ButtonAction.Copy)
                {
                    return 12;
                }

                if (StartupManager.BuildCommandLine(@"C:\Program Files\XButtonMapper\XButtonMapper.exe") !=
                    "\"C:\\Program Files\\XButtonMapper\\XButtonMapper.exe\"")
                {
                    return 13;
                }

                return 0;
            }
            catch
            {
                return 99;
            }
        }
    }
}
