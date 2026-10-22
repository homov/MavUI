using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Avalonia.Controls;
using Mav;
using Mav.Theming;

namespace Mav.ControlsGallery.Android;

[Application]
public sealed class MainApplication : AvaloniaAndroidApplication<AndroidApp>
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder)
            .UseMav(options =>
            {
                options.SeedColor = MavColors.Indigo;
                options.Mode = MavThemeMode.System;
            })
            .LogToTrace();
}

[Activity(
    Label = "Mav Controls Gallery",
    Theme = "@style/MavControls.NoActionBar",
    MainLauncher = true,
    Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation |
        ConfigChanges.ScreenSize |
        ConfigChanges.UiMode |
        ConfigChanges.Density |
        ConfigChanges.Locale |
        ConfigChanges.LayoutDirection)]
public sealed class MainActivity : AvaloniaMainActivity
{
}

/// <summary>Android head: adds the system bars around the shared root.</summary>
public sealed class AndroidApp : ControlsGallery.App
{
    protected override Control CreateMobileRoot()
    {
        var root = base.CreateMobileRoot();

        // Edge-to-edge is an Android 15+ feature; older releases keep the theme surface color
        // in the bars instead of exposing the wallpaper.
        var edgeToEdge = (int)global::Android.OS.Build.VERSION.SdkInt >= 35;
        MavSystemBars.Attach(root, edgeToEdge: edgeToEdge);
        return root;
    }
}
