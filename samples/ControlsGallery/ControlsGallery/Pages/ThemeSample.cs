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
    /// <summary>Readable form of <see cref="Name"/>: "SurfaceContainerHigh" -> "Surface Container High".</summary>
    public string Label => RoleName.SplitWords(Name);

    /// <summary>Second, smaller line of text, e.g. the radius in px for a shape sample.</summary>
    public string? Detail { get; init; }

    public IBrush? Background { get; init; }

    public IBrush? Foreground { get; init; }

    public MavTypeRole Role { get; init; }

    public CornerRadius Corner { get; init; }

    public double Elevation { get; init; }
}
