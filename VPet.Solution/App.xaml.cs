using System.Diagnostics;
using System.Windows;

namespace VPet.Solution;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : Application
{
    public static bool IsDone { get; set; } = false;

    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args != null && e.Args.Count() > 0)
        {
            IsDone = true;
            switch (e.Args[0].ToLower())
            {
                case "removestarup":
                    var path =
                        Environment.GetFolderPath(Environment.SpecialFolder.Startup)
                        + @"\VPET_Simulator.lnk";
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    return;
                case "launchsteam":
                    Process.Start("steam://rungameid/1920960");
                    return;
            }
        }
        IsDone = false;
    }
}
