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
/// the generated fluent methods. Two properties clash with the generator (see <see cref="Disabled{T}"/>
/// and <see cref="Centered{T}"/>), and those are called explicitly by their generated class name.
/// Texts come from MavLocalization; the English argument is the fallback.
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

    /// <summary>Wrapping grid whose items are at least <paramref name="minItemWidth"/> wide (the AdaptiveWrap of the AXAML gallery).</summary>
    public static Control AdaptiveWrap(double minItemWidth, params Control[] content) =>
        new WrapPanel().ItemWidth(minItemWidth).Children(content.Select(Gap).ToArray());

    /// <summary>
    /// Disables a control. The generated <c>IsEnabled</c> extensions have one overload per control type,
    /// and the compiler binds the wrong one for <c>MavButton</c> (CS0311). The explicit call avoids the choice.
    /// </summary>
    public static T Disabled<T>(T control) where T : Control =>
        InputElementMarkup.IsEnabled(control, false);

    /// <summary>
    /// Centers a control both ways. <c>HorizontalAlignment</c> and <c>VerticalAlignment</c> are properties of
    /// <c>Layoutable</c>, and a call written as <c>x.HorizontalAlignment(...)</c> binds to the property (CS1955).
    /// </summary>
    public static T Centered<T>(T control) where T : Control
    {
        LayoutableMarkup.HorizontalAlignment(control, HorizontalAlignment.Center);
        return LayoutableMarkup.VerticalAlignment(control, VerticalAlignment.Center);
    }

    /// <summary>
    /// Sets the size. <c>Width</c> has a generated overload for <c>ColumnDefinition</c> (GridLength), so the
    /// fluent <c>.Width(120)</c> binds to the wrong type (CS1503). The explicit <c>LayoutableMarkup</c> calls avoid that.
    /// </summary>
    public static T Sized<T>(this T control, double width, double? height = null) where T : Control
    {
        LayoutableMarkup.Width(control, width);
        if (height is double h)
            LayoutableMarkup.Height(control, h);
        return control;
    }

    /// <summary>Gap of 8 dp after a wrapped item.</summary>
    private static Control Gap(Control control) =>
        LayoutableMarkup.Margin(control, new Thickness(0, 0, 8, 8));

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
