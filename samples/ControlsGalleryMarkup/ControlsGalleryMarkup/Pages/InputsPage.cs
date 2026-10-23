using Avalonia.Controls;
using Mav.Controls;

namespace Mav.ControlsGalleryMarkup.Pages;

public static class InputsPage
{
    public static Control Build() =>
        new StackPanel()
            .Spacing(12)
            .Children(
                new TextBlock().Text("Inputs").FontSize(24),
                new MavTextField("Name", placeholder: "Your name"),
                new MavTextField("Email", placeholder: "name@example.com"),
                new MavSwitch(isToggled: true));
}
