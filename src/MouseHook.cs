using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace XButtonMapper
{
    internal sealed class MouseHook : IDisposable
    {
        private static readonly UIntPtr InputMarker = CreateInputMarker();

        private readonly NativeMethods.LowLevelMouseProc _hookCallback;
        private IntPtr _hookHandle;
        private bool _xButton1Consumed;
        private bool _xButton2Consumed;
        private bool _disposed;

        internal MouseHook()
        {
            string layoutError = NativeMethods.GetLayoutValidationError();
            if (layoutError != null)
            {
                throw new InvalidOperationException(layoutError);
            }

            Enabled = true;
            BackAction = ButtonAction.Copy;
            ForwardAction = ButtonAction.Paste;
            _hookCallback = HookCallback;

            IntPtr moduleHandle = NativeMethods.GetModuleHandle(null);
            _hookHandle = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_MOUSE_LL,
                _hookCallback,
                moduleHandle,
                0);

            if (_hookHandle == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "无法安装鼠标监听钩子。");
            }
        }

        internal bool Enabled { get; set; }

        internal ButtonAction BackAction { get; set; }

        internal ButtonAction ForwardAction { get; set; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_hookHandle != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= NativeMethods.HC_ACTION)
                {
                    int message = unchecked((int)wParam.ToInt64());
                    if (message == NativeMethods.WM_XBUTTONDOWN || message == NativeMethods.WM_XBUTTONUP)
                    {
                        NativeMethods.MSLLHOOKSTRUCT hookData =
                            (NativeMethods.MSLLHOOKSTRUCT)Marshal.PtrToStructure(
                                lParam,
                                typeof(NativeMethods.MSLLHOOKSTRUCT));

                        uint button = (hookData.mouseData >> 16) & 0xFFFF;
                        bool isDown = message == NativeMethods.WM_XBUTTONDOWN;
                        if (HandleButton(button, isDown))
                        {
                            return new IntPtr(1);
                        }
                    }
                }
            }
            catch
            {
                // Never let an exception cross the unmanaged hook boundary.
            }

            return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        private bool HandleButton(uint button, bool isDown)
        {
            if (button == NativeMethods.XBUTTON1)
            {
                if (!isDown)
                {
                    bool consumed = _xButton1Consumed;
                    _xButton1Consumed = false;
                    return consumed;
                }

                bool shouldConsume = Enabled && BackAction != ButtonAction.PassThrough;
                _xButton1Consumed = shouldConsume;
                if (shouldConsume)
                {
                    // Mapped buttons remain intercepted even if the focused application rejects SendInput.
                    SendShortcut(BackAction);
                }

                return shouldConsume;
            }

            if (button == NativeMethods.XBUTTON2)
            {
                if (!isDown)
                {
                    bool consumed = _xButton2Consumed;
                    _xButton2Consumed = false;
                    return consumed;
                }

                bool shouldConsume = Enabled && ForwardAction != ButtonAction.PassThrough;
                _xButton2Consumed = shouldConsume;
                if (shouldConsume)
                {
                    // This prevents an unexpected browser navigation when a mapped shortcut is rejected.
                    SendShortcut(ForwardAction);
                }

                return shouldConsume;
            }

            return false;
        }

        private static bool SendShortcut(ButtonAction action)
        {
            int virtualKey = action == ButtonAction.Copy ? NativeMethods.VK_C : NativeMethods.VK_V;
            if (action == ButtonAction.CopyWithInsert)
            {
                virtualKey = NativeMethods.VK_INSERT;
            }
            if (IsKeyDown(virtualKey))
            {
                return false;
            }

            bool controlAlreadyDown =
                IsKeyDown(NativeMethods.VK_LCONTROL) ||
                IsKeyDown(NativeMethods.VK_RCONTROL);

            NativeMethods.INPUT[] inputs = new NativeMethods.INPUT[controlAlreadyDown ? 2 : 4];
            uint thread = NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(), IntPtr.Zero);
            IntPtr layout = NativeMethods.GetKeyboardLayout(thread);
            int index = 0;
            if (!controlAlreadyDown)
            {
                inputs[index++] = CreateKeyboardInput(NativeMethods.VK_LCONTROL, false, layout);
            }

            inputs[index++] = CreateKeyboardInput(virtualKey, false, layout);
            inputs[index++] = CreateKeyboardInput(virtualKey, true, layout);

            if (!controlAlreadyDown)
            {
                inputs[index] = CreateKeyboardInput(NativeMethods.VK_LCONTROL, true, layout);
            }

            uint sent = NativeMethods.SendInput(
                (uint)inputs.Length,
                inputs,
                Marshal.SizeOf(typeof(NativeMethods.INPUT)));

            if (sent != inputs.Length)
            {
                ReleasePartiallyInsertedKeys(sent, controlAlreadyDown, virtualKey, layout);
            }

            return sent == inputs.Length;
        }

        private static void ReleasePartiallyInsertedKeys(uint sent, bool controlAlreadyDown, int virtualKey, IntPtr layout)
        {
            bool targetKeyMayBeDown = controlAlreadyDown ? sent >= 1 : sent >= 2;
            bool targetKeyUpWasInserted = controlAlreadyDown ? sent >= 2 : sent >= 3;
            bool synthesizedControlMayBeDown = !controlAlreadyDown && sent >= 1;
            bool synthesizedControlUpWasInserted = !controlAlreadyDown && sent >= 4;

            NativeMethods.INPUT[] cleanup = new NativeMethods.INPUT[2];
            int cleanupCount = 0;

            if (targetKeyMayBeDown && !targetKeyUpWasInserted)
            {
                cleanup[cleanupCount++] = CreateKeyboardInput(virtualKey, true, layout);
            }

            if (synthesizedControlMayBeDown && !synthesizedControlUpWasInserted)
            {
                cleanup[cleanupCount++] = CreateKeyboardInput(NativeMethods.VK_LCONTROL, true, layout);
            }

            if (cleanupCount == 0)
            {
                return;
            }

            if (cleanupCount != cleanup.Length)
            {
                NativeMethods.INPUT[] trimmedCleanup = new NativeMethods.INPUT[cleanupCount];
                Array.Copy(cleanup, trimmedCleanup, cleanupCount);
                cleanup = trimmedCleanup;
            }

            NativeMethods.SendInput(
                (uint)cleanup.Length,
                cleanup,
                Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }

        internal static NativeMethods.INPUT CreateKeyboardInput(int virtualKey, bool keyUp, IntPtr layout)
        {
            NativeMethods.INPUT input = new NativeMethods.INPUT();
            input.type = NativeMethods.INPUT_KEYBOARD;
            input.data.ki = new NativeMethods.KEYBDINPUT();
            input.data.ki.wVk = unchecked((ushort)virtualKey);
            ushort scanCode = (ushort)NativeMethods.MapVirtualKeyEx((uint)virtualKey, NativeMethods.MAPVK_VK_TO_VSC, layout);
            input.data.ki.wScan = scanCode;
            input.data.ki.dwFlags = keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0;
            if (virtualKey == NativeMethods.VK_INSERT)
            {
                // The dedicated Insert key uses the extended form of the keypad scan code.
                input.data.ki.dwFlags |= NativeMethods.KEYEVENTF_EXTENDEDKEY;
            }
            if (scanCode != 0)
            {
                input.data.ki.wVk = 0;
                input.data.ki.dwFlags |= NativeMethods.KEYEVENTF_SCANCODE;
            }

            input.data.ki.time = 0;
            input.data.ki.dwExtraInfo = InputMarker;
            return input;
        }

        private static bool IsKeyDown(int virtualKey)
        {
            return (NativeMethods.GetAsyncKeyState(virtualKey) & 0x8000) != 0;
        }

        private static UIntPtr CreateInputMarker()
        {
            return UIntPtr.Size == 8
                ? new UIntPtr(0x58424D500001UL)
                : new UIntPtr(0x58424D50U);
        }
    }
}
