using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Hc;

[assembly: AvaloniaTestApplication(typeof(Hc.Tests.Ui.HcTestAppBuilder))]

namespace Hc.Tests.Ui;

/// <summary>The real HC application on the headless platform: no display is needed.</summary>
public static class HcTestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseHcDefaults()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

/// <summary>
/// Keeps every test away from the production cloud. The address points at a closed local port, so a test
/// that forgets to use a fake server fails fast instead of calling the real API. Set HC_API_BASE to override.
/// </summary>
internal static class TestEnvironment
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HC_API_BASE")))
            Environment.SetEnvironmentVariable("HC_API_BASE", "http://127.0.0.1:9/stinexapiv1");
    }
}
