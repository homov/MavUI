using Avalonia.Headless.XUnit;
using Mav;
using Mav.Controls;
using Mav.Theming;
using Xunit;

namespace Mav.Tests;

public class InputTests
{
    [AvaloniaFact]
    public void MavTextField_outlined_with_error()
    {
        var field = new MavTextField("Email", MavTextFieldVariant.Outlined, placeholder: "you@example.com")
        {
            Text = "wrong",
            ErrorText = "Invalid email",
            HasError = true,
        };
        Assert.Equal("wrong", field.Text);
        Assert.True(field.HasError);
    }

    [AvaloniaFact]
    public void MavTextArea_min_height()
    {
        var area = new MavTextArea("Notes", minHeight: 200);
        Assert.Equal(200, area.MinBoxHeight);
    }

    [AvaloniaFact]
    public void MavPinInput_numeric_done()
    {
        var pin = new MavPinInput { Length = 6, Code = "123456", IsMasked = true };
        Assert.Equal("123456", pin.Code);
    }

    [AvaloniaFact]
    public void MavNumberBox_range()
    {
        var box = new MavNumberBox { Label = "Age", Minimum = 0, Maximum = 120, Value = 25 };
        Assert.Equal(25, box.Value);
    }

    [AvaloniaFact]
    public void MavTagInput_add_remove()
    {
        var tags = new MavTagInput { Label = "Tags" };
        tags.AddTag("c#");
        Assert.Single(tags.Tags);
        tags.ClearTags();
        Assert.Empty(tags.Tags);
    }

    [AvaloniaFact]
    public void MavRatingBar_half_stars()
    {
        var rating = new MavRatingBar { MaxRating = 5, Value = 3.5, AllowHalf = true };
        Assert.Equal(3.5, rating.Value);
    }
}
