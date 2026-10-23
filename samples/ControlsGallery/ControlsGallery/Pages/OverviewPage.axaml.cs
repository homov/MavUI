using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Theming;

namespace Mav.ControlsGallery.Pages;

public partial class OverviewPage : UserControl
{
    public OverviewPage()
    {
        InitializeComponent();
        DataContext = this;
        // Swatches and shape samples are computed from the current theme, so they are rebuilt on every change.
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
        SwatchItems.ItemsSource = BuildSwatches();
        ShapeItems.ItemsSource = BuildShapeSamples();
        HighlightChoices();
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
    }

    public IReadOnlyList<MavTypeRole> TypeRoles { get; } = Enum.GetValues<MavTypeRole>();

    public IReadOnlyList<SeedChoice> SeedColors { get; } = BuildSeeds();

    public IReadOnlyList<ElevationSample> ElevationSamples { get; } = BuildElevationSamples();

    private static IReadOnlyList<SeedChoice> BuildSeeds()
    {
        var seeds = new (string Name, Color Color)[]
        {
            ("Indigo", MavColors.Indigo), ("Purple", MavColors.Purple), ("Blue", MavColors.Blue),
            ("Teal", MavColors.Teal), ("Green", MavColors.Green), ("Orange", MavColors.Orange),
            ("Rose", MavColors.Rose),
        };
        return seeds
            .Select(s => new SeedChoice(MavLocalization.Get("Overview.Seed." + s.Name, s.Name), s.Color))
            .ToList();
    }

    private static IReadOnlyList<RoleSwatch> BuildSwatches()
    {
        var roles = new[]
        {
            MavColorRole.Primary, MavColorRole.Secondary, MavColorRole.Tertiary,
            MavColorRole.Error, MavColorRole.Surface, MavColorRole.SurfaceVariant,
            MavColorRole.SurfaceContainerLow, MavColorRole.SurfaceContainer,
            MavColorRole.SurfaceContainerHigh, MavColorRole.SurfaceContainerHighest,
            MavColorRole.Outline, MavColorRole.InverseSurface,
            MavColorRole.PrimaryContainer, MavColorRole.SecondaryContainer,
            MavColorRole.TertiaryContainer, MavColorRole.ErrorContainer,
        };

        return roles.Select(role =>
        {
            var color = MavTheme.ColorOf(role);
            var on = MavAccessibility.RelativeLuminance(color) >= 0.5 ? Colors.Black : Colors.White;
            return new RoleSwatch(role.ToString(), MavTheme.BrushOf(color), MavTheme.BrushOf(on));
        }).ToList();
    }

    private static IReadOnlyList<ShapeSample> BuildShapeSamples()
    {
        var shape = MavTheme.Current.Shape;
        return
        [
            new("ExtraSmall", new CornerRadius(shape.ExtraSmall)),
            new("Small", new CornerRadius(shape.Small)),
            new("Medium", new CornerRadius(shape.Medium)),
            new("Large", new CornerRadius(shape.Large)),
            new("ExtraLarge", new CornerRadius(shape.ExtraLarge)),
            new("Full", new CornerRadius(shape.Full)),
        ];
    }

    private static IReadOnlyList<ElevationSample> BuildElevationSamples()
    {
        return Enumerable.Range(0, 6)
            .Select(level => new ElevationSample(
                MavLocalization.Format("Overview.Elevation.Level", "Level {0}", level),
                level))
            .ToList();
    }
}
