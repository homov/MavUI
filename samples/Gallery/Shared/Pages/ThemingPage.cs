using System.Globalization;
using Avalonia.Controls;
using Mav.Primitives;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Theming;

namespace Mav.Gallery;

internal static class ThemingPage
{
    private static readonly IMavLocalizationProvider DemoLocalization =
        new MavLocalization.DelegateProvider((key, culture) =>
        {
            var russian = culture?.TwoLetterISOLanguageName == "ru";
            return key switch
            {
                "Gallery.Greeting" => russian ? "Привет" : "Hello",
                "Gallery.DialogTitle" => russian ? "Удалить проект?" : "Delete project?",
                "Gallery.DialogMessage" => russian ? "Это действие нельзя отменить." : "This action cannot be undone.",
                MavLocalization.Keys.Ok => russian ? "Хорошо" : "OK",
                MavLocalization.Keys.Cancel => russian ? "Отмена" : "Cancel",
                _ => null,
            };
        });

    public static Control Build()
    {
        return GalleryUi.Page(
            GalleryUi.Section("Configuration", "All theme changes are global and rebuild the palette immediately.", Controls()),
            GalleryUi.Section("Color roles", "Every MD3 color role for the current scheme.", Colors()),
            GalleryUi.Section("Type scale", "15 roles; MavLabel applies the font, size, weight and line height.", TypeScale()),
            GalleryUi.Section("Shape and elevation", "Corner radius tokens and the five elevation levels.", ShapeElevation()),
            GalleryUi.Section("Contrast", "WCAG 2.1 ratios for common role pairs via MavAccessibility.", Contrast()),
            GalleryUi.Section("Localization", "Switch language with one call; no event subscriptions and no manual text reassignment.", Localization()));
    }

    private static Control Controls()
    {
        var seeds = new (string Name, Color Color)[]
        {
            ("Indigo", MavColors.Indigo), ("Purple", MavColors.Purple), ("Blue", MavColors.Blue),
            ("Teal", MavColors.Teal), ("Green", MavColors.Green), ("Orange", MavColors.Orange),
            ("Rose", MavColors.Rose), ("Slate", MavColors.Slate),
        };

        var seedRow = GalleryUi.Row();
        foreach (var (name, color) in seeds)
        {
            var chip = new MavChip(name, MavChipKind.Filter) { Icon = MavIcons.Circle };
            chip.Clicked += (_, _) => MavTheme.Configure(seed: color);
            ((WrapPanel)seedRow).Children.Add(chip);
        }

        return GalleryUi.Showcase(
            "Theme controls",
            "Seed color, mode, contrast, density, motion and shape scale.",
            GalleryUi.Column(
                GalleryUi.Row(
                    GalleryUi.Button("Light", MavButtonVariant.Outlined, MavIcons.LightMode, MavTheme.UseLight),
                    GalleryUi.Button("Dark", MavButtonVariant.Outlined, MavIcons.DarkMode, MavTheme.UseDark),
                    GalleryUi.Button("System", MavButtonVariant.Outlined, MavIcons.SettingsBrightness, MavTheme.UseSystemMode)),
                seedRow,
                GalleryUi.Row(
                    GalleryUi.Button("Contrast: Standard", MavButtonVariant.Tonal, null, () => MavTheme.Configure(contrast: MavContrast.Standard)),
                    GalleryUi.Button("Medium", MavButtonVariant.Tonal, null, () => MavTheme.Configure(contrast: MavContrast.Medium)),
                    GalleryUi.Button("High", MavButtonVariant.Tonal, null, () => MavTheme.Configure(contrast: MavContrast.High))),
                GalleryUi.Row(
                    GalleryUi.Button("Comfortable", MavButtonVariant.Text, null, () => MavTheme.Configure(density: MavDensity.Comfortable)),
                    GalleryUi.Button("Compact", MavButtonVariant.Text, null, () => MavTheme.Configure(density: MavDensity.Compact)),
                    GalleryUi.Button("Dense", MavButtonVariant.Text, null, () => MavTheme.Configure(density: MavDensity.Dense))),
                GalleryUi.Row(
                    GalleryUi.Button("Motion: Expressive", MavButtonVariant.Outlined, null, () => MavTheme.Configure(motion: MavMotionScheme.Expressive)),
                    GalleryUi.Button("Standard", MavButtonVariant.Outlined, null, () => MavTheme.Configure(motion: MavMotionScheme.Standard)),
                    GalleryUi.Button("Reduced", MavButtonVariant.Outlined, null, () => MavTheme.Configure(motion: MavMotionScheme.Reduced)),
                    GalleryUi.Button("None", MavButtonVariant.Outlined, null, () => MavTheme.Configure(motion: MavMotionScheme.None))),
                GalleryUi.Row(
                    GalleryUi.Button("Shape: Compact", MavButtonVariant.Tonal, null, () => MavTheme.Configure(shapeScale: MavShapeScale.Compact)),
                    GalleryUi.Button("Standard", MavButtonVariant.Tonal, null, () => MavTheme.Configure(shapeScale: MavShapeScale.Standard)),
                    GalleryUi.Button("Expressive", MavButtonVariant.Tonal, null, () => MavTheme.Configure(shapeScale: MavShapeScale.Expressive)))),
            "MavTheme.Configure(seed, mode, contrast, density, motion, shapeScale)");
    }

