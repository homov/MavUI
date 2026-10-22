using Avalonia.Controls;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Gallery;

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
        stack.Children.Add(new MdLabel
        {
            Text = "Material Design 3 for Avalonia",
            Role = MdTypeRole.DisplaySmall,
            TextWrapping = TextWrapping.Wrap,
        });
        stack.Children.Add(new MdLabel
        {
            Text = "A pure C# control library: no AXAML templates, no Avalonia control theme and no " +
                   "resource dictionaries. The same controls run on desktop, in the browser and on Android.",
            Role = MdTypeRole.BodyLarge,
            Secondary = true,
            TextWrapping = TextWrapping.Wrap,
        });

        stack.Children.Add(GalleryUi.Row(
            new MdChip("Desktop", MdChipKind.Assist, MdIcons.DesktopWindows),
            new MdChip("WebAssembly", MdChipKind.Assist, MdIcons.Language),
            new MdChip("Android phone", MdChipKind.Assist, MdIcons.Smartphone),
            new MdChip("Tablet", MdChipKind.Assist, MdIcons.Tablet)));

        return new MdCard(MdCardVariant.Filled, stack) { CardPadding = new Thickness(24) };
    }

    private static Control ThemeControls()
    {
        var seedColors = new (string Name, Color Color)[]
        {
            ("Indigo", MdColors.Indigo),
            ("Purple", MdColors.Purple),
            ("Blue", MdColors.Blue),
            ("Teal", MdColors.Teal),
            ("Green", MdColors.Green),
            ("Orange", MdColors.Orange),
            ("Rose", MdColors.Rose),
        };

        var seeds = GalleryUi.Row();
        foreach (var (name, color) in seedColors)
        {
            var chip = new MdChip(name, MdChipKind.Filter)
            {
                Icon = MdIcons.Circle,
            };
            chip.Clicked += (_, _) => MdTheme.Configure(seed: color);
            ((WrapPanel)seeds).Children.Add(chip);
        }

        return GalleryUi.Section(
            "Theme",
            "Mode, seed color, contrast, density, motion and shape scale are global; change them and every control updates.",
            GalleryUi.Row(
                GalleryUi.Button("Light", MdButtonVariant.Outlined, MdIcons.LightMode, MdTheme.UseLight),
                GalleryUi.Button("Dark", MdButtonVariant.Outlined, MdIcons.DarkMode, MdTheme.UseDark),
                GalleryUi.Button("System", MdButtonVariant.Outlined, MdIcons.SettingsBrightness, MdTheme.UseSystemMode)),
            seeds,
            GalleryUi.Row(
                GalleryUi.Button("Standard contrast", MdButtonVariant.Tonal, null,
                    () => MdTheme.Configure(contrast: MdContrast.Standard)),
                GalleryUi.Button("Medium", MdButtonVariant.Tonal, null,
                    () => MdTheme.Configure(contrast: MdContrast.Medium)),
                GalleryUi.Button("High", MdButtonVariant.Tonal, null,
                    () => MdTheme.Configure(contrast: MdContrast.High))),
            GalleryUi.Row(
                GalleryUi.Button("Comfortable", MdButtonVariant.Text, null,
                    () => MdTheme.Configure(density: MdDensity.Comfortable)),
                GalleryUi.Button("Compact", MdButtonVariant.Text, null,
                    () => MdTheme.Configure(density: MdDensity.Compact)),
                GalleryUi.Button("Dense", MdButtonVariant.Text, null,
                    () => MdTheme.Configure(density: MdDensity.Dense))),
            GalleryUi.Row(
                GalleryUi.Button("Expressive motion", MdButtonVariant.Outlined, null,
                    () => MdTheme.Configure(motion: MdMotionScheme.Expressive)),
                GalleryUi.Button("Standard", MdButtonVariant.Outlined, null,
                    () => MdTheme.Configure(motion: MdMotionScheme.Standard)),
                GalleryUi.Button("Reduced", MdButtonVariant.Outlined, null,
                    () => MdTheme.Configure(motion: MdMotionScheme.Reduced))));
    }

    private static Control Typography()
    {
        var stack = new StackPanel { Spacing = 4 };
        foreach (var role in Enum.GetValues<MdTypeRole>())
        {
            stack.Children.Add(new MdLabel
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
        var roles = new MdColorRole[]
        {
            MdColorRole.Primary, MdColorRole.Secondary, MdColorRole.Tertiary,
            MdColorRole.Error, MdColorRole.Surface, MdColorRole.SurfaceVariant,
            MdColorRole.SurfaceContainerLow, MdColorRole.SurfaceContainer,
            MdColorRole.SurfaceContainerHigh, MdColorRole.SurfaceContainerHighest,
            MdColorRole.Outline, MdColorRole.InverseSurface,
            MdColorRole.PrimaryContainer, MdColorRole.SecondaryContainer,
            MdColorRole.TertiaryContainer, MdColorRole.ErrorContainer,
        };

        var panel = new WrapPanel();
        foreach (var role in roles)
        {
            var swatch = GalleryUi.Swatch(role.ToString(), role);
            panel.Children.Add(swatch);
        }

        return GalleryUi.Section(
            "Color roles",
            "Role-based colors from MdTheme.ColorOf / MdTheme.BrushOf; they follow the current scheme.",
            panel);
    }

    private static Control ShapeAndElevation()
    {
        var shape = MdTheme.Current.Shape;
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
                Background = MdTheme.BrushOf(MdTheme.ColorOf(MdColorRole.PrimaryContainer)),
                Child = new MdLabel
                {
                    Text = name,
                    Role = MdTypeRole.LabelSmall,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            });
        }

        var elevations = new WrapPanel();
        for (var level = 0; level <= 5; level++)
        {
            var surface = new MdSurface
            {
                Width = 120,
                Height = 72,
                Margin = new Thickness(0, 0, 8, 8),
                Radius = 12,
                Elevation = level,
                FillColor = MdTheme.ColorOf(MdColorRole.SurfaceContainerLow),
                Content = new MdLabel
                {
                    Text = $"Level {level}",
                    Role = MdTypeRole.LabelSmall,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };
            elevations.Children.Add(surface);
        }

        return GalleryUi.Section(
            "Shape and elevation",
            "Shape tokens scale with MdShapeScale; elevation uses two-layer MD3 shadows.",
            shapes,
            elevations);
    }

    private static Control Motion()
    {
        return GalleryUi.Section(
            "Motion",
            "Durations and easings from MdMotion; MdMotionScheme can be Standard, Expressive, Reduced or None.",
            GalleryUi.Row(
                new MdChip("Short 50–200 ms", MdChipKind.Assist),
                new MdChip("Medium 250–400 ms", MdChipKind.Assist),
                new MdChip("Long 450–600 ms", MdChipKind.Assist),
                new MdChip("ExtraLong 700–1000 ms", MdChipKind.Assist)));
    }
}
