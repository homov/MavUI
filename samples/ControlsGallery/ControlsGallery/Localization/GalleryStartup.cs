using System.Globalization;
using Mav;
using Mav.Theming;

namespace Mav.ControlsGallery.Localization;

/// <summary>
/// What both galleries do before their first page is built: the language follows the one the
/// machine is set to, and hosts that animate on a slow path start with the smallest motion profile.
/// </summary>
public static class GalleryStartup
{
    /// <summary>True on desktop; false in a browser tab and on phones and tablets.</summary>
    public static bool IsDesktop =>
        !(OperatingSystem.IsBrowser() || OperatingSystem.IsAndroid() || OperatingSystem.IsIOS());

    /// <summary>
    /// The motion profile to start with. A browser tab and a phone draw on a software or
    /// battery-powered path, so they start reduced; the desktop keeps the expressive profile.
    /// </summary>
    public static MavMotionScheme DefaultMotion() => MotionFor(IsDesktop);

    /// <summary>The profile of a host: the desktop keeps the expressive one, everything else starts reduced.</summary>
    public static MavMotionScheme MotionFor(bool isDesktop) =>
        isDesktop ? MavMotionScheme.Expressive : MavMotionScheme.Reduced;

    /// <summary>Wires the shared provider, picks the language and sets the motion profile.</summary>
    public static void Apply()
    {
        // Remember the culture the machine is set to: the "system" choice in the picker returns to it
        // even after the user tried other languages.
        GalleryLanguage.SystemCulture = CultureInfo.CurrentUICulture;

        MavLocalization.Current = GalleryStrings.Instance;
        // Keep the region for formatting too (fr-CA is not fr-FR). Unsupported languages retain
        // their regional formatting, while the UI uses its English fallback.
        GalleryLanguage.Select(null);
        MavTheme.Configure(motion: DefaultMotion());
    }
}
