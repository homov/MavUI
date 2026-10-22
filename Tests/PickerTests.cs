using Avalonia.Headless.XUnit;
using Mav;
using Mav.Controls;
using Mav.Theming;
using Xunit;

namespace Mav.Tests;

public class PickerTests
{
    [AvaloniaFact]
    public void MavDatePicker_selected()
    {
        var picker = new MavDatePicker { SelectedDate = new System.DateTime(2026, 10, 21) };
        Assert.NotNull(picker.SelectedDate);
    }

    [AvaloniaFact]
    public void MavDatePickerField_clear()
    {
        var f = new MavDatePickerField { Label = "Due", IsClearable = true, SelectedDate = System.DateTime.Today };
        f.Clear();
        Assert.Null(f.SelectedDate);
    }

    [AvaloniaFact]
    public void MavTimePicker_24h()
    {
        var picker = new MavTimePicker { SelectedTime = new System.TimeSpan(14, 30, 0), Is24Hour = true };
        Assert.Equal(14, picker.SelectedTime.Hours);
    }

    [AvaloniaFact]
    public void MavColorPicker_color()
    {
        var c = new MavColorPicker { SelectedColor = global::Avalonia.Media.Colors.Teal, ShowHexInput = true };
        Assert.Equal(global::Avalonia.Media.Colors.Teal, c.SelectedColor);
    }
}
