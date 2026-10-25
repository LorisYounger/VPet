using Avalonia;
using System;
using LinePutScript;
using LinePutScript.Dictionary;

namespace VPet_Simulator.MutiPlatform;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        //多开要用它挑开哪一只
        App.Args = args;

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Avalonia.Wayland.AvaloniaWaylandException ex)
        {
            Console.Error.WriteLine($"原生 Wayland 启动失败：{ex.Message}\n移除 wayland / wayland:| 启动参数可恢复 X11/XWayland 模式。");
            Environment.ExitCode = 1;
        }
    }

    /// <summary>
    /// Avalonia 启动配置
    /// </summary>
    /// 默认走 X11/XWayland，保留自动走动和置顶。wayland / wayland:| 显式启用实验后端。
    /// UseWayland 没有自动回退；没有 Wayland 服务时启动失败，移除参数即可恢复默认模式。
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
        var arguments = new LPS_D();
        foreach (var argument in App.Args)
            arguments.Add(new Line(argument));
        if (OperatingSystem.IsLinux() && arguments.ContainsLine("wayland"))
            builder.UseWayland();
        return builder;
    }
}