    private static Control Colors()
    {
        var panel = new WrapPanel();
        foreach (var role in Enum.GetValues<MavColorRole>())
            panel.Children.Add(GalleryUi.Swatch(role.ToString(), role));

        return new MavCard(MavCardVariant.Outlined, panel)
        {
            CardPadding = new Thickness(16),
        };
    }

    private static Control TypeScale()
    {
        var stack = new StackPanel { Spacing = 2 };
        foreach (var role in Enum.GetValues<MavTypeRole>())
        {
            stack.Children.Add(new MavLabel
            {
                Text = $"{role}",
                Role = role,
            });
        }

        return new MavCard(MavCardVariant.Outlined, stack) { CardPadding = new Thickness(16) };
    }

    private static Control ShapeElevation()
    {
        var shape = MavTheme.Current.Shape;
        var shapeRow = new WrapPanel();
        foreach (var (name, radius) in new[]
                 {
                     ("None", shape.None), ("ExtraSmall", shape.ExtraSmall), ("Small", shape.Small),
                     ("Medium", shape.Medium), ("Large", shape.Large), ("LargeIncreased", shape.LargeIncreased),
                     ("ExtraLarge", shape.ExtraLarge), ("Full", shape.Full),
                 })
        {
            shapeRow.Children.Add(new Border
            {
                Width = 120,
                Height = 64,
                Margin = new Thickness(0, 0, 8, 8),
                CornerRadius = new CornerRadius(radius),
                Background = MavTheme.BrushOf(MavTheme.ColorOf(MavColorRole.SecondaryContainer)),
                Child = new MavLabel
                {
                    Text = name,
                    Role = MavTypeRole.LabelSmall,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            });
        }

        var elevationRow = new WrapPanel();
        for (var level = 0; level <= 5; level++)
        {
            elevationRow.Children.Add(new MavSurface
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
            });
        }

        return new MavCard(
            MavCardVariant.Outlined,
            GalleryUi.Column(shapeRow, elevationRow))
        {
            CardPadding = new Thickness(16),
        };
    }

    private static Control Contrast()
    {
        var pairs = new (string Name, MavColorRole Foreground, MavColorRole Background)[]
        {
            ("OnPrimary / Primary", MavColorRole.OnPrimary, MavColorRole.Primary),
            ("OnSurface / Surface", MavColorRole.OnSurface, MavColorRole.Surface),
            ("OnSurfaceVariant / Surface", MavColorRole.OnSurfaceVariant, MavColorRole.Surface),
            ("OnError / Error", MavColorRole.OnError, MavColorRole.Error),
            ("OnPrimaryContainer / PrimaryContainer", MavColorRole.OnPrimaryContainer, MavColorRole.PrimaryContainer),
        };

        var stack = new StackPanel { Spacing = 6 };
        foreach (var (name, foreground, background) in pairs)
        {
            var ratio = MavAccessibility.ContrastRatio(
                MavTheme.ColorOf(foreground),
                MavTheme.ColorOf(background));

            stack.Children.Add(new MavLabel
            {
                Text = $"{name}: {ratio:F2}:1 {(ratio >= 4.5 ? "✓ AA" : ratio >= 3 ? "✓ AA large" : "—")}",
                Role = MavTypeRole.BodySmall,
            });
        }

        return new MavCard(MavCardVariant.Outlined, stack) { CardPadding = new Thickness(16) };
    }

    private static Control Localization()
    {
        var greeting = new MavLabel
        {
            Role = MavTypeRole.HeadlineSmall,
        }.Localize("Gallery.Greeting", "Hello");

        var dialogButton = new MavButton("Open dialog", MavButtonVariant.Filled);
        dialogButton.Clicked += (_, _) =>
        {
            var dialog = new MavDialog
            {
                Title = MavLocalization.Get("Gallery.DialogTitle", "Delete project?"),
                Message = MavLocalization.Get("Gallery.DialogMessage", "This action cannot be undone."),
                DismissText = MavLocalization.Cancel,
            };
            dialog.Show(dialogButton);
        };

        var english = GalleryUi.Button("English", MavButtonVariant.Outlined, null, () => UseDemoCulture("en-US"));
        var russian = GalleryUi.Button("Русский", MavButtonVariant.Outlined, null, () => UseDemoCulture("ru-RU"));

        return GalleryUi.Showcase(
            "Language switch",
            "The greeting and dialog labels use the same provider. The language buttons call SetCulture; attached texts update automatically, without event handlers.",
            GalleryUi.Column(
                greeting,
                GalleryUi.Row(english, russian, dialogButton)),
            "MavLocalization.Current = new DelegateProvider(...); MavLocalization.SetCulture(new CultureInfo(\"ru-RU\")); new MavLabel().Localize(\"Gallery.Greeting\")");
    }

    private static void UseDemoCulture(string cultureName)
    {
        MavLocalization.Current = DemoLocalization;
        MavLocalization.SetCulture(new CultureInfo(cultureName));
    }
}
