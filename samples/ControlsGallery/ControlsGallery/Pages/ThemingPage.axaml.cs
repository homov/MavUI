using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.ControlsGallery.Pages;

public partial class ThemingPage : UserControl
{
    // Commands bound from AXAML. Each takes its value from CommandParameter.
    public static readonly GalleryCommand ModeLight =
        new(_ => MavTheme.UseLight());

    public static readonly GalleryCommand ModeDark =
        new(_ => MavTheme.UseDark());

    public static readonly GalleryCommand ModeSystem =
        new(_ => MavTheme.UseSystemMode());

    public static readonly GalleryCommand SetSeed =
        new(p => MavTheme.Configure(seed: (Color)p!));

    public static readonly GalleryCommand SetContrast =
        new(p => MavTheme.Configure(contrast: (MavContrast)p!));

    public static readonly GalleryCommand SetDensity =
        new(p => MavTheme.Configure(density: (MavDensity)p!));

    public static readonly GalleryCommand SetMotion =
        new(p => MavTheme.Configure(motion: (MavMotionScheme)p!));

    public static readonly GalleryCommand SetShape =
        new(p => MavTheme.Configure(shapeScale: (MavShapeScale)p!));

    public static readonly GalleryCommand SetEnglish =
        new(_ => MavLocalization.SetCulture(new CultureInfo("en-US")));

    public static readonly GalleryCommand SetRussian =
        new(_ => MavLocalization.SetCulture(new CultureInfo("ru-RU")));

    /// <summary>Colour swatches for every colour role; the text colour is chosen for contrast.</summary>
    public static IReadOnlyList<ThemeSample> ColorSwatches =>
        Enum.GetValues<MavColorRole>().Select(role =>
        {
            var color = MavTheme.ColorOf(role);
            var on = MavAccessibility.RelativeLuminance(color) >= 0.5 ? Colors.Black : Colors.White;
            return new ThemeSample(SplitWords(role.ToString()))
            {
                Background = MavTheme.BrushOf(color),
                Foreground = MavTheme.BrushOf(on),
            };
        }).ToList();

    /// <summary>One sample per type role.</summary>
    public static IReadOnlyList<ThemeSample> TypeScale =>
        Enum.GetValues<MavTypeRole>().Select(role => new ThemeSample(role.ToString()) { Role = role }).ToList();

    /// <summary>Corner radius samples from the current shape scale.</summary>
    public static IReadOnlyList<ThemeSample> ShapeSamples
    {
        get
        {
            var shape = MavTheme.Current.Shape;
            return new[]
            {
                ("None", shape.None), ("ExtraSmall", shape.ExtraSmall), ("Small", shape.Small),
                ("Medium", shape.Medium), ("Large", shape.Large), ("LargeIncreased", shape.LargeIncreased),
                ("ExtraLarge", shape.ExtraLarge), ("Full", shape.Full),
            }.Select(s => new ThemeSample(s.Item1) { Corner = new CornerRadius(s.Item2) }).ToList();
        }
    }

    /// <summary>Elevation levels 0 to 5, with localized labels.</summary>
    public static IReadOnlyList<ThemeSample> ElevationSamples =>
        Enumerable.Range(0, 6).Select(level => new ThemeSample(
            MavLocalization.Format("Overview.Elevation.Level", "Level {0}", level))
        {
            Elevation = level,
        }).ToList();

    /// <summary>Contrast ratios of the main role pairs, with WCAG verdicts.</summary>
    public static IReadOnlyList<string> ContrastLines
    {
        get
        {
            var pairs = new (string Name, MavColorRole Foreground, MavColorRole Background)[]
            {
                ("OnPrimary / Primary", MavColorRole.OnPrimary, MavColorRole.Primary),
                ("OnSurface / Surface", MavColorRole.OnSurface, MavColorRole.Surface),
                ("OnSurfaceVariant / Surface", MavColorRole.OnSurfaceVariant, MavColorRole.Surface),
                ("OnError / Error", MavColorRole.OnError, MavColorRole.Error),
                ("OnPrimaryContainer / PrimaryContainer", MavColorRole.OnPrimaryContainer, MavColorRole.PrimaryContainer),
            };
            return pairs.Select(p =>
            {
                var ratio = MavAccessibility.ContrastRatio(MavTheme.ColorOf(p.Foreground), MavTheme.ColorOf(p.Background));
                var verdict = ratio >= 4.5 ? "✓ AA" : ratio >= 3 ? "✓ AA large" : "—";
                return $"{p.Name}: {ratio.ToString("F2", CultureInfo.InvariantCulture)}:1 {verdict}";
            }).ToList();
        }
    }

    /// <summary>"SurfaceContainerHigh" becomes "Surface Container High", so long role names wrap inside their swatch.</summary>
    private static string SplitWords(string name) =>
        System.Text.RegularExpressions.Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");

    public ThemingPage()
    {
        InitializeComponent();
    }
}
