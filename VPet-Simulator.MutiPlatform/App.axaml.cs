using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core.MutiPlatform;

namespace VPet_Simulator.MutiPlatform;

public partial class App : Application
{
    /// <summary>
    /// 现在开着的桌宠
    /// </summary>
    /// 多开是进程内的: 一个进程里几个窗口, 各自一套设置和存档。开新进程的话
    /// MOD 会被加载好几遍, 内存和启动时间都是白花的。
    internal static List<PetWindow> OpenPets { get; } = new List<PetWindow>();

    /// <summary>
    /// 命令行参数
    /// </summary>
    internal static string[] Args { get; set; } = Array.Empty<string>();

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 开哪一只: 命令行说了算, 没说就看 startup_ 标记, 都没有就开默认那只
            var prefix = MultiPetStore.ReadPrefixArgument(Args)
                ?? MultiPetStore.ReadStartupMarker(AppPaths.DataRoot)
                ?? string.Empty;
            desktop.MainWindow = CreatePet(prefix);

            // 关掉最后一只才退出: 多开时关掉其中一只不该把别的也带走
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
        }
        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 再开一只桌宠
    /// </summary>
    internal static void OpenPet(string prefix)
    {
        var window = CreatePet(prefix);
        window.Show();
    }

    private static PetWindow CreatePet(string prefix)
    {
        var window = new PetWindow(prefix);
        OpenPets.Add(window);
        window.Closed += (_, _) =>
        {
            OpenPets.Remove(window);
            if (OpenPets.Count == 0
                && Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        };
        return window;
    }
}
