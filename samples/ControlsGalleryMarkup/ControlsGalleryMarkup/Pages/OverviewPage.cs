using Avalonia.Controls;
using Avalonia.Layout;
using Mav.Controls;

namespace Mav.ControlsGalleryMarkup.Pages;

public static class OverviewPage
{
    public static Control Build() =>
        new StackPanel()
            .Spacing(12)
            .Children(
                new TextBlock().Text("Overview").FontSize(24),
                new TextBlock().Text("Button variants"),
                new StackPanel()
                    .Orientation(Orientation.Horizontal)
                    .Spacing(8)
                    .Children(
                        new MavButton("Filled", MavButtonVariant.Filled),
                        new MavButton("Tonal", MavButtonVariant.Tonal),
                        new MavButton("Outlined", MavButtonVariant.Outlined),
                        new MavButton("Text", MavButtonVariant.Text)));
}
