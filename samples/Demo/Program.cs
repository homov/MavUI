using Avalonia;
using Md3.Avalonia;
using Md3.Avalonia.Theming;

namespace Md3.Demo;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseMd3(options =>
            {
                options.SeedColor = MdColors.Blue;
                options.Mode = MdThemeMode.System;
                options.SchemeVariant = MdSchemeVariant.TonalSpot;
            })
            .LogToTrace();
}
