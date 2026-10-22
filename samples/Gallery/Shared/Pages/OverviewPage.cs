using Avalonia.Controls;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Gallery;

internal static class OverviewPage
{
    public static Control Build()
    {
        return GalleryUi.Page(
            Hero(),
            ThemeControls(),
            Typography(),
            ColorRoles(),
            ShapeAndElevation(),
            Motion());
    }

    private static Control Hero()
    {
        var stack = new StackPanel { Spacing = 10 };
        stack.Children.Add(new MavLabel
        {
            Text = "Material Design 3 for Avalonia",
            Role = MavTypeRole.DisplaySmall,
            TextWrapping = TextWrapping.Wrap,
        });
        stack.Children.Add(new MavLabel
        {
            Text = "A pure C# control library: no AXAML templates, no Avalonia control theme and no " +
                   "resource dictionaries. The same controls run on desktop, in the browser and on Android.",
            Role = MavTypeRole.BodyLarge,
            Secondary = true,
            TextWrapping = TextWrapping.Wrap,
        });

        stack.Children.Add(GalleryUi.Row(
            new MavChip("Desktop", MavChipKind.Assist, MavIcons.DesktopWindows),
            new MavChip("WebAssembly", MavChipKind.Assist, MavIcons.Language),
            new MavChip("Android phone", MavChipKind.Assist, MavIcons.Smartphone),
            new MavChip("Tablet", MavChipKind.Assist, MavIcons.Tablet)));

        return new MavCard(MavCardVariant.Filled, stack) { CardPadding = new Thickness(24) };
    }

    private static Control ThemeControls()
    {
        var seedColors = new (string Name, Color Color)[]
        {
            ("Indigo", MavColors.Indigo),
            ("Purple", MavColors.Purple),
            ("Blue", MavColors.Blue),
            ("Teal", MavColors.Teal),
            ("Green", MavColors.Green),
            ("Orange", MavColors.Orange),
            ("Rose", MavColors.Rose),
        };

        var seeds = GalleryUi.Row();
        foreach (var (name, color) in seedColors)
        {
            var chip = new MavChip(name, MavChipKind.Filter)
            {
                Icon = MavIcons.Circle,
            };
            chip.Clicked += (_, _) => MavTheme.Configure(seed: color);
            ((WrapPanel)seeds).Children.Add(chip);
        }

        return GalleryUi.Section(
            "Theme",
            "Mode, seed color, contrast, density, motion and shape scale are global; change them and every control updates.",
            GalleryUi.Row(
                GalleryUi.Button("Light", MavButtonVariant.Outlined, MavIcons.LightMode, MavTheme.UseLight),
                GalleryUi.Button("Dark", MavButtonVariant.Outlined, MavIcons.DarkMode, MavTheme.UseDark),
                GalleryUi.Button("System", MavButtonVariant.Outlined, MavIcons.SettingsBrightness, MavTheme.UseSystemMode)),
            seeds,
            GalleryUi.Row(
                GalleryUi.Button("Standard contrast", MavButtonVariant.Tonal, null,
                    () => MavTheme.Configure(contrast: MavContrast.Standard)),
                GalleryUi.Button("Medium", MavButtonVariant.Tonal, null,
                    () => MavTheme.Configure(contrast: MavContrast.Medium)),
                GalleryUi.Button("High", MavButtonVariant.Tonal, null,
                    () => MavTheme.Configure(contrast: MavContrast.High))),
            GalleryUi.Row(
                GalleryUi.Button("Comfortable", MavButtonVariant.Text, null,
                    () => MavTheme.Configure(density: MavDensity.Comfortable)),
                GalleryUi.Button("Compact", MavButtonVariant.Text, null,
                    () => MavTheme.Configure(density: MavDensity.Compact)),
                GalleryUi.Button("Dense", MavButtonVariant.Text, null,
                    () => MavTheme.Configure(density: MavDensity.Dense))),
            GalleryUi.Row(
                GalleryUi.Button("Expressive motion", MavButtonVariant.Outlined, null,
                    () => MavTheme.Configure(motion: MavMotionScheme.Expressive)),
                GalleryUi.Button("Standard", MavButtonVariant.Outlined, null,
                    () => MavTheme.Configure(motion: MavMotionScheme.Standard)),
                GalleryUi.Button("Reduced", MavButtonVariant.Outlined, null,
                    () => MavTheme.Configure(motion: MavMotionScheme.Reduced))));
    }

