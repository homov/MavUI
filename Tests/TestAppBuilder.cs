using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;

namespace Mav.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

public sealed class TestApp : Application
{
}
