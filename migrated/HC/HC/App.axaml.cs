using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Hc.Localization;
using Hc.Pages.Shell;
using Mav;

namespace Hc;

/// <summary>
/// Shared application for every target. Platform hosts subclass it and override the root factories.
/// </summary>
public partial class App : global::Avalonia.Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        MavLocalization.Current = HcLocalization.Instance;

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = CreateDesktopWindow();
                break;
            case IActivityApplicationLifetime activity:
                activity.MainViewFactory = CreateRoot;
                break;
            case ISingleViewApplicationLifetime single:
                single.MainView = CreateRoot();
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Root for single-view hosts (Android). Platform hosts may wrap it.</summary>
    protected virtual Control CreateRoot() => new MainView();

    /// <summary>Window for desktop hosts. Windows uses the phone-sized preview window.</summary>
    protected virtual Window CreateDesktopWindow() => new Window { Content = CreateRoot() };
}
