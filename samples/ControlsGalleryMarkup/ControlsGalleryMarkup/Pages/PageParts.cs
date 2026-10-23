using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Theming;

namespace Mav.ControlsGalleryMarkup.Pages;

/// <summary>
/// Shared blocks of the gallery pages, laid out like the AXAML gallery: a scrollable body,
/// section headings with a description, and outlined cards with a title, a description and an API hint.
/// Texts come from MavLocalization; the English argument is the fallback.
/// </summary>
public static class PageParts
{
    /// <summary>Localized text with an English fallback.</summary>
    public static string L(string key, string english) => MavLocalization.Get(key, english);

    /// <summary>Scrollable page body with the gallery's padding and 18 dp between sections.</summary>
    public static Control Page(params Control[] sections) =>
        new MavScrollViewer
        {
            Content = new Border
            {
                Padding = new Thickness(20, 16, 20, 40),
                Child = new StackPanel().Spacing(18).Children(sections),
            },
        };

    /// <summary>A section: heading, description, then its content (cards or plain rows).</summary>
    public static Control Section(string titleKey, string titleEnglish, string bodyKey, string bodyEnglish, params Control[] content)
    {
        var column = new StackPanel().Spacing(12);
        column.Children.Add(new MavLabel(L(titleKey, titleEnglish), MavTypeRole.HeadlineSmall));
        column.Children.Add(new MavLabel(L(bodyKey, bodyEnglish), MavTypeRole.BodyMedium)
            .Secondary(true)
            .TextWrapping(TextWrapping.Wrap));
        foreach (var item in content)
            column.Children.Add(item);
        return column;
    }

    /// <summary>An outlined card: component name, localized description, the content and an API hint.</summary>
    public static Control Card(string title, string bodyKey, string bodyEnglish, string hint, params Control[] content)
    {
        var column = new StackPanel().Spacing(10);
        column.Children.Add(new MavLabel(title, MavTypeRole.TitleMedium));
        column.Children.Add(new MavLabel(L(bodyKey, bodyEnglish), MavTypeRole.BodySmall)
            .Secondary(true)
            .TextWrapping(TextWrapping.Wrap));
        foreach (var item in content)
            column.Children.Add(item);
        column.Children.Add(new MavLabel(hint, MavTypeRole.LabelSmall)
            .Secondary(true)
            .TextWrapping(TextWrapping.Wrap));
        return new MavCard(MavCardVariant.Outlined, column).CardPadding(16);
    }

    /// <summary>Wrapping row of controls with 8 dp gaps.</summary>
    public static Control Row(params Control[] content)
    {
        var panel = new WrapPanel();
        foreach (var item in content)
            panel.Children.Add(Tight(item));
        return panel;
    }

    /// <summary>Wrapping grid whose items are at least <paramref name="minItemWidth"/> wide (the AdaptiveWrap of the AXAML gallery).</summary>
    public static Control AdaptiveWrap(double minItemWidth, params Control[] content)
    {
        var panel = new WrapPanel { ItemWidth = minItemWidth };
        foreach (var item in content)
            panel.Children.Add(Tight(item));
        return panel;
    }

    /// <summary>An outlined card around plain content, without a title.</summary>
    public static Control Plain(Control body) => new MavCard(MavCardVariant.Outlined, body).CardPadding(16);

    /// <summary>Adds the 8 dp right and bottom gap used between wrapped items.</summary>
    public static T Tight<T>(T control) where T : Control
    {
        control.Margin = new Thickness(0, 0, 8, 8);
        return control;
    }

    /// <summary>Disables a control. Used instead of the generated IsEnabled setter, which clashes with an Avalonia markup extension.</summary>
    public static T Disabled<T>(T control) where T : Control
    {
        control.IsEnabled = false;
        return control;
    }
}
