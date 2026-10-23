using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Primitives;
using Mav.Theming;
using static Mav.ControlsGalleryMarkup.Pages.PageParts;

namespace Mav.ControlsGalleryMarkup.Pages;

/// <summary>Theme configuration, color roles, type scale, shape, contrast and localization. Mirrors the AXAML Theming page.</summary>
public static class ThemingPage
{
    private static readonly (string Name, Color Color)[] Seeds =
    [
        ("Indigo", MavColors.Indigo), ("Purple", MavColors.Purple), ("Blue", MavColors.Blue),
        ("Teal", MavColors.Teal), ("Green", MavColors.Green), ("Orange", MavColors.Orange),
        ("Rose", MavColors.Rose), ("Slate", MavColors.Slate),
    ];

    public static Control Build() => Page(
        Section(
            "Theming.Config.Title", "Configuration",
            "Theming.Config.Body", "All theme changes are global and rebuild the palette immediately.",
            Card(
                L("Theming.Controls.Title", "Theme controls"),
                "Theming.Controls.CardBody", "Seed color, mode, contrast, density, motion and shape scale.",
                "MavTheme.Configure(seed, mode, contrast, density, motion, shapeScale)",
                Stack(8,
                    Row(
                        Button("Overview.Mode.Light", "Light", MavButtonVariant.Outlined, MavIcons.LightMode, () => MavTheme.UseLight()),
                        Button("Overview.Mode.Dark", "Dark", MavButtonVariant.Outlined, MavIcons.DarkMode, () => MavTheme.UseDark()),
                        Button("Overview.Mode.System", "System", MavButtonVariant.Outlined, MavIcons.SettingsBrightness, () => MavTheme.UseSystemMode())),
                    SeedRow(),
                    Row(
                        Button("Theming.Contrast.Standard", "Contrast: Standard", MavButtonVariant.Tonal, null, () => MavTheme.Configure(contrast: MavContrast.Standard)),
                        Button("Overview.Contrast.Medium", "Medium", MavButtonVariant.Tonal, null, () => MavTheme.Configure(contrast: MavContrast.Medium)),
                        Button("Overview.Contrast.High", "High", MavButtonVariant.Tonal, null, () => MavTheme.Configure(contrast: MavContrast.High))),
                    Row(
                        Button("Overview.Density.Comfortable", "Comfortable", MavButtonVariant.Text, null, () => MavTheme.Configure(density: MavDensity.Comfortable)),
                        Button("Overview.Density.Compact", "Compact", MavButtonVariant.Text, null, () => MavTheme.Configure(density: MavDensity.Compact)),
                        Button("Overview.Density.Dense", "Dense", MavButtonVariant.Text, null, () => MavTheme.Configure(density: MavDensity.Dense))),
                    Row(
                        Button("Theming.Motion.Expressive", "Motion: Expressive", MavButtonVariant.Outlined, null, () => MavTheme.Configure(motion: MavMotionScheme.Expressive)),
                        Button("Overview.Motion.Standard", "Standard", MavButtonVariant.Outlined, null, () => MavTheme.Configure(motion: MavMotionScheme.Standard)),
                        Button("Overview.Motion.Reduced", "Reduced", MavButtonVariant.Outlined, null, () => MavTheme.Configure(motion: MavMotionScheme.Reduced)),
                        Button("Theming.Motion.None", "None", MavButtonVariant.Outlined, null, () => MavTheme.Configure(motion: MavMotionScheme.None))),
                    Row(
                        Button("Theming.Shape.Compact", "Shape: Compact", MavButtonVariant.Tonal, null, () => MavTheme.Configure(shapeScale: MavShapeScale.Compact)),
                        Button("Overview.Motion.Standard", "Standard", MavButtonVariant.Tonal, null, () => MavTheme.Configure(shapeScale: MavShapeScale.Standard)),
                        Button("Theming.Shape.Expressive", "Expressive", MavButtonVariant.Tonal, null, () => MavTheme.Configure(shapeScale: MavShapeScale.Expressive)))))),
        Section(
            "Theming.Colors.Title", "Color roles",
            "Theming.Colors.Body", "Every MD3 color role for the current scheme.",
            Card(
                "MavColorRole",
                "Theming.Colors.CardBody", "Each swatch shows the role name over its own color.",
                "MavTheme.ColorOf(role) · MavTheme.BrushOf(color)",
                ColorSwatches())),
        Section(
            "Theming.Type.Title", "Type scale",
            "Theming.Type.Body", "15 roles; MavLabel applies the font, size, weight and line height.",
            Card(
                "MavTypeRole",
                "Theming.Type.CardBody", "Every role rendered with its own style.",
                "new MavLabel { Role = MavTypeRole.TitleLarge }",
                TypeScale())),
        Section(
            "Theming.Shape.Title", "Shape and elevation",
            "Theming.Shape.Body", "Corner radius tokens and the five elevation levels.",
            Card(
                "MavTheme.Current.Shape · MavSurface.Elevation",
                "Theming.Shape.CardBody", "Shape tokens follow MavShapeScale; elevation uses two-layer shadows.",
                "MavTheme.Current.Shape · MavSurface.Elevation",
                Stack(8, ShapeSamples(), ElevationSamples()))),
        Section(
            "Theming.Contrast.Title", "Contrast",
            "Theming.Contrast.Body", "WCAG 2.1 ratios for common role pairs via MavAccessibility.",
            Card(
                "MavAccessibility.ContrastRatio",
                "Theming.Contrast.CardBody", "Ratios are computed from the current scheme.",
                "MavAccessibility.ContrastRatio(foreground, background)",
                ContrastLines())),
        Section(
            "Theming.Localization.Title", "Localization",
            "Theming.Localization.Body", "Switch language with one call; no event subscriptions and no manual text reassignment.",
            Card(
                L("Theming.Language.Title", "Language switch"),
                "Theming.Language.CardBody", "The greeting and dialog labels use the same provider. The language buttons call SetCulture; attached texts update automatically, without event handlers.",
                "MavLocalization.SetCulture(new CultureInfo(\"ru-RU\")) · {mav:Loc Key, Fallback=...}",
                Stack(8,
                    new MavLabel(L("Theming.Greeting", "Hello"), MavTypeRole.HeadlineSmall),
                    Row(
                        Button(null, "English", MavButtonVariant.Outlined, null, () => MavLocalization.SetCulture(new CultureInfo("en-US"))),
                        Button(null, "Русский", MavButtonVariant.Outlined, null, () => MavLocalization.SetCulture(new CultureInfo("ru-RU"))),
                        WithDialog(
                            new MavButton(L("Theming.Lang.OpenDialog", "Open dialog"), MavButtonVariant.Filled, null),
                            new MavDialog()
                                .Title(L("Theming.Dialog.Title", "Delete project?"))
                                .Message(L("Theming.Dialog.Message", "This action cannot be undone."))
                                .DismissText(MavLocalization.Cancel)))))));

