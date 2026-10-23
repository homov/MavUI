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
            return new ThemeSample(role.ToString())
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
            }.Select(s => new ThemeSample(s.Item1)
            {
                Corner = new CornerRadius(s.Item2),
                Detail = $"{s.Item2:0.#} px",
            }).ToList();
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

    public ThemingPage()
    {
        InitializeComponent();
        // Swatches, radii and contrast ratios are computed from the current theme, so they are rebuilt on every change.
        AttachedToVisualTree += (_, _) =>
        {
            MavTheme.Changed -= OnThemeChanged;
            MavTheme.Changed += OnThemeChanged;
            RefreshThemed();
        };
        DetachedFromVisualTree += (_, _) => MavTheme.Changed -= OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, MavTheme theme) => RefreshThemed();

    private void RefreshThemed()
    {
        ColorItems.ItemsSource = ColorSwatches;
        ShapeItems.ItemsSource = ShapeSamples;
        ContrastItems.ItemsSource = ContrastLines;
        RefreshShapeSummary();
        RefreshShapePreview();
        SeedIndigoRadio.IsChecked = MavTheme.SeedColor == MavColors.Indigo;
        SeedPurpleRadio.IsChecked = MavTheme.SeedColor == MavColors.Purple;
        SeedBlueRadio.IsChecked = MavTheme.SeedColor == MavColors.Blue;
        SeedTealRadio.IsChecked = MavTheme.SeedColor == MavColors.Teal;
        SeedGreenRadio.IsChecked = MavTheme.SeedColor == MavColors.Green;
        SeedOrangeRadio.IsChecked = MavTheme.SeedColor == MavColors.Orange;
        SeedRoseRadio.IsChecked = MavTheme.SeedColor == MavColors.Rose;
        SeedSlateRadio.IsChecked = MavTheme.SeedColor == MavColors.Slate;
        HighlightChoices();
    }

    /// <summary>Explains the three shape buttons in numbers, for the scale that is active right now.</summary>
    private void RefreshShapeSummary()
    {
        var shape = MavTheme.Current.Shape;
        var mode = MavTheme.ShapeScaleMode;
        ShapeSummary.Text = MavLocalization.Format(
            "Theming.Shape.Summary",
            "Shape scale multiplies every corner radius: Compact ×0.72, Standard ×1.0, Expressive ×1.28. " +
            "Current {0} — cards {1:0.#} px, chips {2:0.#} px, text fields {3:0.#} px.",
            mode,
            shape.Medium,
            shape.Small,
            shape.ExtraSmall);
    }

    /// <summary>Real controls whose corner radius comes from the shape tokens, rebuilt for the current scale.</summary>
    private void RefreshShapePreview()
    {
        var shape = MavTheme.Current.Shape;
        ShapePreview.Children.Clear();

        void Add(Control control, double margin)
        {
            control.Margin = new Thickness(0, 0, margin, 8);
            ShapePreview.Children.Add(control);
        }

        Add(new MavCard
        {
            Variant = MavCardVariant.Outlined,
            CardPadding = new Thickness(12, 10),
            Child = new MavLabel
            {
                Text = $"MavCard · {shape.Medium:0.#} px",
                Role = MavTypeRole.LabelMedium,
            },
        }, 12);

        Add(new MavChip($"MavChip · {shape.Small:0.#} px"), 12);

        Add(new MavInfoBar(
            title: MavLocalization.Get("Theming.Shape.Preview.InfoBar", "MavInfoBar"),
            message: $"{shape.Medium:0.#} px",
            severity: MavInfoBarSeverity.Info,
            isClosable: false), 12);
    }

    private void HighlightChoices()
    {
        ModeLightButton.Variant = ChoiceButtonStyle.Of(MavTheme.Mode == MavThemeMode.Light, MavButtonVariant.Outlined);
        ModeDarkButton.Variant = ChoiceButtonStyle.Of(MavTheme.Mode == MavThemeMode.Dark, MavButtonVariant.Outlined);
        ModeSystemButton.Variant = ChoiceButtonStyle.Of(MavTheme.Mode == MavThemeMode.System, MavButtonVariant.Outlined);
        ContrastStandardButton.Variant = ChoiceButtonStyle.Of(MavTheme.Contrast == MavContrast.Standard, MavButtonVariant.Tonal);
        ContrastMediumButton.Variant = ChoiceButtonStyle.Of(MavTheme.Contrast == MavContrast.Medium, MavButtonVariant.Tonal);
        ContrastHighButton.Variant = ChoiceButtonStyle.Of(MavTheme.Contrast == MavContrast.High, MavButtonVariant.Tonal);
        DensityComfortableButton.Variant = ChoiceButtonStyle.Of(MavTheme.Density == MavDensity.Comfortable, MavButtonVariant.Text);
        DensityCompactButton.Variant = ChoiceButtonStyle.Of(MavTheme.Density == MavDensity.Compact, MavButtonVariant.Text);
        DensityDenseButton.Variant = ChoiceButtonStyle.Of(MavTheme.Density == MavDensity.Dense, MavButtonVariant.Text);
        MotionExpressiveButton.Variant = ChoiceButtonStyle.Of(MavTheme.Motion == MavMotionScheme.Expressive, MavButtonVariant.Outlined);
        MotionStandardButton.Variant = ChoiceButtonStyle.Of(MavTheme.Motion == MavMotionScheme.Standard, MavButtonVariant.Outlined);
        MotionReducedButton.Variant = ChoiceButtonStyle.Of(MavTheme.Motion == MavMotionScheme.Reduced, MavButtonVariant.Outlined);
        MotionNoneButton.Variant = ChoiceButtonStyle.Of(MavTheme.Motion == MavMotionScheme.None, MavButtonVariant.Outlined);
        ShapeCompactButton.Variant = ChoiceButtonStyle.Of(MavTheme.ShapeScaleMode == MavShapeScale.Compact, MavButtonVariant.Tonal);
        ShapeStandardButton.Variant = ChoiceButtonStyle.Of(MavTheme.ShapeScaleMode == MavShapeScale.Standard, MavButtonVariant.Tonal);
        ShapeExpressiveButton.Variant = ChoiceButtonStyle.Of(MavTheme.ShapeScaleMode == MavShapeScale.Expressive, MavButtonVariant.Tonal);
    }
}
