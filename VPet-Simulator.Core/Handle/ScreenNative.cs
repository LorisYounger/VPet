using System;
using System.Runtime.InteropServices;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// Win32 原生屏幕几何查询 (物理像素, 不受 WPF DPI 换算影响)
    /// 用于 #546: 多屏/不同缩放下逻辑坐标可能失真, 关键判定需以物理边界为准
    /// </summary>
    public static class ScreenNative
    {
        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

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
        /// 计算窗口与其所在显示器工作区的可见面积比例 (0~1)
        /// 窗口句柄无效或查询失败时返回 1 (视为完全可见, 不触发保护逻辑)
        /// </summary>
        public static double GetVisibleFraction(IntPtr hwnd)
        {
            try
            {
                if (hwnd == IntPtr.Zero) return 1;
                if (!GetWindowRect(hwnd, out var wr)) return 1;
                var mon = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                if (mon == IntPtr.Zero) return 1;
                var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                if (!GetMonitorInfo(mon, ref mi)) return 1;

                long winW = (long)wr.Right - wr.Left, winH = (long)wr.Bottom - wr.Top;
                if (winW <= 0 || winH <= 0) return 1;

                long ix = Math.Min(wr.Right, mi.rcWork.Right) - Math.Max(wr.Left, mi.rcWork.Left);
                long iy = Math.Min(wr.Bottom, mi.rcWork.Bottom) - Math.Max(wr.Top, mi.rcWork.Top);
                if (ix < 0) ix = 0;
                if (iy < 0) iy = 0;

                return (double)(ix * iy) / (winW * winH);
            }
            catch { return 1; }
        }
    }
}
