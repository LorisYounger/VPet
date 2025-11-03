using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace VPet_Simulator.Windows.Function
{
    /// <summary>
    /// 窗口管理器
    /// </summary>
    public class WindowManager
    {
        /// <summary>
        /// 窗口信息列表
        /// </summary>
        public List<WindowInfo> WindowInfos { get; private set; }
        
        /// <summary>
        /// 当前桌宠窗口句柄
        /// </summary>
        private IntPtr _petHandle;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="petHandle">桌宠窗口句柄</param>
        public WindowManager(IntPtr petHandle)
        {
            _petHandle = petHandle;
            WindowInfos = new List<WindowInfo>();
            RefreshWindowList();
        }
        
        /// <summary>
        /// 刷新窗口列表
        /// </summary>
        public void RefreshWindowList()
        {
            WindowInfos.Clear();
            
            // 枚举所有顶层窗口
            Win32.User32.EnumWindows((handle, lParam) =>
            {
                // 跳过自身窗口
                if (handle == _petHandle)
                    return true;
                
                WindowInfo windowInfo = new WindowInfo(handle);
                
                // 只添加有效的窗口
                if (windowInfo.IsValidWindow)
                {
                    WindowInfos.Add(windowInfo);
                }
                
                return true;
            }, IntPtr.Zero);
        }
        
        /// <summary>
        /// 获取当前鼠标位置下的窗口
        /// </summary>
        /// <returns>窗口信息，如果没有找到则返回null</returns>
        public WindowInfo GetWindowUnderMouse()
        {
            // 获取当前鼠标位置
            Point mousePos = System.Windows.Forms.Control.MousePosition;
            
            // 获取鼠标位置下的窗口句柄
            IntPtr windowHandle = Win32.User32.WindowFromPoint(new Win32.User32.POINT { x = mousePos.X, y = mousePos.Y });
            
            // 如果窗口句柄是桌宠自身，则获取父窗口
            if (windowHandle == _petHandle)
            {
                windowHandle = Win32.User32.GetParent(windowHandle);
            }
            
            // 如果找到窗口，则返回窗口信息
            if (windowHandle != IntPtr.Zero)
            {
                return new WindowInfo(windowHandle);
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取指定位置下的窗口
        /// </summary>
        /// <param name="point">位置</param>
        /// <returns>窗口信息，如果没有找到则返回null</returns>
        public WindowInfo GetWindowAtPosition(Point point)
        {
            // 获取指定位置下的窗口句柄
            IntPtr windowHandle = Win32.User32.WindowFromPoint(new Win32.User32.POINT { x = point.X, y = point.Y });
            
            // 如果窗口句柄是桌宠自身，则获取父窗口
            if (windowHandle == _petHandle)
            {
                windowHandle = Win32.User32.GetParent(windowHandle);
            }
            
            // 如果找到窗口，则返回窗口信息
            if (windowHandle != IntPtr.Zero)
            {
                return new WindowInfo(windowHandle);
            }
            
            return null;
        }
        
        /// <summary>
        /// 检查指定矩形是否触碰到任何窗口边缘
        /// </summary>
        /// <param name="rect">要检查的矩形</param>
        /// <returns>触碰到的窗口信息，如果没有触碰到则返回null</returns>
        public WindowInfo CheckWindowEdge(System.Windows.Rect rect)
        {
            // 刷新窗口列表
            RefreshWindowList();
            
            // 遍历每个窗口，检查是否触碰到边缘
            foreach (var window in WindowInfos)
            {
                if (!window.IsValidWindow)
                    continue;
                
                // 计算矩形与窗口的边缘距离
                double distanceLeft = Math.Abs(rect.Right - window.Rect.Left);
                double distanceRight = Math.Abs(rect.Left - window.Rect.Right);
                double distanceTop = Math.Abs(rect.Bottom - window.Rect.Top);
                double distanceBottom = Math.Abs(rect.Top - window.Rect.Bottom);
                
                // 定义触发穿梭的阈值
                const double shuttleThreshold = 10.0;
                
                // 检查是否触碰到窗口的任何边缘
                if (distanceLeft < shuttleThreshold || distanceRight < shuttleThreshold ||
                    distanceTop < shuttleThreshold || distanceBottom < shuttleThreshold)
                {
                    return window;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取穿梭到目标窗口另一侧的位置
        /// </summary>
        /// <param name="window">目标窗口</param>
        /// <param name="petBounds">宠物的边界矩形</param>
        /// <returns>穿梭后的位置</returns>
        public System.Windows.Point GetShuttleTargetPosition(WindowInfo window, System.Windows.Rect petBounds)
        {
            // 计算矩形与窗口的边缘距离
            double distanceLeft = Math.Abs(petBounds.Right - window.Rect.Left);
            double distanceRight = Math.Abs(petBounds.Left - window.Rect.Right);
            double distanceTop = Math.Abs(petBounds.Bottom - window.Rect.Top);
            double distanceBottom = Math.Abs(petBounds.Top - window.Rect.Bottom);
            
            // 找到最近的边缘
            double minDistance = Math.Min(Math.Min(distanceLeft, distanceRight), Math.Min(distanceTop, distanceBottom));
            
            System.Windows.Point newPosition = new System.Windows.Point();
            
            // 根据最近的边缘计算目标位置
            if (minDistance == distanceLeft)
            {
                // 穿梭到窗口的右侧
                newPosition.X = window.Rect.Right + 10;
                newPosition.Y = petBounds.Y + (window.Rect.Height - petBounds.Height) / 2;
            }
            else if (minDistance == distanceRight)
            {
                // 穿梭到窗口的左侧
                newPosition.X = window.Rect.Left - petBounds.Width - 10;
                newPosition.Y = petBounds.Y + (window.Rect.Height - petBounds.Height) / 2;
            }
            else if (minDistance == distanceTop)
            {
                // 穿梭到窗口的下方
                newPosition.X = petBounds.X + (window.Rect.Width - petBounds.Width) / 2;
                newPosition.Y = window.Rect.Bottom + 10;
            }
            else if (minDistance == distanceBottom)
            {
                // 穿梭到窗口的上方
                newPosition.X = petBounds.X + (window.Rect.Width - petBounds.Width) / 2;
                newPosition.Y = window.Rect.Top - petBounds.Height - 10;
            }
            
            return newPosition;
        }
        
        /// <summary>
        /// 获取所有可见窗口
        /// </summary>
        /// <returns>可见窗口列表</returns>
        public List<WindowInfo> GetVisibleWindows()
        {
            return WindowInfos.Where(w => w.IsValidWindow).ToList();
        }
        
        /// <summary>
        /// 获取随机窗口
        /// </summary>
        /// <returns>随机窗口信息，如果没有找到则返回null</returns>
        public WindowInfo GetRandomWindow()
        {
            List<WindowInfo> visibleWindows = GetVisibleWindows();
            if (visibleWindows.Count == 0)
                return null;
            
            Random random = new Random();
            return visibleWindows[random.Next(visibleWindows.Count)];
        }
        
        /// <summary>
        /// 检查窗口是否在指定窗口内部
        /// </summary>
        /// <param name="windowInfo">窗口信息</param>
        /// <param name="containerWindow">容器窗口</param>
        /// <returns>如果在内部则返回true，否则返回false</returns>
        public bool IsWindowInside(WindowInfo windowInfo, WindowInfo containerWindow)
        {
            if (windowInfo == null || containerWindow == null)
                return false;
            
            return windowInfo.Bounds.X >= containerWindow.Bounds.X &&
                   windowInfo.Bounds.Y >= containerWindow.Bounds.Y &&
                   windowInfo.Bounds.Right <= containerWindow.Bounds.Right &&
                   windowInfo.Bounds.Bottom <= containerWindow.Bounds.Bottom;
        }
        
        /// <summary>
        /// 获取窗口层级
        /// </summary>
        /// <param name="windowInfo">窗口信息</param>
        /// <returns>窗口层级</returns>
        public int GetWindowZOrder(WindowInfo windowInfo)
        {
            if (windowInfo == null)
                return -1;
            
            int zOrder = 0;
            IntPtr currentWindow = windowInfo.Handle;
            
            while (currentWindow != IntPtr.Zero)
            {
                zOrder++;
                currentWindow = Win32.User32.GetWindow(currentWindow, Win32.User32.GW_HWNDPREV);
            }
            
            return zOrder;
        }
        
        /// <summary>
        /// 设置窗口层级
        /// </summary>
        /// <param name="windowInfo">窗口信息</param>
        /// <param name="zOrder">层级</param>
        public void SetWindowZOrder(WindowInfo windowInfo, int zOrder)
        {
            if (windowInfo == null)
                return;
            
            // 获取最顶层窗口
            IntPtr topWindow = Win32.User32.GetTopWindow(IntPtr.Zero);
            
            // 如果要设置的层级是最顶层
            if (zOrder <= 1)
            {
                Win32.User32.SetWindowPos(windowInfo.Handle, Win32.User32.HWND_TOP, 0, 0, 0, 0,
                    Win32.User32.SWP_NOMOVE | Win32.User32.SWP_NOSIZE | Win32.User32.SWP_NOACTIVATE);
                return;
            }
            
            // 找到目标层级的窗口
            IntPtr targetWindow = topWindow;
            for (int i = 1; i < zOrder && targetWindow != IntPtr.Zero; i++)
            {
                targetWindow = Win32.User32.GetWindow(targetWindow, Win32.User32.GW_HWNDNEXT);
            }
            
            // 如果找到目标窗口，则将当前窗口设置到目标窗口下方
            if (targetWindow != IntPtr.Zero)
            {
                Win32.User32.SetWindowPos(windowInfo.Handle, targetWindow, 0, 0, 0, 0,
                    Win32.User32.SWP_NOMOVE | Win32.User32.SWP_NOSIZE | Win32.User32.SWP_NOACTIVATE);
            }
            else
            {
                // 如果没有找到目标窗口，则将当前窗口设置到最底层
                Win32.User32.SetWindowPos(windowInfo.Handle, Win32.User32.HWND_BOTTOM, 0, 0, 0, 0,
                    Win32.User32.SWP_NOMOVE | Win32.User32.SWP_NOSIZE | Win32.User32.SWP_NOACTIVATE);
            }
        }
        
        /// <summary>
        /// 将桌宠窗口设置到指定窗口后方
        /// </summary>
        /// <param name="targetWindow">目标窗口</param>
        public void SetPetWindowBehind(WindowInfo targetWindow)
        {
            if (targetWindow == null)
                return;
            
            // 将桌宠窗口设置到目标窗口的后方
            Win32.User32.SetWindowPos(_petHandle, targetWindow.Handle, 0, 0, 0, 0,
                Win32.User32.SWP_NOMOVE | Win32.User32.SWP_NOSIZE | Win32.User32.SWP_NOACTIVATE);
        }
        
        /// <summary>
        /// 将桌宠窗口设置到最顶层
        /// </summary>
        public void SetPetWindowTopmost()
        {
            Win32.User32.SetWindowPos(_petHandle, Win32.User32.HWND_TOPMOST, 0, 0, 0, 0,
                Win32.User32.SWP_NOMOVE | Win32.User32.SWP_NOSIZE | Win32.User32.SWP_NOACTIVATE);
        }
        
        /// <summary>
        /// 将桌宠窗口设置到正常层级
        /// </summary>
        public void SetPetWindowNormal()
        {
            Win32.User32.SetWindowPos(_petHandle, Win32.User32.HWND_NOTOPMOST, 0, 0, 0, 0,
                Win32.User32.SWP_NOMOVE | Win32.User32.SWP_NOSIZE | Win32.User32.SWP_NOACTIVATE);
        }
    }
}