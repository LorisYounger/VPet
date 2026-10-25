using Avalonia.Controls;
using System;
using System.Runtime.InteropServices;

namespace VPet_Simulator.MutiPlatform;

/// <summary>X11/XWayland 的 EWMH 工作区请求。原生 Wayland 由桌面环境管理。</summary>
internal static class LinuxWorkspaces
{
    public static bool IsSupported(Window window) => OperatingSystem.IsLinux()
        && window.TryGetPlatformHandle()?.HandleDescriptor == "XID";

    // 只在窗口打开和用户切换设置时调用，不在移动帧或计时器里连接 X11。
    public static void Apply(Window window, bool allWorkspaces)
    {
        if (!IsSupported(window))
            return;
        var display = XOpenDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero)
        {
            MainWindow.Log("无法连接 X11，未发送跨工作区请求。");
            return;
        }
        try
        {
            var root = XDefaultRootWindow(display);
            var desktop = new IntPtr(uint.MaxValue);
            if (!allWorkspaces)
            {
                // 关闭时放到当前工作区，而不是硬编码第一个工作区。
                var atom = XInternAtom(display, "_NET_CURRENT_DESKTOP", false);
                var status = XGetWindowProperty(display, root, atom, IntPtr.Zero, new IntPtr(1), false,
                    new IntPtr(6), out _, out var format, out var count, out _, out var data);
                try
                {
                    if (status != 0 || format != 32 || count == IntPtr.Zero || data == IntPtr.Zero)
                    {
                        MainWindow.Log("窗口管理器未提供当前工作区，未发送工作区切换请求。");
                        return;
                    }
                    desktop = Marshal.ReadIntPtr(data);
                }
                finally
                {
                    if (data != IntPtr.Zero)
                        XFree(data);
                }
            }

            var message = new XClientMessageEvent
            {
                Type = 33, // ClientMessage
                SendEvent = 1,
                Display = display,
                Window = window.TryGetPlatformHandle()!.Handle,
                MessageType = XInternAtom(display, "_NET_WM_DESKTOP", false),
                Format = 32,
                Data0 = desktop,
                Data1 = new IntPtr(1), // EWMH: 请求来自应用程序
            };
            // XEvent 在 Linux x64 上为 24 个 long；XClientMessageEvent 占其前半部分。
            if (XSendEvent(display, root, false, new IntPtr((1L << 20) | (1L << 19)), ref message) == 0)
                MainWindow.Log("X11 跨工作区请求发送失败。");
            XFlush(display);
        }
        finally
        {
            XCloseDisplay(display);
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 192)]
    private struct XClientMessageEvent
    {
        public int Type;
        public IntPtr Serial;
        public int SendEvent;
        public IntPtr Display;
        public IntPtr Window;
        public IntPtr MessageType;
        public int Format;
        public IntPtr Data0, Data1, Data2, Data3, Data4;
    }

    [DllImport("libX11.so.6")] private static extern IntPtr XOpenDisplay(IntPtr displayName);
    [DllImport("libX11.so.6")] private static extern IntPtr XDefaultRootWindow(IntPtr display);
    [DllImport("libX11.so.6")] private static extern IntPtr XInternAtom(IntPtr display, string name, [MarshalAs(UnmanagedType.Bool)] bool onlyIfExists);
    [DllImport("libX11.so.6")] private static extern int XGetWindowProperty(IntPtr display, IntPtr window, IntPtr property,
        IntPtr offset, IntPtr length, [MarshalAs(UnmanagedType.Bool)] bool delete, IntPtr requestedType,
        out IntPtr actualType, out int format, out IntPtr count, out IntPtr bytesAfter, out IntPtr data);
    [DllImport("libX11.so.6")] private static extern int XSendEvent(IntPtr display, IntPtr window,
        [MarshalAs(UnmanagedType.Bool)] bool propagate, IntPtr eventMask, ref XClientMessageEvent message);
    [DllImport("libX11.so.6")] private static extern int XFree(IntPtr data);
    [DllImport("libX11.so.6")] private static extern int XFlush(IntPtr display);
    [DllImport("libX11.so.6")] private static extern int XCloseDisplay(IntPtr display);
}
