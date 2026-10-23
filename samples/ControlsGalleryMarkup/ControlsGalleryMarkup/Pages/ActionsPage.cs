using Avalonia.Controls;
using Mav.Controls;

namespace Mav.ControlsGalleryMarkup.Pages;

public static class ActionsPage
{
    public static Control Build()
    {
        var clicks = 0;
        var status = new TextBlock().Text("Not clicked yet");

        var increment = new MavButton("Increment")
            .OnClicked(() => status.Text = $"Clicked {++clicks} time(s)");

        return new StackPanel()
            .Spacing(12)
            .Children(
                new TextBlock().Text("Actions").FontSize(24),
                increment,
                status);
    }
}
