using System;
using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using VPet.Solution.MultiPlatform;

namespace VPet.Solution.MultiPlatform.Android;

[Application]
public class AndroidApp : AvaloniaAndroidApplication<App>
{
    protected AndroidApp(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer) { }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder).WithInterFont();
    }
}
