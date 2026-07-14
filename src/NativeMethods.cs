using System;
using System.Runtime.InteropServices;

namespace XButtonMapper
{
    internal static class NativeMethods
    {
        internal const int WH_MOUSE_LL = 14;
        internal const int HC_ACTION = 0;
        internal const int WM_XBUTTONDOWN = 0x020B;
        internal const int WM_XBUTTONUP = 0x020C;
        internal const int WM_APP_ACTIVATE = 0x8000 + 77;
        internal const uint XBUTTON1 = 0x0001;
        internal const uint XBUTTON2 = 0x0002;
        internal const uint INPUT_KEYBOARD = 1;
        internal const uint KEYEVENTF_KEYUP = 0x0002;
        internal const int VK_LCONTROL = 0xA2;
        internal const int VK_RCONTROL = 0xA3;
        internal const int VK_C = 0x43;
        internal const int VK_V = 0x56;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            internal int x;
            internal int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MSLLHOOKSTRUCT
        {
            internal POINT pt;
            internal uint mouseData;
            internal uint flags;
            internal uint time;
            internal UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            internal int dx;
            internal int dy;
            internal uint mouseData;
            internal uint dwFlags;
            internal uint time;
            internal UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            internal ushort wVk;
            internal ushort wScan;
            internal uint dwFlags;
            internal uint time;
            internal UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HARDWAREINPUT
        {
            internal uint uMsg;
            internal ushort wParamL;
            internal ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct InputUnion
        {
            [FieldOffset(0)]
            internal MOUSEINPUT mi;

            [FieldOffset(0)]
            internal KEYBDINPUT ki;

            [FieldOffset(0)]
            internal HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct INPUT
        {
            internal uint type;
            internal InputUnion data;
        }

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelMouseProc lpfn,
            IntPtr hMod,
            uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        internal static extern IntPtr CallNextHookEx(
            IntPtr hhk,
            int nCode,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SendInput(
            uint nInputs,
            [In] INPUT[] pInputs,
            int cbSize);

        [DllImport("user32.dll")]
        internal static extern short GetAsyncKeyState(int vKey);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        internal static string GetLayoutValidationError()
        {
            int expectedInputSize = IntPtr.Size == 8 ? 40 : 28;
            int expectedKeyboardInputSize = IntPtr.Size == 8 ? 24 : 16;
            int expectedMouseInputSize = IntPtr.Size == 8 ? 32 : 24;
            int expectedMouseHookSize = IntPtr.Size == 8 ? 32 : 24;

            if (Marshal.SizeOf(typeof(INPUT)) != expectedInputSize)
            {
                return "INPUT 结构大小不正确。";
            }

            if (Marshal.SizeOf(typeof(KEYBDINPUT)) != expectedKeyboardInputSize)
            {
                return "KEYBDINPUT 结构大小不正确。";
            }

            if (Marshal.SizeOf(typeof(MOUSEINPUT)) != expectedMouseInputSize)
            {
                return "MOUSEINPUT 结构大小不正确。";
            }

            if (Marshal.SizeOf(typeof(MSLLHOOKSTRUCT)) != expectedMouseHookSize)
            {
                return "MSLLHOOKSTRUCT 结构大小不正确。";
            }

            return null;
        }
    }
}
