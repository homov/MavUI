using Avalonia;
using Md3.Avalonia;
using Md3.Avalonia.Theming;

namespace Md3.Gallery;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<DesktopGalleryApp>()
            .UsePlatformDetect()
            .UseMd3(options =>
            {
                options.SeedColor = MdColors.Indigo;
                options.Mode = MdThemeMode.System;
            })
            .LogToTrace();
}
