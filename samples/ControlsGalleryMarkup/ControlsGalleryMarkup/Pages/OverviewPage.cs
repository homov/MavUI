using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Primitives;
using Mav.Theming;
using static Mav.ControlsGalleryMarkup.Pages.PageParts;

namespace Mav.ControlsGalleryMarkup.Pages;

/// <summary>Theme, typography, color, shape and motion overview. Mirrors the AXAML Overview page.</summary>
public static class OverviewPage
{
    private static readonly (string Name, Color Color)[] Seeds =
    [
        ("Indigo", MavColors.Indigo), ("Purple", MavColors.Purple), ("Blue", MavColors.Blue),
        ("Teal", MavColors.Teal), ("Green", MavColors.Green), ("Orange", MavColors.Orange),
        ("Rose", MavColors.Rose),
    ];

    private static readonly MavColorRole[] Roles =
    [
        MavColorRole.Primary, MavColorRole.Secondary, MavColorRole.Tertiary,
        MavColorRole.Error, MavColorRole.Surface, MavColorRole.SurfaceVariant,
        MavColorRole.SurfaceContainerLow, MavColorRole.SurfaceContainer,
        MavColorRole.SurfaceContainerHigh, MavColorRole.SurfaceContainerHighest,
        MavColorRole.Outline, MavColorRole.InverseSurface,
        MavColorRole.PrimaryContainer, MavColorRole.SecondaryContainer,
        MavColorRole.TertiaryContainer, MavColorRole.ErrorContainer,
    ];

    public static Control Build() => Page(
        Hero(),
        Section(
            "Overview.Theme.Title", "Theme",
            "Overview.Theme.Body", "Mode, seed color, contrast, density, motion and shape scale are global; change them and every control updates.",
            ModeRow(),
            SeedRow(),
            SchemeVariantRow(),
            ContrastRow(),
            Row(
                Button("Overview.Density.Comfortable", "Comfortable", MavButtonVariant.Text, null, () => MavTheme.Configure(density: MavDensity.Comfortable)),
                Button("Overview.Density.Compact", "Compact", MavButtonVariant.Text, null, () => MavTheme.Configure(density: MavDensity.Compact)),
                Button("Overview.Density.Dense", "Dense", MavButtonVariant.Text, null, () => MavTheme.Configure(density: MavDensity.Dense))),
            Row(
                Button("Overview.Motion.Expressive", "Expressive motion", MavButtonVariant.Outlined, null, () => MavTheme.Configure(motion: MavMotionScheme.Expressive)),
                Button("Overview.Motion.Standard", "Standard", MavButtonVariant.Outlined, null, () => MavTheme.Configure(motion: MavMotionScheme.Standard)),
                Button("Overview.Motion.Reduced", "Reduced", MavButtonVariant.Outlined, null, () => MavTheme.Configure(motion: MavMotionScheme.Reduced)))),
        Section(
            "Overview.Typography.Title", "Typography",
            "Overview.Typography.Body", "15 MD3 type roles from DisplayLarge to LabelSmall.",
            TypeRoles()),
        Section(
            "Overview.Color.Title", "Color roles",
            "Overview.Color.Body", "Role-based colors from MavTheme.ColorOf / MavTheme.BrushOf; they follow the current scheme.",
            ColorSwatches()),
        Section(
            "Overview.Shape.Title", "Shape and elevation",
            "Overview.Shape.Body", "Shape tokens scale with MavShapeScale; elevation uses two-layer MD3 shadows.",
            ShapeSamples(),
            ElevationSamples()),
        Section(
            "Overview.Motion.Title", "Motion",
            "Overview.Motion.Body", "Durations and easings from MavMotion; MavMotionScheme can be Standard, Expressive, Reduced or None.",
            Row(
                Duration("Overview.Duration.Short", "Short 50–200 ms"),
                Duration("Overview.Duration.Medium", "Medium 250–400 ms"),
                Duration("Overview.Duration.Long", "Long 450–600 ms"),
                Duration("Overview.Duration.ExtraLong", "ExtraLong 700–1000 ms"))));

