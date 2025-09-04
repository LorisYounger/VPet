using System.Diagnostics;
using System.Windows;

namespace VPet.Solution;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args != null && e.Args.Count() > 0)
        {
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
                        Arguments = "/c start steam://rungameid/1920960"
                    };
                    Process.Start(psi);
                    break;
            }
            Application.Current.Shutdown();
        }
    }
}
