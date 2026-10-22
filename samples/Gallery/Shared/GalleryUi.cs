using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Gallery;

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

        return new MdScrollViewer { Content = content };
    }

    public static Control Section(string title, string? description = null, params Control[] children)
    {
        var stack = new StackPanel { Spacing = 12 };
        stack.Children.Add(new MdLabel
        {
            Text = title,
            Role = MdTypeRole.HeadlineSmall,
        });

        if (!string.IsNullOrWhiteSpace(description))
        {
            stack.Children.Add(new MdLabel
            {
                Text = description,
                Role = MdTypeRole.BodyMedium,
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
        stack.Children.Add(new MdLabel
        {
            Text = name,
            Role = MdTypeRole.TitleMedium,
        });
        stack.Children.Add(new MdLabel
        {
            Text = description,
            Role = MdTypeRole.BodySmall,
            Secondary = true,
            TextWrapping = TextWrapping.Wrap,
        });
        stack.Children.Add(demo);
        stack.Children.Add(new MdLabel
        {
            Text = api,
            Role = MdTypeRole.LabelSmall,
            Secondary = true,
            TextWrapping = TextWrapping.Wrap,
        });

        return new MdCard(MdCardVariant.Outlined, stack)
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

    public static MdButton Button(
        string text,
        MdButtonVariant variant,
        string? icon,
        Action action,
        bool isToggle = false)
    {
        var button = new MdButton(text, variant, icon) { IsToggle = isToggle };
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

    public static MdLabel Muted(string text, MdTypeRole role = MdTypeRole.BodySmall) => new()
    {
        Text = text,
        Role = role,
        Secondary = true,
        TextWrapping = TextWrapping.Wrap,
    };

    public static Control Swatch(string name, MdColorRole role)
    {
        var color = MdTheme.ColorOf(role);
        var onColor = MdAccessibility.RelativeLuminance(color) >= 0.5
            ? Colors.Black
            : Colors.White;

        var label = new TextBlock
        {
            Text = name,
            FontSize = 11,
            Foreground = MdTheme.BrushOf(onColor),
        };

        return new Border
        {
            Width = 132,
            Height = 64,
            CornerRadius = new CornerRadius(12),
            Background = MdTheme.BrushOf(color),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 8, 8),
            Child = label,
        };
    }

    public static Control Gap(double height = 8) => new Border { Height = height };
}