    private static Control Button(string? key, string english, MavButtonVariant variant, string? icon, Action onClick) =>
        new MavButton(key is null ? english : L(key, english), variant, icon).OnClicked(onClick);

    private static MavButton WithDialog(MavButton button, MavDialog dialog)
    {
        MavFlyout.SetDialog(button, dialog);
        return button;
    }

    private static StackPanel Stack(double spacing, params Control[] items) =>
        new StackPanel().Spacing(spacing).Children(items);

    /// <summary>Seed colors as a radio group; the first one is checked, as in the AXAML page.</summary>
    private static Control SeedRow() =>
        Row(Seeds.Select((seed, index) => (Control)new MavRadioButton()
                .Label(L("Overview.Seed." + seed.Name, seed.Name))
                .GroupName("Seed")
                .IsChecked(index == 0)
                .OnCheckedChanged((_, on) => { if (on) MavTheme.Configure(seed: seed.Color); }))
            .ToArray());

    /// <summary>Colour swatches for every colour role; the text colour is chosen for contrast.</summary>
    private static Control ColorSwatches() =>
        Row(Enum.GetValues<MavColorRole>().Select(role =>
        {
            var color = MavTheme.ColorOf(role);
            var on = MavAccessibility.RelativeLuminance(color) >= 0.5 ? Colors.Black : Colors.White;
            return (Control)new Border()
                .Sized(132, 64)
                .CornerRadius(new CornerRadius(12))
                .Padding(10)
                .Background(MavTheme.BrushOf(color))
                .Child(new TextBlock()
                    .Text(SplitWords(role.ToString()))
                    .FontSize(11)
                    .TextWrapping(TextWrapping.Wrap)
                    .Foreground(MavTheme.BrushOf(on)));
        }).ToArray());

