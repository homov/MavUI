using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Controls;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Gallery;

internal static class GalleryUi
{
    public const double MaxContentWidth = 1180;

    public static Control Page(params Control[] children)
    {
        var stack = new StackPanel { Spacing = 18 };
        foreach (var child in children)
            stack.Children.Add(child);

        var content = new Border
        {
            MaxWidth = MaxContentWidth,
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20, 16, 20, 40),
            Child = stack,
        };

        return new MavScrollViewer { Content = content };
    }

    public static Control Section(string title, string? description = null, params Control[] children)
    {
        var stack = new StackPanel { Spacing = 12 };
        stack.Children.Add(new MavLabel
        {
            Text = title,
            Role = MavTypeRole.HeadlineSmall,
        });

        if (!string.IsNullOrWhiteSpace(description))
        {
            stack.Children.Add(new MavLabel
            {
                Text = description,
                Role = MavTypeRole.BodyMedium,
                Secondary = true,
                TextWrapping = TextWrapping.Wrap,
            });
        }

        foreach (var child in children)
            stack.Children.Add(child);

        return stack;
    }

    public static Control Showcase(string name, string description, Control demo, string api)
    {
        var stack = new StackPanel { Spacing = 10 };
        stack.Children.Add(new MavLabel
        {
            Text = name,
            Role = MavTypeRole.TitleMedium,
        });
        stack.Children.Add(new MavLabel
        {
            Text = description,
            Role = MavTypeRole.BodySmall,
            Secondary = true,
            TextWrapping = TextWrapping.Wrap,
        });
        stack.Children.Add(demo);
        stack.Children.Add(new MavLabel
        {
            Text = api,
            Role = MavTypeRole.LabelSmall,
            Secondary = true,
            TextWrapping = TextWrapping.Wrap,
        });

        return new MavCard(MavCardVariant.Outlined, stack)
        {
            Margin = new Thickness(0),
            CardPadding = new Thickness(16),
        };
    }

    public static Control Row(params Control[] children)
    {
        var panel = new WrapPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
        };

        foreach (var child in children)
        {
            child.Margin = new Thickness(0, 0, 8, 8);
            panel.Children.Add(child);
        }

        return panel;
    }

    public static MavButton Button(
        string text,
        MavButtonVariant variant,
        string? icon,
        Action action,
        bool isToggle = false)
    {
        var button = new MavButton(text, variant, icon) { IsToggle = isToggle };
        button.Clicked += (_, _) => action();
        return button;
    }

    public static Control Column(params Control[] children)
    {
        var stack = new StackPanel { Spacing = 8 };
        foreach (var child in children)
            stack.Children.Add(child);
        return stack;
    }

    public static MavLabel Muted(string text, MavTypeRole role = MavTypeRole.BodySmall) => new()
    {
        Text = text,
        Role = role,
        Secondary = true,
        TextWrapping = TextWrapping.Wrap,
    };

    public static Control Swatch(string name, MavColorRole role)
    {
        var color = MavTheme.ColorOf(role);
        var onColor = MavAccessibility.RelativeLuminance(color) >= 0.5
            ? Colors.Black
            : Colors.White;

        var label = new TextBlock
        {
            Text = name,
            FontSize = 11,
            Foreground = MavTheme.BrushOf(onColor),
        };

        return new Border
        {
            Width = 132,
            Height = 64,
            CornerRadius = new CornerRadius(12),
            Background = MavTheme.BrushOf(color),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 8, 8),
            Child = label,
        };
    }

    public static Control Gap(double height = 8) => new Border { Height = height };
}
