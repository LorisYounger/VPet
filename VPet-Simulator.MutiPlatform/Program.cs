using Avalonia;
using System;
using System.Linq;

namespace VPet_Simulator.MutiPlatform;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        //多开要用它挑开哪一只
        App.Args = args;
        App.UiWalk = ReadUiWalkArgument(args);
        App.LanguageOverride = ReadLanguageArgument(args);

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// 解析 --language xx: 只在界面走查时生效, 不写进设置
    /// </summary>
    private static string? ReadLanguageArgument(string[] args)
    {
        int index = Array.FindIndex(args, x => x == "--language");
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    /// <summary>
    /// 解析 --ui-walk 脚本 输出目录
    /// </summary>
    private static (string Script, string Output)? ReadUiWalkArgument(string[] args)
    {
        int index = Array.FindIndex(args, x => x == "--ui-walk");
        if (index < 0 || index + 2 >= args.Length)
            return null;
        return (args[index + 1], args[index + 2]);
    }

    /// <summary>
    /// Avalonia 启动配置
    /// </summary>
    /// Linux 上刻意优先走 X11: 只要有 DISPLAY 可用(原生 X11 或 XWayland 都算),
    /// 桌宠就能自设窗口坐标, 爬行/贴边/回正这些核心行为才成立.
    /// 原生 Wayland 协议禁止应用设置全局坐标, 那种情况下桌宠会退化成固定位置,
    /// 见 MWController 的说明.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
