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
        /// 获取窗口所在显示器的完整屏幕范围(含任务栏)物理像素
        /// 多屏环境下不同显示器尺寸可能不同, 距离判定需要以窗口实际所在的屏幕为准;
        /// 与原有按主屏全尺寸判断的行为保持一致, 这里同样取含任务栏的整块屏幕区域
        /// 返回false表示查询失败, 调用方应回退到原有逻辑
        /// </summary>
        public static bool TryGetMonitorBounds(IntPtr hwnd, out int x, out int y, out int width, out int height)
        {
            x = y = width = height = 0;
            try
            {
                if (hwnd == IntPtr.Zero) return false;
                var mon = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                if (mon == IntPtr.Zero) return false;
                var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                if (!GetMonitorInfo(mon, ref mi)) return false;
                x = mi.rcMonitor.Left;
                y = mi.rcMonitor.Top;
                width = mi.rcMonitor.Right - mi.rcMonitor.Left;
                height = mi.rcMonitor.Bottom - mi.rcMonitor.Top;
                return true;
            }
            catch { return false; }
        }

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
