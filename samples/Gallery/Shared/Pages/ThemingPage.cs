using System.Globalization;
using Avalonia.Controls;
using Md3.Avalonia.Primitives;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Theming;

namespace Md3.Gallery;

internal static class ThemingPage
{
    private static readonly IMdLocalizationProvider DemoLocalization =
        new MdLocalization.DelegateProvider((key, culture) =>
        {
            var russian = culture?.TwoLetterISOLanguageName == "ru";
            return key switch
            {
                "Gallery.Greeting" => russian ? "Привет" : "Hello",
                "Gallery.DialogTitle" => russian ? "Удалить проект?" : "Delete project?",
                "Gallery.DialogMessage" => russian ? "Это действие нельзя отменить." : "This action cannot be undone.",
                MdLocalization.Keys.Ok => russian ? "Хорошо" : "OK",
                MdLocalization.Keys.Cancel => russian ? "Отмена" : "Cancel",
                _ => null,
            };
        });

    public static Control Build()
    {
        return GalleryUi.Page(
            GalleryUi.Section("Configuration", "All theme changes are global and rebuild the palette immediately.", Controls()),
            GalleryUi.Section("Color roles", "Every MD3 color role for the current scheme.", Colors()),
            GalleryUi.Section("Type scale", "15 roles; MdLabel applies the font, size, weight and line height.", TypeScale()),
            GalleryUi.Section("Shape and elevation", "Corner radius tokens and the five elevation levels.", ShapeElevation()),
            GalleryUi.Section("Contrast", "WCAG 2.1 ratios for common role pairs via MdAccessibility.", Contrast()),
            GalleryUi.Section("Localization", "Switch language with one call; no event subscriptions and no manual text reassignment.", Localization()));
    }

    private static Control Controls()
    {
        var seeds = new (string Name, Color Color)[]
        {
            ("Indigo", MdColors.Indigo), ("Purple", MdColors.Purple), ("Blue", MdColors.Blue),
            ("Teal", MdColors.Teal), ("Green", MdColors.Green), ("Orange", MdColors.Orange),
            ("Rose", MdColors.Rose), ("Slate", MdColors.Slate),
        };

        var seedRow = GalleryUi.Row();
        foreach (var (name, color) in seeds)
        {
            var chip = new MdChip(name, MdChipKind.Filter) { Icon = MdIcons.Circle };
            chip.Clicked += (_, _) => MdTheme.Configure(seed: color);
            ((WrapPanel)seedRow).Children.Add(chip);
        }

        return GalleryUi.Showcase(
            "Theme controls",
            "Seed color, mode, contrast, density, motion and shape scale.",
            GalleryUi.Column(
                GalleryUi.Row(
                    GalleryUi.Button("Light", MdButtonVariant.Outlined, MdIcons.LightMode, MdTheme.UseLight),
                    GalleryUi.Button("Dark", MdButtonVariant.Outlined, MdIcons.DarkMode, MdTheme.UseDark),
                    GalleryUi.Button("System", MdButtonVariant.Outlined, MdIcons.SettingsBrightness, MdTheme.UseSystemMode)),
                seedRow,
                GalleryUi.Row(
                    GalleryUi.Button("Contrast: Standard", MdButtonVariant.Tonal, null, () => MdTheme.Configure(contrast: MdContrast.Standard)),
                    GalleryUi.Button("Medium", MdButtonVariant.Tonal, null, () => MdTheme.Configure(contrast: MdContrast.Medium)),
                    GalleryUi.Button("High", MdButtonVariant.Tonal, null, () => MdTheme.Configure(contrast: MdContrast.High))),
                GalleryUi.Row(
                    GalleryUi.Button("Comfortable", MdButtonVariant.Text, null, () => MdTheme.Configure(density: MdDensity.Comfortable)),
                    GalleryUi.Button("Compact", MdButtonVariant.Text, null, () => MdTheme.Configure(density: MdDensity.Compact)),
                    GalleryUi.Button("Dense", MdButtonVariant.Text, null, () => MdTheme.Configure(density: MdDensity.Dense))),
                GalleryUi.Row(
                    GalleryUi.Button("Motion: Expressive", MdButtonVariant.Outlined, null, () => MdTheme.Configure(motion: MdMotionScheme.Expressive)),
                    GalleryUi.Button("Standard", MdButtonVariant.Outlined, null, () => MdTheme.Configure(motion: MdMotionScheme.Standard)),
                    GalleryUi.Button("Reduced", MdButtonVariant.Outlined, null, () => MdTheme.Configure(motion: MdMotionScheme.Reduced)),
                    GalleryUi.Button("None", MdButtonVariant.Outlined, null, () => MdTheme.Configure(motion: MdMotionScheme.None))),
                GalleryUi.Row(
                    GalleryUi.Button("Shape: Compact", MdButtonVariant.Tonal, null, () => MdTheme.Configure(shapeScale: MdShapeScale.Compact)),
                    GalleryUi.Button("Standard", MdButtonVariant.Tonal, null, () => MdTheme.Configure(shapeScale: MdShapeScale.Standard)),
                    GalleryUi.Button("Expressive", MdButtonVariant.Tonal, null, () => MdTheme.Configure(shapeScale: MdShapeScale.Expressive)))),
            "MdTheme.Configure(seed, mode, contrast, density, motion, shapeScale)");
    }

