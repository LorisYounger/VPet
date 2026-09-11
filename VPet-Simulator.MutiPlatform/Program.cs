using Avalonia;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace VPet_Simulator.MutiPlatform;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        //存档里的数字是按当前区域设置格式化的: 德语环境下 12.5 会写成 12,5,
        //存档哈希当场对不上, 存档也没法在两个平台之间拷来拷去.
        //Windows 版没这个问题只是因为它的用户几乎都在中文/英文环境下.
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        //多开要用它挑开哪一只
        App.Args = args;

        var probe = ReadProbeArgument(args);
        if (probe != null)
        {
            RunProbe(probe.Value.Name, probe.Value.Output);
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// 解析 --probe 参数
    /// </summary>
    /// <returns>没有这个参数时返回 null</returns>
    private static (string Name, string Output)? ReadProbeArgument(string[] args)
    {
        int index = Array.FindIndex(args, x => x == "--probe");
        if (index < 0)
            return null;
        var name = index + 1 < args.Length ? args[index + 1] : "styles";
        var output = index + 2 < args.Length ? args[index + 2] : name + ".png";
        return (name, output);
    }

    /// <summary>
    /// 渲染一张界面截图然后退出
    /// </summary>
    /// 不开窗口, 直接把控件画进位图. 迁移期间没有 Linux/macOS 机器可以实机验证
    /// (所有者已确认), 拿到机器之后跑一条命令就能把两个平台的渲染结果摆在一起比,
    /// 而不是靠人肉点一遍。改完样式想看一眼有没有画崩也用它。
    ///
    /// 用的是真的平台后端而不是 Avalonia.Headless: 后者要多引一个包, 而桌宠本来
    /// 就是桌面程序, 跑探针的机器一定有显示环境。
    private static void RunProbe(string name, string output)
    {
        try
        {
            BuildAvaloniaApp().SetupWithoutStarting();
            if (!RenderProbe.Render(name, output))
                Environment.ExitCode = 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"渲染失败: {ex.Message}");
            Console.WriteLine($"可用的界面: {string.Join(", ", RenderProbe.Names)}");
            Environment.ExitCode = 1;
        }
    }

    /// <summary>
    /// Avalonia 启动配置
    /// </summary>
    /// Linux 上刻意优先走 X11: 只要有 DISPLAY 可用(原生 X11 或 XWayland 都算),
    /// 桌宠就能自设窗口坐标, 爬行/贴边/回正这些核心行为才成立.
    /// 原生 Wayland 协议禁止应用设置全局坐标, 那种情况下桌宠会退化成固定位置,
    /// 见 AvaloniaController 的说明.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
