using Avalonia;
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

    /// <summary>
    /// Opens the demo window. It is built per click, and only on desktop: in a browser build
    /// there is no windowing platform, so constructing a <see cref="Window"/> throws
    /// "Browser doesn't support windowing platform" even if it is never shown. Declaring the
    /// window in XAML resources built it on page load, which broke the Layout page on the web.
    /// </summary>
    public static readonly GalleryCommand ShowWindow = new(_ =>
    {
        if (!IsDesktop)
            return;

        new MavWindow
        {
            Title = "MavWindow demo",
            Width = 520,
            Height = 360,
            Content = new MavCard
            {
                Variant = MavCardVariant.Filled,
                Child = new MavLabel
                {
                    Margin = new Thickness(24),
                    TextWrapping = TextWrapping.Wrap,
                    Text = MavLocalization.Get(
                        "Layout.Window.Body",
                        "This is a second MavWindow with its own overlay layer."),
                },
            },
        }.Show();
    });

    public LayoutPage()
    {
        InitializeComponent();
    }
}
