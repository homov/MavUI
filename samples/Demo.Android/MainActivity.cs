using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Mav;
using Mav.Theming;

namespace Mav.Demo.Android;

[Application]
public sealed class MainApplication : AvaloniaAndroidApplication<MobileApp>
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder)
            .UseMav(options =>
            {
                options.SeedColor = MavColors.Blue;
                options.Mode = MavThemeMode.System;
                options.SchemeVariant = MavSchemeVariant.TonalSpot;
            })
            .LogToTrace();
}

[Activity(
    Label = "Mav Demo",
    Theme = "@style/MavDemo.NoActionBar",
    MainLauncher = true,
    Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation |
        ConfigChanges.ScreenSize |
        ConfigChanges.UiMode |
        ConfigChanges.Density |
        // Keep the activity alive when the locale or layout direction changes;
        // Mav text and templates are rebuilt from the current culture on redraw.
        ConfigChanges.Locale |
        ConfigChanges.LayoutDirection)]
public sealed class MainActivity : AvaloniaMainActivity
{
}
