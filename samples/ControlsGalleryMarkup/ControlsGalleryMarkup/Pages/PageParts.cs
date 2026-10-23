using Avalonia.Controls;
using Avalonia.Media;
using Mav.Controls;
using Mav.Theming;

namespace Mav.ControlsGalleryMarkup.Pages;

/// <summary>Shared blocks of the gallery pages: a section heading with a description and outlined cards.</summary>
public static class PageParts
{
    /// <summary>A section: heading, description, and the given content inside one outlined card.</summary>
    public static Control Section(string title, string body, params Control[] content) =>
        new StackPanel()
            .Spacing(12)
            .Children(
                new MavLabel(title, MavTypeRole.HeadlineSmall),
                new MavLabel(body, MavTypeRole.BodyMedium).TextWrapping(TextWrapping.Wrap),
                Card(Column(content)));

    /// <summary>An outlined card with the given body.</summary>
    public static Control Card(Control body) =>
        new MavCard(MavCardVariant.Outlined, body).CardPadding(16);

    /// <summary>Vertical stack with 10 dp spacing.</summary>
    public static Control Column(params Control[] content) =>
        new StackPanel().Spacing(10).Children(content);

    /// <summary>Wrapping row for controls of different widths.</summary>
    public static Control Row(params Control[] content) =>
        new WrapPanel().Children(content);
}
