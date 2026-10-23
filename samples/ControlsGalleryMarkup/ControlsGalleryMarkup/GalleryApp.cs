using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Mav;
using Mav.ControlsGalleryMarkup.Localization;

namespace Mav.ControlsGalleryMarkup;

/// <summary>Shared application for the markup gallery. The desktop head configures the builder and theme.</summary>
public class GalleryApp : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        // Strings come from GalleryStrings; English is the fallback passed to every MavLocalization.Get call.
        MavLocalization.Current = GalleryStrings.Instance;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();

        base.OnFrameworkInitializationCompleted();
    }
}