    /// <summary>One sample per type role, each rendered with its own style.</summary>
    private static Control TypeScale() =>
        Stack(2, Enum.GetValues<MavTypeRole>()
            .Select(role => (Control)new MavLabel(role.ToString(), role))
            .ToArray());

    /// <summary>Corner radius samples from the current shape scale.</summary>
    private static Control ShapeSamples()
    {
        var shape = MavTheme.Current.Shape;
        var brush = MavTheme.BrushOf(MavTheme.ColorOf(MavColorRole.SecondaryContainer));
        var radii = new (string Name, double Radius)[]
        {
            ("None", shape.None), ("ExtraSmall", shape.ExtraSmall), ("Small", shape.Small),
            ("Medium", shape.Medium), ("Large", shape.Large), ("LargeIncreased", shape.LargeIncreased),
            ("ExtraLarge", shape.ExtraLarge), ("Full", shape.Full),
        };
        return Row(radii.Select(item => (Control)new Border()
            .Sized(120, 64)
            .CornerRadius(new CornerRadius(item.Radius))
            .Background(brush)
            .Child(Centered(new MavLabel(item.Name, MavTypeRole.LabelSmall)))).ToArray());
    }

    /// <summary>Elevation levels 0 to 5, with localized labels.</summary>
    private static Control ElevationSamples()
    {
        var fill = MavTheme.ColorOf(MavColorRole.SurfaceContainerLow);
        return Row(Enumerable.Range(0, 6).Select(level => (Control)new MavSurface()
            .Sized(120, 72)
            .Radius(12)
            .Elevation(level)
            .FillColor(fill)
            .Child(Centered(new MavLabel(MavLocalization.Format("Overview.Elevation.Level", "Level {0}", level), MavTypeRole.LabelSmall)))).ToArray());
    }

    /// <summary>Contrast ratios of the main role pairs, with WCAG verdicts.</summary>
    private static Control ContrastLines()
    {
        var pairs = new (string Name, MavColorRole Foreground, MavColorRole Background)[]
        {
            ("OnPrimary / Primary", MavColorRole.OnPrimary, MavColorRole.Primary),
            ("OnSurface / Surface", MavColorRole.OnSurface, MavColorRole.Surface),
            ("OnSurfaceVariant / Surface", MavColorRole.OnSurfaceVariant, MavColorRole.Surface),
            ("OnError / Error", MavColorRole.OnError, MavColorRole.Error),
            ("OnPrimaryContainer / PrimaryContainer", MavColorRole.OnPrimaryContainer, MavColorRole.PrimaryContainer),
        };
        return Stack(6, pairs.Select(p =>
        {
            var ratio = MavAccessibility.ContrastRatio(MavTheme.ColorOf(p.Foreground), MavTheme.ColorOf(p.Background));
            var verdict = ratio >= 4.5 ? "✓ AA" : ratio >= 3 ? "✓ AA large" : "—";
            return (Control)new MavLabel($"{p.Name}: {ratio.ToString("F2", CultureInfo.InvariantCulture)}:1 {verdict}", MavTypeRole.BodySmall);
        }).ToArray());
    }

    /// <summary>"SurfaceContainerHigh" becomes "Surface Container High", so long role names wrap inside their swatch.</summary>
    private static string SplitWords(string name) =>
        Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");
}
