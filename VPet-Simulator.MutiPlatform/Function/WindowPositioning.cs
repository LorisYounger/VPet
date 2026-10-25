using Avalonia;
using Avalonia.Controls;
using System;

namespace VPet_Simulator.MutiPlatform;

/// <summary>窗口坐标必须成对提交；X11 的 Position 在 ConfigureNotify 到达前仍然是旧值。</summary>
internal static class WindowPositioning
{
    // 同时存在 DISPLAY 和 WAYLAND_DISPLAY 时也必须按实际窗口后端判断。
    public static bool CanPosition(Window window) => !OperatingSystem.IsLinux()
        || window.TryGetPlatformHandle()?.HandleDescriptor == "XID";

    public static void SetPosition(Window window, double left, double top, double scaling)
    {
        if (CanPosition(window))
            window.Position = new PixelPoint((int)Math.Round(left * scaling), (int)Math.Round(top * scaling));
    }

    public static void MoveBy(Window window, double x, double y, double scaling)
    {
        var position = window.Position;
        window.Position = new PixelPoint(
            (int)Math.Round(position.X + x * scaling),
            (int)Math.Round(position.Y + y * scaling));
    }
}
