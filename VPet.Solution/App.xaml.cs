using System.Diagnostics;
using System.IO;
using System.Windows;
using HanumanInstitute.MvvmDialogs;
using Microsoft.Extensions.DependencyInjection;
using VPet.Solution.ViewModels;

namespace VPet.Solution;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args == null || e.Args.Length <= 0)
        {
            base.OnStartup(e);
            Services = IOCInitializer.ConfigureServices();
            Services
                .GetService<IDialogService>()!
                .Show(null, Services.GetService<MainViewModel>()!);
            return;
        }

        switch (e.Args[0].ToLowerInvariant())
        {
            case "removestarup":
                var path =
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup)
                    + @"\VPET_Simulator.lnk";
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                break;
            case "launchsteam":
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    Arguments = "/c start steam://rungameid/1920960",
                };
                Process.Start(psi);
                break;
        }
        Application.Current.Shutdown();
    }
}
