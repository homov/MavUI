using Avalonia.Controls;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Theming;

namespace Mav.ControlsGallery.Pages;

public partial class LayoutPage : UserControl
{
    /// <summary>Surface colour for the demo tiles, resolved from the current theme.</summary>
    public static Color SurfaceContainerHigh => MavTheme.ColorOf(MavColorRole.SurfaceContainerHigh);

    /// <summary>Low-emphasis brush for the inset demos, resolved from the current theme.</summary>
    public static IBrush SurfaceContainerLowBrush =>
        MavTheme.BrushOf(MavTheme.ColorOf(MavColorRole.SurfaceContainerLow));

    public static bool IsDarkMode => MavTheme.Current.IsDark;

    public static bool IsDesktop => !(OperatingSystem.IsBrowser() || OperatingSystem.IsAndroid());

    public static bool IsUnsupportedPlatform => !IsDesktop;

    /// <summary>Opens the window declared in the page resources; the parameter is that window.</summary>
    public static readonly GalleryCommand ShowWindow = new(p => ((MavWindow)p!).Show());

    public LayoutPage()
    {
        InitializeComponent();
    }
}
