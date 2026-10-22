using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Mav.Controls;
using Mav.Theming;
using Xunit;

namespace Mav.Tests;

public class WindowTests
{
    [AvaloniaFact]
    public void MavWindow_paints_the_theme_surface_by_default()
    {
        var window = new MavWindow();

        var brush = Assert.IsType<SolidColorBrush>(window.Background);
        Assert.Equal(MavTheme.Current.Color.Surface, brush.Color);
    }

    [AvaloniaFact]
    public void MavWindow_keeps_a_background_chosen_by_the_caller()
    {
        var window = new MavWindow { Background = Brushes.Red };

        Assert.Same(Brushes.Red, window.Background);
    }
}
