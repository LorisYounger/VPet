using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace VPet.Solution.MultiPlatform.Android;

[Activity(
    Label = "VPet.Solution.MultiPlatform.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation
        | ConfigChanges.ScreenSize
        | ConfigChanges.UiMode
)]
public class MainActivity : AvaloniaMainActivity { }
