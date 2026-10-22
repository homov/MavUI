using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Md3.Avalonia;
using Md3.Avalonia.Theming;

namespace Md3.Demo.Android;

[Application]
public sealed class MainApplication : AvaloniaAndroidApplication<MobileApp>
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        => base.CustomizeAppBuilder(builder)
            .UseMd3(options =>
            {
                options.SeedColor = MdColors.Blue;
                options.Mode = MdThemeMode.System;
                options.SchemeVariant = MdSchemeVariant.TonalSpot;
            })
            .LogToTrace();
}

[Activity(
    Label = "Md3 Demo",
    Theme = "@style/Md3Demo.NoActionBar",
    MainLauncher = true,
    Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation |
        ConfigChanges.ScreenSize |
        ConfigChanges.UiMode |
        ConfigChanges.Density |
        // Keep the activity alive when the locale or layout direction changes;
        // Md3 text and templates are rebuilt from the current culture on redraw.
        ConfigChanges.Locale |
        ConfigChanges.LayoutDirection)]
public sealed class MainActivity : AvaloniaMainActivity
{
}
