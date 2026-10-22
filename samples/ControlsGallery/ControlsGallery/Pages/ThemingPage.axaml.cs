using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.ControlsGallery.Pages;

public partial class ThemingPage : UserControl
{
    public ThemingPage()
    {
        InitializeComponent();

        ConfigHost.Children.Add(BuildThemeControls());
        ColorsHost.Children.Add(BuildColors());
        TypeHost.Children.Add(BuildTypeScale());
        ShapeHost.Children.Add(BuildShapeAndElevation());
        ContrastHost.Children.Add(BuildContrast());
        LocalizationHost.Children.Add(BuildLocalization());
    }

    private static Control Row(params Control[] children)
    {
        var panel = new WrapPanel();
        foreach (var child in children)
        {
            child.Margin = new Thickness(0, 0, 8, 8);
            panel.Children.Add(child);
        }
        return panel;
    }

    private static Control Column(params Control[] children)
    {
        var stack = new StackPanel { Spacing = 8 };
        foreach (var child in children)
            stack.Children.Add(child);
        return stack;
    }

    private static MavButton ActionButton(string text, MavButtonVariant variant, string? icon, Action action)
    {
        var button = new MavButton(text, variant, icon);
        button.Clicked += (_, _) => action();
        return button;
    }

    private static Control BuildThemeControls()
    {
        var seeds = new (string Name, Color Color)[]
        {
            ("Indigo", MavColors.Indigo), ("Purple", MavColors.Purple), ("Blue", MavColors.Blue),
            ("Teal", MavColors.Teal), ("Green", MavColors.Green), ("Orange", MavColors.Orange),
            ("Rose", MavColors.Rose), ("Slate", MavColors.Slate),
        };

        // One seed at a time: radio buttons that wrap with the window width. Each button unchecks the others.
        var seedRow = new WrapPanel();
        var radios = new System.Collections.Generic.List<MavRadioButton>();
        var syncing = false;
        for (var i = 0; i < seeds.Length; i++)
        {
            var index = i;
            var (name, color) = seeds[i];
            var radio = new MavRadioButton(MavLocalization.Get("Overview.Seed." + name, name), i == 0)
            {
                Margin = new Thickness(0, 0, 16, 8),
            };
            radio.PropertyChanged += (_, e) =>
            {
                if (syncing || e.Property != MavRadioButton.IsCheckedProperty)
                    return;

                syncing = true;
                if (radio.IsChecked)
                {
                    for (var j = 0; j < radios.Count; j++)
                        if (j != index) radios[j].IsChecked = false;
                    syncing = false;
                    MavTheme.Configure(seed: color);
                    return;
                }

                // A seed is always selected: re-check the one that was just cleared.
                var any = false;
                foreach (var other in radios)
                    any |= other.IsChecked;
                if (!any) radio.IsChecked = true;
                syncing = false;
            };
            radios.Add(radio);
            seedRow.Children.Add(radio);
        }

        return Column(
            Row(
                ActionButton(MavLocalization.Get("Overview.Mode.Light", "Light"), MavButtonVariant.Outlined, MavIcons.LightMode, MavTheme.UseLight),
                ActionButton(MavLocalization.Get("Overview.Mode.Dark", "Dark"), MavButtonVariant.Outlined, MavIcons.DarkMode, MavTheme.UseDark),
                ActionButton(MavLocalization.Get("Overview.Mode.System", "System"), MavButtonVariant.Outlined, MavIcons.SettingsBrightness, MavTheme.UseSystemMode)),
            seedRow,
            Row(
                ActionButton(MavLocalization.Get("Theming.Contrast.Standard", "Contrast: Standard"), MavButtonVariant.Tonal, null, () => MavTheme.Configure(contrast: MavContrast.Standard)),
                ActionButton(MavLocalization.Get("Overview.Contrast.Medium", "Medium"), MavButtonVariant.Tonal, null, () => MavTheme.Configure(contrast: MavContrast.Medium)),
                ActionButton(MavLocalization.Get("Overview.Contrast.High", "High"), MavButtonVariant.Tonal, null, () => MavTheme.Configure(contrast: MavContrast.High))),
            Row(
                ActionButton(MavLocalization.Get("Overview.Density.Comfortable", "Comfortable"), MavButtonVariant.Text, null, () => MavTheme.Configure(density: MavDensity.Comfortable)),
                ActionButton(MavLocalization.Get("Overview.Density.Compact", "Compact"), MavButtonVariant.Text, null, () => MavTheme.Configure(density: MavDensity.Compact)),
                ActionButton(MavLocalization.Get("Overview.Density.Dense", "Dense"), MavButtonVariant.Text, null, () => MavTheme.Configure(density: MavDensity.Dense))),
            Row(
                ActionButton(MavLocalization.Get("Theming.Motion.Expressive", "Motion: Expressive"), MavButtonVariant.Outlined, null, () => MavTheme.Configure(motion: MavMotionScheme.Expressive)),
                ActionButton(MavLocalization.Get("Overview.Motion.Standard", "Standard"), MavButtonVariant.Outlined, null, () => MavTheme.Configure(motion: MavMotionScheme.Standard)),
                ActionButton(MavLocalization.Get("Overview.Motion.Reduced", "Reduced"), MavButtonVariant.Outlined, null, () => MavTheme.Configure(motion: MavMotionScheme.Reduced)),
                ActionButton(MavLocalization.Get("Theming.Motion.None", "None"), MavButtonVariant.Outlined, null, () => MavTheme.Configure(motion: MavMotionScheme.None))),
            Row(
                ActionButton(MavLocalization.Get("Theming.Shape.Compact", "Shape: Compact"), MavButtonVariant.Tonal, null, () => MavTheme.Configure(shapeScale: MavShapeScale.Compact)),
                ActionButton(MavLocalization.Get("Overview.Motion.Standard", "Standard"), MavButtonVariant.Tonal, null, () => MavTheme.Configure(shapeScale: MavShapeScale.Standard)),
                ActionButton(MavLocalization.Get("Theming.Shape.Expressive", "Expressive"), MavButtonVariant.Tonal, null, () => MavTheme.Configure(shapeScale: MavShapeScale.Expressive))));
    }

