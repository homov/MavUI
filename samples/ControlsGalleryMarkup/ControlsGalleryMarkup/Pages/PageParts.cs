using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Theming;

namespace Mav.ControlsGalleryMarkup.Pages;

/// <summary>
/// Shared blocks of the gallery pages, laid out like the AXAML gallery. Everything is written with
/// the generated fluent methods. Texts come from MavLocalization; the English argument is the fallback.
/// </summary>
public static class PageParts
{
    /// <summary>Localized text with an English fallback.</summary>
    public static string L(string key, string english) => MavLocalization.Get(key, english);

    /// <summary>Scrollable page body with the gallery's padding and 18 dp between sections.</summary>
    public static Control Page(params Control[] sections) =>
        new MavScrollViewer().Content(
            new Border().Padding(20, 16, 20, 40).Child(
                new StackPanel().Spacing(18).Children(sections)));

    /// <summary>A section: heading, description, then its content (cards or plain rows).</summary>
    public static Control Section(string titleKey, string titleEnglish, string bodyKey, string bodyEnglish, params Control[] content) =>
        new StackPanel().Spacing(12).Children(
            Concat(
                new MavLabel(L(titleKey, titleEnglish), MavTypeRole.HeadlineSmall),
                Description(bodyKey, bodyEnglish),
                content));

    /// <summary>An outlined card: component name, localized description, the content and an API hint.</summary>
    public static Control Card(string title, string bodyKey, string bodyEnglish, string hint, params Control[] content) =>
        Plain(new StackPanel().Spacing(10).Children(
            Concat(
                new MavLabel(title, MavTypeRole.TitleMedium),
                Description(bodyKey, bodyEnglish, MavTypeRole.BodySmall),
                content,
                new MavLabel(hint, MavTypeRole.LabelSmall).Secondary(true).TextWrapping(TextWrapping.Wrap))));

    /// <summary>An outlined card around plain content, without a title.</summary>
    public static Control Plain(Control body) =>
        new MavCard(MavCardVariant.Outlined, body).CardPadding(16);

    /// <summary>Wrapping row of controls with 8 dp gaps.</summary>
    public static Control Row(params Control[] content) =>
        new WrapPanel().Children(content.Select(Gap).ToArray());

    /// <summary>Equal columns no narrower than <paramref name="minItemWidth"/>, so the cards fill the width as the window grows.</summary>
    public static Control Adaptive(double minItemWidth, params Control[] content) =>
        new AdaptiveWrap().MinItemWidth(minItemWidth).Children(content);

    /// <summary>Disables a control.</summary>
    public static T Disabled<T>(T control) where T : Control =>
        control.IsEnabled(false);

    /// <summary>Centers a control both ways.</summary>
    public static T Centered<T>(T control) where T : Control =>
        control.HorizontalAlignment(HorizontalAlignment.Center).VerticalAlignment(VerticalAlignment.Center);

    /// <summary>Sets the width and, when given, the height.</summary>
    public static T Sized<T>(this T control, double width, double? height = null) where T : Control
    {
        control = control.Width(width);
        return height is double h ? control.Height(h) : control;
    }

    /// <summary>
    /// Colour swatch for one colour role. The fill and the text colour are re-applied on every theme change
    /// through <see cref="MavTheme.Observe{T}"/>, so the swatch keeps showing the current role colour.
    /// </summary>
    public static Control ColorSwatch(MavColorRole role, string text)
    {
        var label = new TextBlock().Text(text).FontSize(11).TextWrapping(TextWrapping.Wrap);
        var swatch = new Border().Sized(132, 64).CornerRadius(new CornerRadius(12)).Padding(10).Child(label);
        MavTheme.Observe(swatch, (b, theme) =>
        {
            var color = theme.Color.Get(role);
            var on = MavAccessibility.RelativeLuminance(color) >= 0.5 ? Colors.Black : Colors.White;
            b.Background = MavTheme.BrushOf(color);
            label.Foreground = MavTheme.BrushOf(on);
        });
        return swatch.Margin(new Thickness(0, 0, 8, 8));
    }

    /// <summary>Gap of 8 dp after a wrapped item.</summary>
    private static Control Gap(Control control) =>
        control.Margin(new Thickness(0, 0, 8, 8));

    private static Control Description(string key, string english, MavTypeRole role = MavTypeRole.BodyMedium) =>
        new MavLabel(L(key, english), role).Secondary(true).TextWrapping(TextWrapping.Wrap);

    /// <summary>Joins the heading, the description, the content and an optional tail into one child list.</summary>
    private static Control[] Concat(Control head, Control body, Control[] content, Control? tail = null)
    {
        var items = new List<Control> { head, body };
        items.AddRange(content);
        if (tail is not null)
            items.Add(tail);
        return items.ToArray();
    }
}
