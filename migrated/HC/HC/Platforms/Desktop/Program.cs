using Avalonia;
using Mav.Essentials;
using Mav.Essentials.Linux;

namespace Hc.Platforms.Desktop;

/// <summary>Desktop entry point. A phone-sized window is used for fast visual checks.</summary>
internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Secrets (saved password) go to the desktop keyring through secret-tool on Linux.
        if (OperatingSystem.IsLinux())
            MavSecureStorage.SetDefault(new LibsecretSecureStorage());

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<DesktopApp>()
            .UsePlatformDetect()
            .UseHcDefaults();
}
