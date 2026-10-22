using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Md3.Demo;

public sealed class App : Application
{
    public override void Initialize()
    {
        // Md3 controls provide their own templates; no Avalonia theme is required.
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();

        base.OnFrameworkInitializationCompleted();
    }
}
