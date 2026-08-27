using System;
using System.Runtime.InteropServices;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// Win32 原生屏幕几何查询。
    /// </summary>
    public static class ScreenNative
    {
        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// 计算窗口与其所在显示器工作区的可见面积比例（0 到 1）。
        /// 查询失败时按完全可见处理，以阻止错误的侧隐判定。
        /// </summary>
        public static double GetVisibleFraction(IntPtr hwnd)
        {
            try
            {
                if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var windowRect))
                    return 1;

                var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                if (monitor == IntPtr.Zero)
                    return 1;

                var monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                if (!GetMonitorInfo(monitor, ref monitorInfo))
                    return 1;

                long windowWidth = (long)windowRect.Right - windowRect.Left;
                long windowHeight = (long)windowRect.Bottom - windowRect.Top;
                if (windowWidth <= 0 || windowHeight <= 0)
                    return 1;

                long intersectionWidth = Math.Min(windowRect.Right, monitorInfo.rcWork.Right) - Math.Max(windowRect.Left, monitorInfo.rcWork.Left);
                long intersectionHeight = Math.Min(windowRect.Bottom, monitorInfo.rcWork.Bottom) - Math.Max(windowRect.Top, monitorInfo.rcWork.Top);
                if (intersectionWidth < 0) intersectionWidth = 0;
                if (intersectionHeight < 0) intersectionHeight = 0;

                return (double)(intersectionWidth * intersectionHeight) / (windowWidth * windowHeight);
            }
            catch
            {
                return 1;
            }
        }
    }
}
