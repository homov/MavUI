using Avalonia;
using Mav;
using Mav.Theming;

namespace Mav.ControlsGallery.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<ControlsGallery.App>()
            .UsePlatformDetect()
            .UseMav(options =>
            {
                options.SeedColor = MavColors.Indigo;
                options.Mode = MavThemeMode.System;
            })
            .LogToTrace();
}
