using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Mav.ControlsGallery.Localization;
using Mav.ControlsGallery.Shell;

namespace Mav.ControlsGallery;

/// <summary>
/// Shared application for every head (desktop, Android, browser). The platform heads only
/// configure the builder and, on mobile, may customize the root (see <see cref="CreateMobileRoot"/>).
/// </summary>
public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        // Strings come from GalleryStrings; English is the AXAML fallback. The language is the one
        // the machine is set to, and browsers and phones start with the reduced motion profile.
        GalleryStartup.Apply();

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow();
                break;
            case IActivityApplicationLifetime activity:
                activity.MainViewFactory = CreateMobileRoot;
                break;
            case ISingleViewApplicationLifetime single:
                single.MainView = new MainView();
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Root view for single-view hosts (Android, browser). Platform heads may wrap it.</summary>
    protected virtual Control CreateMobileRoot() => new MainView();
}
