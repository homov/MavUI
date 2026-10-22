using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Md3.Avalonia;
using Md3.Avalonia.Theming;

namespace Md3.Gallery.Android;

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
                options.SeedColor = MdColors.Indigo;
                options.Mode = MdThemeMode.System;
            })
            .LogToTrace();
}

[Activity(
    Label = "Md3 Gallery",
    Theme = "@style/Md3Gallery.NoActionBar",
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
