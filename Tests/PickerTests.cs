using Avalonia.Headless.XUnit;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Theming;
using Xunit;

namespace Md3.Avalonia.Tests;

public class PickerTests
{
    [AvaloniaFact]
    public void MdDatePicker_selected()
    {
        var picker = new MdDatePicker { SelectedDate = new System.DateTime(2026, 10, 21) };
        Assert.NotNull(picker.SelectedDate);
    }

    [AvaloniaFact]
    public void MdDatePickerField_clear()
    {
        var f = new MdDatePickerField { Label = "Due", IsClearable = true, SelectedDate = System.DateTime.Today };
        f.Clear();
        Assert.Null(f.SelectedDate);
    }

    [AvaloniaFact]
    public void MdTimePicker_24h()
    {
        var picker = new MdTimePicker { SelectedTime = new System.TimeSpan(14, 30, 0), Is24Hour = true };
        Assert.Equal(14, picker.SelectedTime.Hours);
    }

    [AvaloniaFact]
    public void MdColorPicker_color()
    {
        var c = new MdColorPicker { SelectedColor = global::Avalonia.Media.Colors.Teal, ShowHexInput = true };
        Assert.Equal(global::Avalonia.Media.Colors.Teal, c.SelectedColor);
    }
}
