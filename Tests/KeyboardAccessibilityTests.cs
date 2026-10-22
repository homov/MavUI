using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Mav;
using Mav.Controls;
using Mav.Primitives;
using Xunit;

namespace Mav.Tests;

public class KeyboardAccessibilityTests
{
    private static Window Show(Control content)
    {
        var window = new Window { Width = 400, Height = 300, Content = content };
        window.Show();
        return window;
    }

    private static void PressKey(InputElement target, Key key)
    {
        target.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = key,
        });
    }

    private static MavInkSurface SurfaceOf(Control control) =>
        control.GetVisualDescendants().OfType<MavInkSurface>().First();

    [AvaloniaTheory]
    [InlineData(Key.Space)]
    [InlineData(Key.Enter)]
    public void MavButton_activates_from_the_keyboard(Key key)
    {
        var button = new MavButton("Save");
        var clicks = 0;
        button.Clicked += (_, _) => clicks++;
        var window = Show(button);

        var surface = SurfaceOf(button);
        surface.Focus();
        PressKey(surface, key);

        Assert.Equal(1, clicks);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(Key.Space)]
    [InlineData(Key.Enter)]
    public void MavFab_activates_from_the_keyboard(Key key)
    {
        var fab = new MavFab(MavIcons.Add);
        var clicks = 0;
        fab.Clicked += (_, _) => clicks++;
        var window = Show(fab);

        var surface = SurfaceOf(fab);
        surface.Focus();
        PressKey(surface, key);

        Assert.Equal(1, clicks);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(Key.Space)]
    [InlineData(Key.Enter)]
    public void MavCheckBox_toggles_from_the_keyboard(Key key)
    {
        var check = new MavCheckBox("Remember me");
        var window = Show(check);

        check.Focus();
        PressKey(check, key);

        Assert.True(check.IsChecked);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(Key.Space)]
    [InlineData(Key.Enter)]
    public void MavSwitch_toggles_from_the_keyboard(Key key)
    {
        var toggle = new MavSwitch();
        var window = Show(toggle);

        toggle.Focus();
        PressKey(toggle, key);

        Assert.True(toggle.IsToggled);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(Key.Space)]
    [InlineData(Key.Enter)]
    public void MavRadioButton_selects_from_the_keyboard(Key key)
    {
        var radio = new MavRadioButton { Label = "Admin" };
        var window = Show(radio);

        radio.Focus();
        PressKey(radio, key);

        Assert.True(radio.IsChecked);
        window.Close();
    }

    [AvaloniaFact]
    public void MavSlider_steps_with_arrow_keys()
    {
        var slider = new MavSlider(0, 100, 50, divisions: 0) { Step = 10 };
        var window = Show(slider);

        slider.Focus();
        PressKey(slider, Key.Right);
        Assert.Equal(60, slider.Value);

        PressKey(slider, Key.Left);
        Assert.Equal(50, slider.Value);

        window.Close();
    }
}
