using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HanumanInstitute.MvvmDialogs;
using Microsoft.Extensions.DependencyInjection;
using VPet.Solution.MultiPlatform.ViewModels;
using VPet.Solution.MultiPlatform.Views;

namespace VPet.Solution.MultiPlatform;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args;
            if (args is null || args.Length <= 0)
            {
                Services = new AppServices().Build().BuildServiceProvider();
                var mainViewModel = Services.GetService<MainViewModel>()!;
                desktop.MainWindow = new MainWindow { DataContext = mainViewModel };
                // TODO: DialogService 显示主窗口
                // Services.GetService<IDialogService>()!.Show(null, mainViewModel);
                base.OnFrameworkInitializationCompleted();
                return;
            }

            switch (args[0].ToLowerInvariant())
            {
                case "removestarup":
                    if (OperatingSystem.IsWindows())
                    {
                        var path = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                            "VPET_Simulator.lnk"
                        );
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                    }
                    break;
                case "launchsteam":
                    // TODO: 跨平台启动 Steam 链接
                    // NativeUtils.OpenLink("steam://rungameid/1920960");
                    break;
            }

            desktop.Shutdown();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
