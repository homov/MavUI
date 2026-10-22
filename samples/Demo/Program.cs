using Avalonia;
using Mav;
using Mav.Theming;

namespace Mav.Demo;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseMav(options =>
            {
                options.SeedColor = MavColors.Blue;
                options.Mode = MavThemeMode.System;
                options.SchemeVariant = MavSchemeVariant.TonalSpot;
            })
            .LogToTrace();
}
