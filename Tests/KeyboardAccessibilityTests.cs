using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Primitives;
using Xunit;

namespace Md3.Avalonia.Tests;

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

    private static MdInkSurface SurfaceOf(Control control) =>
        control.GetVisualDescendants().OfType<MdInkSurface>().First();

    [AvaloniaTheory]
    [InlineData(Key.Space)]
    [InlineData(Key.Enter)]
    public void MdButton_activates_from_the_keyboard(Key key)
    {
        var button = new MdButton("Save");
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
    public void MdFab_activates_from_the_keyboard(Key key)
    {
        var fab = new MdFab(MdIcons.Add);
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
    public void MdCheckBox_toggles_from_the_keyboard(Key key)
    {
        var check = new MdCheckBox("Remember me");
        var window = Show(check);

        check.Focus();
        PressKey(check, key);

        Assert.True(check.IsChecked);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(Key.Space)]
    [InlineData(Key.Enter)]
    public void MdSwitch_toggles_from_the_keyboard(Key key)
    {
        var toggle = new MdSwitch();
        var window = Show(toggle);

        toggle.Focus();
        PressKey(toggle, key);

        Assert.True(toggle.IsToggled);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(Key.Space)]
    [InlineData(Key.Enter)]
    public void MdRadioButton_selects_from_the_keyboard(Key key)
    {
        var radio = new MdRadioButton { Label = "Admin" };
        var window = Show(radio);

        radio.Focus();
        PressKey(radio, key);

        Assert.True(radio.IsChecked);
        window.Close();
    }

    [AvaloniaFact]
    public void MdSlider_steps_with_arrow_keys()
    {
        var slider = new MdSlider(0, 100, 50, divisions: 0) { Step = 10 };
        var window = Show(slider);

        slider.Focus();
        PressKey(slider, Key.Right);
        Assert.Equal(60, slider.Value);

        PressKey(slider, Key.Left);
        Assert.Equal(50, slider.Value);

        window.Close();
    }
}
