using System;
using System.Drawing;
using System.Text;
using System.Runtime.InteropServices;

namespace VPet_Simulator.Windows.Function
{
    /// <summary>
    /// 窗口信息类
    /// </summary>
    public class WindowInfo
    {
        /// <summary>
        /// 窗口句柄
        /// </summary>
        public IntPtr Handle { get; set; }
        /// <summary>
        /// 窗口标题
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 窗口位置和大小
        /// </summary>
        public Rectangle Bounds { get; set; }
        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible { get; set; }
        /// <summary>
        /// 窗口类名
        /// </summary>
        public string ClassName { get; set; }
        /// <summary>
        /// 是否是最小化窗口
        /// </summary>
        public bool IsMinimized { get; set; }
        /// <summary>
        /// 是否是最大化窗口
        /// </summary>
        public bool IsMaximized { get; set; }
        /// <summary>
        /// 窗口进程ID
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 窗口线程ID
        /// </summary>
        public int ThreadId { get; set; }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="handle">窗口句柄</param>
        public WindowInfo(IntPtr handle)
        {
            Handle = handle;
            
            // 获取窗口标题
            int titleLength = Win32.User32.GetWindowTextLength(handle);
            if (titleLength > 0)
            {
                StringBuilder titleBuilder = new StringBuilder(titleLength + 1);
                Win32.User32.GetWindowText(handle, titleBuilder, titleBuilder.Capacity);
                Title = titleBuilder.ToString();
            }
            else
            {
                Title = string.Empty;
            }
            
            // 获取窗口矩形
            Win32.User32.GetWindowRect(handle, out Win32.User32.RECT rect);
            Bounds = new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
            
            IsVisible = Win32.User32.IsWindowVisible(handle);
            
            // 获取窗口类名
            StringBuilder classNameBuilder = new StringBuilder(256);
            Win32.User32.GetClassName(handle, classNameBuilder, classNameBuilder.Capacity);
            ClassName = classNameBuilder.ToString();
            
            IsMinimized = Win32.User32.IsIconic(handle);
            IsMaximized = Win32.User32.IsZoomed(handle);
            ThreadId = Win32.User32.GetWindowThreadProcessId(handle, out int processId);
            ProcessId = processId;
        }
        /// <summary>
        /// 是否是有效的窗口（非最小化、可见、有标题）
        /// </summary>
        public bool IsValidWindow
        {
            get
            {
                return IsVisible && !IsMinimized && !string.IsNullOrWhiteSpace(Title) && Bounds.Width > 0 && Bounds.Height > 0;
            }
        }
    }
}