    private static Control Hero() =>
        new MavCard(MavCardVariant.Filled, new StackPanel().Spacing(10).Children(
            new MavLabel(L("Overview.Hero.Title", "Material Design 3 for Avalonia"), MavTypeRole.DisplaySmall)
                .TextWrapping(TextWrapping.Wrap),
            new MavLabel(L("Overview.Hero.Body", "A pure C# control library: no AXAML templates, no Avalonia control theme and no resource dictionaries. The same controls run on desktop, in the browser and on Android."), MavTypeRole.BodyLarge)
                .Secondary(true)
                .TextWrapping(TextWrapping.Wrap),
            Row(
                new MavChip(L("Overview.Chip.Desktop", "Desktop"), MavChipKind.Assist, MavIcons.DesktopWindows),
                new MavChip(L("Overview.Chip.Web", "WebAssembly"), MavChipKind.Assist, MavIcons.Language),
                new MavChip(L("Overview.Chip.Phone", "Android phone"), MavChipKind.Assist, MavIcons.Smartphone),
                new MavChip(L("Overview.Chip.Tablet", "Tablet"), MavChipKind.Assist, MavIcons.Tablet)))).CardPadding(24);

    private static Control Button(string key, string english, MavButtonVariant variant, string? icon, Action onClick) =>
        new MavButton(L(key, english), variant, icon).OnClicked(onClick);

    private static Control SeedRow() =>
        Row(Seeds.Select(seed => (Control)new MavChip(
                L("Overview.Seed." + seed.Name, seed.Name), MavChipKind.Filter, MavIcons.Circle)
            .OnClicked(() => MavTheme.Configure(seed: seed.Color))).ToArray());

    private static Control Duration(string key, string english) =>
        new MavChip(L(key, english), MavChipKind.Assist, null);

    private static Control TypeRoles() =>
        new StackPanel().Spacing(4).Children(
            Enum.GetValues<MavTypeRole>()
                .Select(role => (Control)new MavLabel($"{role} — The quick brown fox", role)
                    .TextWrapping(TextWrapping.Wrap))
                .ToArray());

    private static Control ColorSwatches() =>
        new WrapPanel().Children(Roles.Select(role => Swatch(role)).ToArray());

    private static Control Swatch(MavColorRole role) => ColorSwatch(role, role.ToString());

    private static Control ShapeSamples()
    {
        var radii = new (string Name, Func<MavShapeTokens, double> Radius)[]
        {
            ("ExtraSmall", s => s.ExtraSmall), ("Small", s => s.Small), ("Medium", s => s.Medium),
            ("Large", s => s.Large), ("ExtraLarge", s => s.ExtraLarge), ("Full", s => s.Full),
        };
        return Row(radii.Select(item =>
        {
            var border = new Border()
                .Sized(120, 64)
                .Child(Centered(new MavLabel(item.Name, MavTypeRole.LabelSmall)));
            // Radius and fill are re-read on every theme change.
            MavTheme.Observe(border, (b, theme) =>
            {
                b.CornerRadius = new CornerRadius(item.Radius(theme.Shape));
                b.Background = MavTheme.BrushOf(theme.Color.Get(MavColorRole.PrimaryContainer));
            });
            return (Control)border;
        }).ToArray());
    }

    private static Control ElevationSamples() =>
        Row(Enumerable.Range(0, 6).Select(level =>
        {
            var surface = new MavSurface()
                .Sized(120, 72)
                .Radius(12)
                .Elevation(level)
                .Child(Centered(new MavLabel(MavLocalization.Format("Overview.Elevation.Level", "Level {0}", level), MavTypeRole.LabelSmall)));
            MavTheme.Observe(surface, (s, theme) => s.FillColor = theme.Color.Get(MavColorRole.SurfaceContainerLow));
            return (Control)surface;
        }).ToArray());
}
