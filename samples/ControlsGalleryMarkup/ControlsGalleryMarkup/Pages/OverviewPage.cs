using Avalonia;
using Avalonia.Controls;
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
            Row(
                Button("Overview.Mode.Light", "Light", MavButtonVariant.Outlined, MavIcons.LightMode, () => MavTheme.UseLight()),
                Button("Overview.Mode.Dark", "Dark", MavButtonVariant.Outlined, MavIcons.DarkMode, () => MavTheme.UseDark()),
                Button("Overview.Mode.System", "System", MavButtonVariant.Outlined, MavIcons.SettingsBrightness, () => MavTheme.UseSystemMode())),
            SeedRow(),
            Row(
                Button("Overview.Contrast.Standard", "Standard contrast", MavButtonVariant.Tonal, null, () => MavTheme.Configure(contrast: MavContrast.Standard)),
                Button("Overview.Contrast.Medium", "Medium", MavButtonVariant.Tonal, null, () => MavTheme.Configure(contrast: MavContrast.Medium)),
                Button("Overview.Contrast.High", "High", MavButtonVariant.Tonal, null, () => MavTheme.Configure(contrast: MavContrast.High))),
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

    private static Control Swatch(MavColorRole role)
    {
        var color = MavTheme.ColorOf(role);
        var on = MavAccessibility.RelativeLuminance(color) >= 0.5 ? Colors.Black : Colors.White;
        return new Border()
            .Width(132)
            .Height(64)
            .CornerRadius(new CornerRadius(12))
            .Padding(10)
            .Background(MavTheme.BrushOf(color))
            .Child(new TextBlock()
                .Text(role.ToString())
                .FontSize(11)
                .Foreground(MavTheme.BrushOf(on)))
            .Margin(new Thickness(0, 0, 8, 8));
    }

    private static Control ShapeSamples()
    {
        var shape = MavTheme.Current.Shape;
        var brush = MavTheme.BrushOf(MavTheme.ColorOf(MavColorRole.PrimaryContainer));
        var radii = new (string Name, double Radius)[]
        {
            ("ExtraSmall", shape.ExtraSmall), ("Small", shape.Small), ("Medium", shape.Medium),
            ("Large", shape.Large), ("ExtraLarge", shape.ExtraLarge), ("Full", shape.Full),
        };

        return Row(radii.Select(item => (Control)new Border()
            .Width(120)
            .Height(64)
            .CornerRadius(new CornerRadius(item.Radius))
            .Background(brush)
            .Child(Centered(new MavLabel(item.Name, MavTypeRole.LabelSmall)))).ToArray());
    }

    private static Control ElevationSamples()
    {
        var fill = MavTheme.ColorOf(MavColorRole.SurfaceContainerLow);
        return Row(Enumerable.Range(0, 6).Select(level => (Control)new MavSurface()
            .Width(120)
            .Height(72)
            .Radius(12)
            .Elevation(level)
            .FillColor(fill)
            .Content(Centered(new MavLabel(MavLocalization.Format("Overview.Elevation.Level", "Level {0}", level), MavTypeRole.LabelSmall)))).ToArray());
    }
}
