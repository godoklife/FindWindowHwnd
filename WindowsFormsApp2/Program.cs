using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    internal class Program
    {
        private static void Main()
        {
            MouseHook.Start(); // 마우스 후킹 시작

            MouseHook.MouseAction += (sender, e) =>
                                     {
                                         if (e.Button == MouseButtons.Left)
                                         {
                                             Point mousePosition = Cursor.Position;
                                             IntPtr windowHandle = GetWindowHandleAtMousePosition(mousePosition);
                                             Debug.WriteLine("Window Handle: " + windowHandle);
                                         }
                                     };

            Application.Run(); // 이벤트 루프 실행
        }

        private static IntPtr GetWindowHandleAtMousePosition(Point mousePosition)
        {
            IntPtr monitorHandle = MonitorFromPoint(mousePosition, MonitorOptions.MONITOR_DEFAULTTONEAREST);
            IntPtr windowHandle = IntPtr.Zero;

            EnumWindows((hwnd, lParam) =>
                        {
                            Rect windowRect;
                            GetWindowRect(hwnd, out windowRect);

                            if (windowRect.Contains(mousePosition) && IsWindowVisible(hwnd))
                            {
                                uint processId;
                                GetWindowThreadProcessId(hwnd, out processId);

                                if (processId != 0)
                                {
                                    IntPtr monitorHandleOfWindow = MonitorFromWindow(hwnd, MonitorOptions.MONITOR_DEFAULTTONEAREST);
                                    if (monitorHandleOfWindow == monitorHandle)
                                    {
                                        windowHandle = hwnd;
                                        return false; // 열거 중단
                                    }
                                }
                            }

                            return true; // 계속 열거
                        }, IntPtr.Zero);

            return windowHandle;
        }

        #region Native Methods
        private delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsDelegate lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern void GetWindowRect(IntPtr hWnd, out Rect lpRect);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, MonitorOptions dwFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(Point pt, MonitorOptions dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public readonly int Left;
            public readonly int Top;
            public readonly int Right;
            public readonly int Bottom;

            public bool Contains(Point point)
            {
                return point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
            }
        }

        [Flags]
        private enum MonitorOptions : uint
        {
            MONITOR_DEFAULTTONULL = 0x00000000,
            MONITOR_DEFAULTTOPRIMARY = 0x00000001,
            MONITOR_DEFAULTTONEAREST = 0x00000002
        }
        #endregion

        #region Mouse Hook
        private static class MouseHook
        {
            private static IntPtr hookHandle;
            private static Win32Api.HookProc hookProcedure;

            public static event EventHandler<MouseEventArgs> MouseAction;

            public static void Start()
            {
                hookProcedure = HookCallback;
                hookHandle = Win32Api.SetWindowsHookEx(Win32Api.HookType.WH_MOUSE_LL, hookProcedure, IntPtr.Zero, 0);
            }

            public static void Stop()
            {
                Win32Api.UnhookWindowsHookEx(hookHandle);
            }

            private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
            {
                if (nCode >= 0 && MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam)
                {
                    MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    MouseButtons button = MouseButtons.Left;
                    MouseEventArgs e = new MouseEventArgs(button, 1, hookStruct.pt.x, hookStruct.pt.y, 0);
                    MouseAction?.Invoke(null, e);
                }

                return Win32Api.CallNextHookEx(hookHandle, nCode, wParam, lParam);
            }
        }

        private static class Win32Api
        {
            public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr SetWindowsHookEx(HookType hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

            public enum HookType : int
            {
                WH_MOUSE_LL = 14
            }
        }

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201
        }

        private struct POINT
        {
            public int x;
            public int y;
        }

        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        #endregion
    }
}