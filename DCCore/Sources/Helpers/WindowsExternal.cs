using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace D2Tools.Helpers
{
    public static class WindowsExternal
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
            uint processAccess,
            bool bInheritHandle,
            int processId
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(
            IntPtr hObject
        );

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead);

        // This helper static method is required because the 32-bit version of user32.dll does not contain this API
        // (on any versions of Windows), so linking the method will fail at run-time. The bridge dispatches the request
        // to the correct function (GetWindowLong in 32-bit mode and GetWindowLongPtr in 64-bit mode)

        // If that doesn't work, the following signature can be used alternatively.
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        // This static method is required because Win32 does not support
        // GetWindowLongPtr directly
        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return GetWindowLongPtr32(hWnd, nIndex);
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        
        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, ref Rect lpRect);
        
        public static Rectangle GetWindowRectWithoutBorders(IntPtr handle, out Point topLeftCorner)
        {
            var windowRect = new Rect();
            GetWindowRect(handle, ref windowRect);

            var clientRect = new Rect();
            GetClientRect(handle, ref clientRect);

            int borderSize = ((windowRect.Right - windowRect.Left) - (clientRect.Right - clientRect.Left)) / 2;
            int titleBarSize = ((windowRect.Bottom - windowRect.Top) - (clientRect.Bottom - clientRect.Top)) - borderSize;

            topLeftCorner = new Point(windowRect.Left + borderSize, windowRect.Top + titleBarSize);
            
            var bounds = new Rectangle(topLeftCorner.X, topLeftCorner.Y, windowRect.Right - windowRect.Left - borderSize * 2, windowRect.Bottom - windowRect.Top - titleBarSize - borderSize);
        
            if(bounds.Width <= 0 || bounds.Height <= 0)
                return new Rectangle(0,0,0,0);

            return bounds;
        }

        public static readonly uint WINEVENT_OUTOFCONTEXT = 0;
        public static readonly uint EVENT_SYSTEM_FOREGROUND = 3;
        
        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        
        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);
    }
}
