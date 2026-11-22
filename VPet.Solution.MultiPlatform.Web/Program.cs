using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Media;
using VPet.Solution.MultiPlatform;

[assembly: SupportedOSPlatform("browser")]

internal sealed partial class Program
{
    private static Task Main(string[] args) =>
        BuildAvaloniaApp()
            .WithInterFont()
            .With(
                new FontManagerOptions
                {
                    FontFallbacks =
                    [
                        new FontFallback
                        {
                            // Web没有中文字库, 不装会乱码
                            FontFamily = new FontFamily(
                                "avares://VPet.Solution.MultiPlatform/Assets/Fonts#Source Han Sans SC"
                            ),
                        },
                    ],
                }
            )
            .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>();
}
