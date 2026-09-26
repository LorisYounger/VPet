using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using LinePutScript.Localization;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;

namespace VPet_Simulator.MutiPlatform;

public partial class App : Application
{
    /// <summary>
    /// 现在开着的桌宠 (与 Windows 版 App.MainWindows 同名)
    /// </summary>
    /// 多开是进程内的: 一个进程里几个窗口, 各自一套设置和存档。开新进程的话
    /// MOD 会被加载好几遍, 内存和启动时间都是白花的。
    public static List<MainWindow> MainWindows { get; } = new List<MainWindow>();

    /// <summary>
    /// 多存档系统名称
    /// </summary>
    /// 与 Windows 版一样是启动时扫一遍 Setting*.lps 得到的名字表 (不带 "-", 默认那只是空串), 设置面板新建/删除存档时会往里加减
    public static List<string> MutiSaves { get; set; } = new List<string>();

    /// <summary>
    /// 已加载的 MOD 插件类型名 (与 Windows 版同名, 反馈中心用)
    /// </summary>
    public static HashSet<string> MODType { get; set; } = new HashSet<string>();

    /// <summary>
    /// 命令行参数
    /// </summary>
    internal static string[] Args { get; set; } = Array.Empty<string>();

    /// <summary>
    /// --ui-walk 脚本 与 输出目录; 没给这个参数时为 null
    /// </summary>
    internal static (string Script, string Output)? UiWalk { get; set; }

    /// <summary>
    /// --language 指定的语言; 只在界面走查时生效, 不写进设置
    /// </summary>
    internal static string? LanguageOverride { get; set; }

    public App() : base()
    {
        //没处理的异常先记进日志, 桌宠常驻后台, 不记的话崩了也不知道为什么
        AppDomain.CurrentDomain.UnhandledException += (s, e) => MainWindow.Log("未处理的异常: " + e.ExceptionObject);
        Avalonia.Threading.Dispatcher.UIThread.UnhandledExceptionFilter += (s, e) => MainWindow.Log("未处理的异常: " + e.Exception);
        //每次启动先记一行运行环境: 用户从 macOS/Linux 发来的日志只看内容分不出是哪个系统、哪种架构、怎么启动的
        MainWindow.Log($"==== 启动 系统={System.Runtime.InteropServices.RuntimeInformation.OSDescription} 架构={System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture} .NET={Environment.Version} 参数={string.Join(' ', Args)}");
        //跨平台: WPF 的 DispatcherUnhandledException 对应 Avalonia 的 Dispatcher.UIThread.UnhandledException; 与 Windows 版一样只在发布构建挂
#if !DEBUG
        Avalonia.Threading.Dispatcher.UIThread.UnhandledException += (s, e) => { e.Handled = true; UnhandledException(e.Exception, false); };
        AppDomain.CurrentDomain.UnhandledException += (s, e) => { UnhandledException((e.ExceptionObject as Exception)!, true); };
#endif
    }

    /// <summary>
    /// 第一只桌宠 (Windows 版是 Application.MainWindow)
    /// </summary>
    private static MainWindow? MainWindow => MainWindows.FirstOrDefault();

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            //跨平台: 前缀表由共享的 MultiPetStore 扫, 它给的是带 "-" 的文件名后缀, 这里按 Windows 版的样子去掉
            foreach (var name in MultiPetStore.List(AppPaths.DataRoot))
                MutiSaves.Add(name.Trim('-'));
            if (MutiSaves.Count == 0)
            {
                MutiSaves.Add("");
            }

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

    HashSet<string> ErrorReport = new HashSet<string>();
    private void UnhandledException(Exception e, bool isFatality)
    {
        var expt = e.ToString();
        if (ErrorReport.Contains(expt))
            return;//防止重复报错
        ErrorReport.Add(expt);
        if (expt.Contains("MainWindow.Close") || expt.Contains("System.Windows.Window.DragMove") ||
            expt.Contains("winConsole"))
            return;
        else if ((!isFatality && MainWindow != null && MainWindow.GameSavesData?.GameSave != null &&
            (MainWindow.GameSavesData.GameSave.Money > int.MaxValue || MainWindow.GameSavesData.GameSave.Exp > int.MaxValue)
            ) && ((expt.ToLowerInvariant().Contains("value") && expt.ToLowerInvariant().Contains("nan")) ||
            expt.Contains("System.OverflowException") || expt.Contains("System.DivideByZeroException")))
        {
            MessageBoxX.Show("由于修改游戏数据导致数据溢出,存档可能会出错\n开发者提醒您请不要使用过于超模的MOD".Translate());
            return;
        }
        else if (expt.Contains("System.IO.FileNotFoundException") && expt.Contains("cache"))
        {
            MessageBoxX.Show("缓存被其他软件删除,游戏无法继续运行\n请重启游戏重新生成缓存".Translate());
            return;
        }
        else if (expt.Contains("0x80070008"))
        {
            MessageBoxX.Show("游戏内存不足,请修改设置中渲染分辨率以便降低内存使用".Translate());
            return;
        }
        else if (expt.Contains("UnauthorizedAccessException"))
        {
            MessageBoxX.Show("游戏权限不足,无法写入游戏存档和设置,请检查设置文件是否被其他软件占用".Translate());
            return;
        }
        else if (expt.Contains("VPet.Plugin"))
        {
            var exptin = expt.Split('\n').First(x => x.Contains("VPet.Plugin"));
            exptin = exptin.Substring(exptin.IndexOf("VPet.Plugin") + 12).Split('.')[0];
            MessageBoxX.Show("游戏发生错误,可能是".Translate() + $"MOD({exptin.Translate()})" +
                "导致的\n如有可能请发送 错误信息截图和引发错误之前的操作给相应MOD作者\n感谢您对MOD开发的支持\n".Translate()
                 + expt, "游戏发生错误,可能是".Translate() + exptin);
            return;
        }

        foreach (var modname in MODType)
        {
            if (expt.Contains(modname))
            {
                var exptin = modname.Split('.').Last();
                MessageBoxX.Show("游戏发生错误,可能是".Translate() + $"MOD({modname})" +
                    "导致的\n如有可能请发送 错误信息截图和引发错误之前的操作给相应MOD作者\n感谢您对MOD开发的支持\n".Translate()
                     + expt, "游戏发生错误,可能是".Translate() + exptin);
                return;
            }
        }


        string errstr = "游戏发生错误,可能是".Translate() + (string.IsNullOrWhiteSpace(CoreMOD.NowLoading) ?
            "游戏或者MOD".Translate() : $"MOD({CoreMOD.NowLoading})") +
            "导致的\n如有可能请发送 错误信息截图和引发错误之前的操作 给开发者:service@exlb.net\n感谢您对游戏开发的支持\n".Translate()
            + expt;
        if (isFatality || MainWindow == null)
        {
            MessageBoxX.Show(errstr, "游戏致命性错误".Translate());
            return;
        }
        else
        {
            new winReport(MainWindow, errstr).Show();
            return;
        }
    }

    private static MainWindow CreatePet(string prefix)
    {
        //退出由 MainWindow.Exit 负责: 最后一只关掉时它会 desktop.Shutdown(), 多开时只把自己摘掉 (与 Windows 版同一套流程)
        return new MainWindow(prefix);
    }
}
