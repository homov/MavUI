using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Mav;
using Mav.ControlsGallery.Shared;

namespace Mav.ControlsGalleryMarkup;

/// <summary>Shared application for the markup gallery. The desktop head configures the builder and theme.</summary>
public class GalleryApp : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        // Strings come from the shared GalleryStrings; English is the fallback passed to every
        // MavLocalization.Get call. The language follows the machine, and non-desktop hosts
        // start with the reduced motion profile.
        GalleryStartup.Apply();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();

        base.OnFrameworkInitializationCompleted();
    }
}
