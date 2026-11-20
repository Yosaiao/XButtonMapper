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

                IntPtr layout = NativeMethods.GetKeyboardLayout(0);
                foreach (int key in new int[] { NativeMethods.VK_LCONTROL, NativeMethods.VK_C, NativeMethods.VK_V })
                {
                    NativeMethods.KEYBDINPUT down = MouseHook.CreateKeyboardInput(key, false, layout).data.ki;
                    NativeMethods.KEYBDINPUT up = MouseHook.CreateKeyboardInput(key, true, layout).data.ki;
                    if (down.wScan == 0 || down.wScan != up.wScan || down.wVk != 0 || up.wVk != 0 ||
                        down.dwFlags != NativeMethods.KEYEVENTF_SCANCODE ||
                        up.dwFlags != (NativeMethods.KEYEVENTF_SCANCODE | NativeMethods.KEYEVENTF_KEYUP))
                    {
                        return 14;
                    }
                }

                AppSettings alternate = AppSettings.ParseLines(new string[] { "BackAction=CopyWithInsert" });
                NativeMethods.KEYBDINPUT insert = MouseHook.CreateKeyboardInput(NativeMethods.VK_INSERT, false, layout).data.ki;
                NativeMethods.KEYBDINPUT release = MouseHook.CreateKeyboardInput(NativeMethods.VK_INSERT, true, layout).data.ki;
                if (alternate.BackAction != ButtonAction.CopyWithInsert || alternate.ForwardAction != ButtonAction.Paste ||
                    insert.wScan == 0 || release.wScan != insert.wScan ||
                    insert.dwFlags != (NativeMethods.KEYEVENTF_SCANCODE | NativeMethods.KEYEVENTF_EXTENDEDKEY) ||
                    release.dwFlags != (insert.dwFlags | NativeMethods.KEYEVENTF_KEYUP))
                {
                    return 15;
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
