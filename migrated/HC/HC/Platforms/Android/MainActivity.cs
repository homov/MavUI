using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Avalonia.Controls;
using Mav.Platform;

namespace Hc.Platforms.Android;

[Application]
public sealed class MainApplication : AvaloniaAndroidApplication<AndroidApp>
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder).UseHcDefaults();
}

[Activity(
    Label = "HC",
    Theme = "@style/HC.NoActionBar",
    MainLauncher = true,
    Exported = true,
    // Keep the activity alive on rotation and locale changes; open dialogs reflow instead of being lost.
    ConfigurationChanges = ConfigChanges.Orientation |
        ConfigChanges.ScreenSize |
        ConfigChanges.UiMode |
        ConfigChanges.Density |
        ConfigChanges.Locale |
        ConfigChanges.LayoutDirection)]
public sealed class MainActivity : AvaloniaMainActivity
{
}

/// <summary>Android host: wraps the shared root with the system bars.</summary>
public sealed class AndroidApp : App
{
    protected override Control CreateRoot()
    {
        var root = base.CreateRoot();

        // Edge-to-edge is supported from Android 15 (API 35). Older releases keep the surface color.
        var edgeToEdge = (int)global::Android.OS.Build.VERSION.SdkInt >= 35;
        MavSystemBars.Attach(root, edgeToEdge: edgeToEdge);
        return root;
    }
}
