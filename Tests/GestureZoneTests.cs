using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Platform;
using Xunit;

namespace Md3.Avalonia.Tests;

public class GestureZoneTests
{
    private static Window Show(Control content, double width = 400, double height = 300)
    {
        var window = new Window { Width = width, Height = height, Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaFact]
    public void System_edge_detection_covers_both_sides()
    {
        var host = new Border();
        var window = Show(host);

        Assert.True(MdGestureZones.IsInSystemEdge(host, new Point(4, 100)));
        Assert.False(MdGestureZones.IsInSystemEdge(host, new Point(200, 100)));
        Assert.True(MdGestureZones.IsInSystemEdge(host, new Point(396, 100)));

        window.Close();
    }

    [AvaloniaFact]
    public void Custom_edge_width_is_respected()
    {
        var host = new Border();
        var window = Show(host);

        Assert.False(MdGestureZones.IsInSystemEdge(host, new Point(30, 100), 20));
        Assert.True(MdGestureZones.IsInSystemEdge(host, new Point(30, 100), 40));

        window.Close();
    }

    [AvaloniaFact]
    public void Slider_ignores_touch_starting_in_the_system_edge()
    {
        var slider = new MdSlider(0, 100, 10);
        var window = Show(slider);

        var edge = slider.TranslatePoint(new Point(4, 24), window)!.Value;
        using (var touch = window.TouchBegin(edge, RawInputModifiers.None))
            window.TouchEnd(touch, edge, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(10, slider.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void Slider_accepts_touch_away_from_the_edge()
    {
        var slider = new MdSlider(0, 100, 10);
        var window = Show(slider);

        var center = slider.TranslatePoint(new Point(200, 24), window)!.Value;
        using (var touch = window.TouchBegin(center, RawInputModifiers.None))
            window.TouchEnd(touch, center, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.True(slider.Value > 20, $"slider value {slider.Value}");
        window.Close();
    }

    [AvaloniaFact]
    public void Carousel_ignores_a_drag_starting_in_the_system_edge()
    {
        var carousel = new MdCarousel { Width = 400, Height = 210 };
        carousel.AddItem("One", "1", MdIcons.Home);
        carousel.AddItem("Two", "2", MdIcons.Search);
        carousel.AddItem("Three", "3", MdIcons.Settings);
        var window = Show(carousel);

        var start = carousel.TranslatePoint(new Point(396, 100), window)!.Value;
        var end = carousel.TranslatePoint(new Point(250, 100), window)!.Value;
        using (var touch = window.TouchBegin(start, RawInputModifiers.None))
        {
            window.TouchMove(touch, end, RawInputModifiers.None);
            window.TouchEnd(touch, end, RawInputModifiers.None);
        }
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, carousel.SelectedIndex);
        window.Close();
    }

    [AvaloniaFact]
    public void Carousel_accepts_a_drag_away_from_the_edge()
    {
        var carousel = new MdCarousel { Width = 400, Height = 210 };
        carousel.AddItem("One", "1", MdIcons.Home);
        carousel.AddItem("Two", "2", MdIcons.Search);
        carousel.AddItem("Three", "3", MdIcons.Settings);
        var window = Show(carousel);

        var start = carousel.TranslatePoint(new Point(250, 100), window)!.Value;
        var end = carousel.TranslatePoint(new Point(50, 100), window)!.Value;
        using (var touch = window.TouchBegin(start, RawInputModifiers.None))
        {
            window.TouchMove(touch, end, RawInputModifiers.None);
            window.TouchEnd(touch, end, RawInputModifiers.None);
        }
        Dispatcher.UIThread.RunJobs();

        Assert.True(carousel.SelectedIndex > 0, $"selected {carousel.SelectedIndex}");
        window.Close();
    }
}
