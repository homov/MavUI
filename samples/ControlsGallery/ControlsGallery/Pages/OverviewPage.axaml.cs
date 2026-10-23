using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Mav;
using Mav.Theming;

namespace Mav.ControlsGallery.Pages;

public partial class OverviewPage : UserControl
{
    public OverviewPage()
    {
        InitializeComponent();
        DataContext = this;
    }

    public IReadOnlyList<MavTypeRole> TypeRoles { get; } = Enum.GetValues<MavTypeRole>();

    public IReadOnlyList<SeedChoice> SeedColors { get; } = BuildSeeds();

    public IReadOnlyList<RoleSwatch> Swatches { get; } = BuildSwatches();

    public IReadOnlyList<ShapeSample> ShapeSamples { get; } = BuildShapeSamples();

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
        var brush = MavTheme.BrushOf(MavTheme.ColorOf(MavColorRole.PrimaryContainer));
        return
        [
            new("ExtraSmall", new CornerRadius(shape.ExtraSmall), brush),
            new("Small", new CornerRadius(shape.Small), brush),
            new("Medium", new CornerRadius(shape.Medium), brush),
            new("Large", new CornerRadius(shape.Large), brush),
            new("ExtraLarge", new CornerRadius(shape.ExtraLarge), brush),
            new("Full", new CornerRadius(shape.Full), brush),
        ];
    }

    private static IReadOnlyList<ElevationSample> BuildElevationSamples()
    {
        var fill = MavTheme.ColorOf(MavColorRole.SurfaceContainerLow);
        return Enumerable.Range(0, 6)
            .Select(level => new ElevationSample(
                MavLocalization.Format("Overview.Elevation.Level", "Level {0}", level),
                level,
                fill))
            .ToList();
    }
}
