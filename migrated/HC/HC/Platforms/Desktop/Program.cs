using Avalonia;
using Mav.Essentials;

namespace Hc.Platforms.Desktop;

/// <summary>Desktop entry point. A phone-sized window is used for fast visual checks.</summary>
internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Secrets (saved password): DPAPI on Windows, the desktop keyring (secret-tool) on Linux.
        MavEssentials.Initialize();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<DesktopApp>()
            .UsePlatformDetect()
            .UseHcDefaults();
}