    private static Control Colors()
    {
        var panel = new WrapPanel();
        foreach (var role in Enum.GetValues<MdColorRole>())
            panel.Children.Add(GalleryUi.Swatch(role.ToString(), role));

        return new MdCard(MdCardVariant.Outlined, panel)
        {
            CardPadding = new Thickness(16),
        };
    }

    private static Control TypeScale()
    {
        var stack = new StackPanel { Spacing = 2 };
        foreach (var role in Enum.GetValues<MdTypeRole>())
        {
            stack.Children.Add(new MdLabel
            {
                Text = $"{role}",
                Role = role,
            });
        }

        return new MdCard(MdCardVariant.Outlined, stack) { CardPadding = new Thickness(16) };
    }

    private static Control ShapeElevation()
    {
        var shape = MdTheme.Current.Shape;
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
                Background = MdTheme.BrushOf(MdTheme.ColorOf(MdColorRole.SecondaryContainer)),
                Child = new MdLabel
                {
                    Text = name,
                    Role = MdTypeRole.LabelSmall,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            });
        }

        var elevationRow = new WrapPanel();
        for (var level = 0; level <= 5; level++)
        {
            elevationRow.Children.Add(new MdSurface
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
            });
        }

        return new MdCard(
            MdCardVariant.Outlined,
            GalleryUi.Column(shapeRow, elevationRow))
        {
            CardPadding = new Thickness(16),
        };
    }

    private static Control Contrast()
    {
        var pairs = new (string Name, MdColorRole Foreground, MdColorRole Background)[]
        {
            ("OnPrimary / Primary", MdColorRole.OnPrimary, MdColorRole.Primary),
            ("OnSurface / Surface", MdColorRole.OnSurface, MdColorRole.Surface),
            ("OnSurfaceVariant / Surface", MdColorRole.OnSurfaceVariant, MdColorRole.Surface),
            ("OnError / Error", MdColorRole.OnError, MdColorRole.Error),
            ("OnPrimaryContainer / PrimaryContainer", MdColorRole.OnPrimaryContainer, MdColorRole.PrimaryContainer),
        };

        var stack = new StackPanel { Spacing = 6 };
        foreach (var (name, foreground, background) in pairs)
        {
            var ratio = MdAccessibility.ContrastRatio(
                MdTheme.ColorOf(foreground),
                MdTheme.ColorOf(background));

            stack.Children.Add(new MdLabel
            {
                Text = $"{name}: {ratio:F2}:1 {(ratio >= 4.5 ? "✓ AA" : ratio >= 3 ? "✓ AA large" : "—")}",
                Role = MdTypeRole.BodySmall,
            });
        }

        return new MdCard(MdCardVariant.Outlined, stack) { CardPadding = new Thickness(16) };
    }

    private static Control Localization()
    {
        var greeting = new MdLabel
        {
            Role = MdTypeRole.HeadlineSmall,
        }.Localize("Gallery.Greeting", "Hello");

        var dialogButton = new MdButton("Open dialog", MdButtonVariant.Filled);
        dialogButton.Clicked += (_, _) =>
        {
            var dialog = new MdDialog
            {
                Title = MdLocalization.Get("Gallery.DialogTitle", "Delete project?"),
                Message = MdLocalization.Get("Gallery.DialogMessage", "This action cannot be undone."),
                DismissText = MdLocalization.Cancel,
            };
            dialog.Show(dialogButton);
        };

        var english = GalleryUi.Button("English", MdButtonVariant.Outlined, null, () => UseDemoCulture("en-US"));
        var russian = GalleryUi.Button("Русский", MdButtonVariant.Outlined, null, () => UseDemoCulture("ru-RU"));

        return GalleryUi.Showcase(
            "Language switch",
            "The greeting and dialog labels use the same provider. The language buttons call SetCulture; attached texts update automatically, without event handlers.",
            GalleryUi.Column(
                greeting,
                GalleryUi.Row(english, russian, dialogButton)),
            "MdLocalization.Current = new DelegateProvider(...); MdLocalization.SetCulture(new CultureInfo(\"ru-RU\")); new MdLabel().Localize(\"Gallery.Greeting\")");
    }

    private static void UseDemoCulture(string cultureName)
    {
        MdLocalization.Current = DemoLocalization;
        MdLocalization.SetCulture(new CultureInfo(cultureName));
    }
}
