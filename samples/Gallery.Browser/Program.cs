using Avalonia;
using Avalonia.Browser;
using Mav;
using Mav.Theming;

namespace Mav.Gallery.Browser;

internal static class Program
{
    private static Task Main(string[] args) =>
        BuildAvaloniaApp().StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Mav.Gallery.GalleryApp>()
            .UseBrowser()
            .UseMav(options =>
            {
                options.SeedColor = MavColors.Indigo;
                options.Mode = MavThemeMode.System;
            })
            .LogToTrace();
}
