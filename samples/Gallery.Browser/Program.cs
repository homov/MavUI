using Avalonia;
using Avalonia.Browser;
using Md3.Avalonia;
using Md3.Avalonia.Theming;

namespace Md3.Gallery.Browser;

internal static class Program
{
    private static Task Main(string[] args) =>
        BuildAvaloniaApp().StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<Md3.Gallery.GalleryApp>()
            .UseBrowser()
            .UseMd3(options =>
            {
                options.SeedColor = MdColors.Indigo;
                options.Mode = MdThemeMode.System;
            })
            .LogToTrace();
}
