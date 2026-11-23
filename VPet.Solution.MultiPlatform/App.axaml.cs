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
        Services = new AppServices().BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 如果有参数, 则按要求走
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args;
            if (args is not null && args.Length > 0)
            {
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
                        NativeUtils.Launch("steam://rungameid/1920960");
                        break;
                }

                desktop.Shutdown();
                base.OnFrameworkInitializationCompleted();
                return;
            }
            if (VPetSolution.BaseFolder is null)
                VPetSolution.SetLocalBaseDirectory(Environment.CurrentDirectory);
        }

        // 用 DialogService 显示, 此方式支持桌面和单页
        Services
            .GetRequiredService<IDialogService>()
            .Show(null, Services.GetRequiredService<MainViewModel>());

        //if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        //{
        //    desktop.MainWindow = new MainWindow
        //    {
        //        DataContext = Services.GetRequiredService<MainViewModel>(),
        //    };
        //}
        //else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        //{
        //    singleViewPlatform.MainView = new MainView
        //    {
        //        DataContext = Services.GetRequiredService<MainViewModel>(),
        //    };
        //}

        base.OnFrameworkInitializationCompleted();
    }
}