    /// <summary>"SurfaceContainerHigh" becomes "Surface Container High", so long role names wrap inside their swatch.</summary>
    private static string SplitWords(string name) =>
        System.Text.RegularExpressions.Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");

    private static Control BuildColors()
    {
        var panel = new WrapPanel();
        foreach (var role in Enum.GetValues<MavColorRole>())
        {
            var color = MavTheme.ColorOf(role);
            var on = MavAccessibility.RelativeLuminance(color) >= 0.5 ? Colors.Black : Colors.White;
            panel.Children.Add(new Border
            {
                Width = 132,
                Height = 64,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 8, 8),
                Background = MavTheme.BrushOf(color),
                Child = new global::Avalonia.Controls.TextBlock
                {
                    Text = SplitWords(role.ToString()),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = MavTheme.BrushOf(on),
                },
            });
        }
        return panel;
    }

    private static Control BuildTypeScale()
    {
        var stack = new StackPanel { Spacing = 2 };
        foreach (var role in Enum.GetValues<MavTypeRole>())
            stack.Children.Add(new MavLabel { Text = role.ToString(), Role = role });
        return stack;
    }

    private static Control BuildShapeAndElevation()
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
                    Text = MavLocalization.Format("Overview.Elevation.Level", "Level {0}", level),
                    Role = MavTypeRole.LabelSmall,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            });
        }

        return Column(shapeRow, elevationRow);
    }

    private static Control BuildContrast()
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
            var ratio = MavAccessibility.ContrastRatio(MavTheme.ColorOf(foreground), MavTheme.ColorOf(background));
            var verdict = ratio >= 4.5 ? "✓ AA" : ratio >= 3 ? "✓ AA large" : "—";
            stack.Children.Add(new MavLabel
            {
                Text = $"{name}: {ratio.ToString("F2", CultureInfo.InvariantCulture)}:1 {verdict}",
                Role = MavTypeRole.BodySmall,
            });
        }
        return stack;
    }

    private static Control BuildLocalization()
    {
        var greeting = new MavLabel { Role = MavTypeRole.HeadlineSmall };
        greeting.Localize("Theming.Greeting", "Hello");

        var dialogButton = new MavButton(MavLocalization.Get("Theming.Lang.OpenDialog", "Open dialog"), MavButtonVariant.Filled);
        dialogButton.Clicked += (_, _) =>
        {
            var dialog = new MavDialog
            {
                Title = MavLocalization.Get("Theming.Dialog.Title", "Delete project?"),
                Message = MavLocalization.Get("Theming.Dialog.Message", "This action cannot be undone."),
                DismissText = MavLocalization.Cancel,
            };
            dialog.Show(dialogButton);
        };

        var english = ActionButton("English", MavButtonVariant.Outlined, null, () => MavLocalization.SetCulture(new CultureInfo("en-US")));
        var russian = ActionButton("Русский", MavButtonVariant.Outlined, null, () => MavLocalization.SetCulture(new CultureInfo("ru-RU")));

        return Column(greeting, Row(english, russian, dialogButton));
    }
}
