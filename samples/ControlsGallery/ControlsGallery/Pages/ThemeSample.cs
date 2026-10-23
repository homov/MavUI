using Avalonia;
using Avalonia.Media;
using Mav;
using Mav.Theming;

namespace Mav.ControlsGallery.Pages;

/// <summary>
/// Plain data for the theming demos. One record serves every template: a colour swatch, a type sample,
/// a shape sample or an elevation sample. Only the properties a template reads are set.
/// </summary>
public sealed record ThemeSample(string Name)
{
    public IBrush? Background { get; init; }

    public IBrush? Foreground { get; init; }

    public Color Fill { get; init; }

    public MavTypeRole Role { get; init; }

    public CornerRadius Corner { get; init; }

    public double Elevation { get; init; }
}