    private static Control Typography()
    {
        var stack = new StackPanel { Spacing = 4 };
        foreach (var role in Enum.GetValues<MavTypeRole>())
        {
            stack.Children.Add(new MavLabel
            {
                Text = $"{role} — The quick brown fox",
                Role = role,
                TextWrapping = TextWrapping.Wrap,
            });
        }

        return GalleryUi.Section("Typography", "15 MD3 type roles from DisplayLarge to LabelSmall.", stack);
    }

    private static Control ColorRoles()
    {
        var roles = new MavColorRole[]
        {
            MavColorRole.Primary, MavColorRole.Secondary, MavColorRole.Tertiary,
            MavColorRole.Error, MavColorRole.Surface, MavColorRole.SurfaceVariant,
            MavColorRole.SurfaceContainerLow, MavColorRole.SurfaceContainer,
            MavColorRole.SurfaceContainerHigh, MavColorRole.SurfaceContainerHighest,
            MavColorRole.Outline, MavColorRole.InverseSurface,
            MavColorRole.PrimaryContainer, MavColorRole.SecondaryContainer,
            MavColorRole.TertiaryContainer, MavColorRole.ErrorContainer,
        };

        var panel = new WrapPanel();
        foreach (var role in roles)
        {
            var swatch = GalleryUi.Swatch(role.ToString(), role);
            panel.Children.Add(swatch);
        }

        return GalleryUi.Section(
            "Color roles",
            "Role-based colors from MavTheme.ColorOf / MavTheme.BrushOf; they follow the current scheme.",
            panel);
    }

    private static Control ShapeAndElevation()
    {
        var shape = MavTheme.Current.Shape;
        var radii = new (string Name, double Value)[]
        {
            ("ExtraSmall", shape.ExtraSmall),
            ("Small", shape.Small),
            ("Medium", shape.Medium),
            ("Large", shape.Large),
            ("ExtraLarge", shape.ExtraLarge),
            ("Full", shape.Full),
        };

        var shapes = new WrapPanel();
        foreach (var (name, radius) in radii)
        {
            shapes.Children.Add(new Border
            {
                Width = 120,
                Height = 64,
                Margin = new Thickness(0, 0, 8, 8),
                CornerRadius = new CornerRadius(radius),
                Background = MavTheme.BrushOf(MavTheme.ColorOf(MavColorRole.PrimaryContainer)),
                Child = new MavLabel
                {
                    Text = name,
                    Role = MavTypeRole.LabelSmall,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            });
        }

        var elevations = new WrapPanel();
        for (var level = 0; level <= 5; level++)
        {
            var surface = new MavSurface
            {
                Width = 120,
                Height = 72,
                Margin = new Thickness(0, 0, 8, 8),
                Radius = 12,
                Elevation = level,
                FillColor = MavTheme.ColorOf(MavColorRole.SurfaceContainerLow),
                Content = new MavLabel
                {
                    Text = $"Level {level}",
                    Role = MavTypeRole.LabelSmall,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };
            elevations.Children.Add(surface);
        }

        return GalleryUi.Section(
            "Shape and elevation",
            "Shape tokens scale with MavShapeScale; elevation uses two-layer MD3 shadows.",
            shapes,
            elevations);
    }

    private static Control Motion()
    {
        return GalleryUi.Section(
            "Motion",
            "Durations and easings from MavMotion; MavMotionScheme can be Standard, Expressive, Reduced or None.",
            GalleryUi.Row(
                new MavChip("Short 50–200 ms", MavChipKind.Assist),
                new MavChip("Medium 250–400 ms", MavChipKind.Assist),
                new MavChip("Long 450–600 ms", MavChipKind.Assist),
                new MavChip("ExtraLong 700–1000 ms", MavChipKind.Assist)));
    }
}
