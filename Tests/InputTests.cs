using Avalonia.Headless.XUnit;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Theming;
using Xunit;

namespace Md3.Avalonia.Tests;

public class InputTests
{
    [AvaloniaFact]
    public void MdTextField_outlined_with_error()
    {
        var field = new MdTextField("Email", MdTextFieldVariant.Outlined, placeholder: "you@example.com")
        {
            Text = "wrong",
            ErrorText = "Invalid email",
            HasError = true,
        };
        Assert.Equal("wrong", field.Text);
        Assert.True(field.HasError);
    }

    [AvaloniaFact]
    public void MdTextArea_min_height()
    {
        var area = new MdTextArea("Notes", minHeight: 200);
        Assert.Equal(200, area.MinBoxHeight);
    }

    [AvaloniaFact]
    public void MdPinInput_numeric_done()
    {
        var pin = new MdPinInput { Length = 6, Code = "123456", IsMasked = true };
        Assert.Equal("123456", pin.Code);
    }

    [AvaloniaFact]
    public void MdNumberBox_range()
    {
        var box = new MdNumberBox { Label = "Age", Minimum = 0, Maximum = 120, Value = 25 };
        Assert.Equal(25, box.Value);
    }

    [AvaloniaFact]
    public void MdTagInput_add_remove()
    {
        var tags = new MdTagInput { Label = "Tags" };
        tags.AddTag("c#");
        Assert.Single(tags.Tags);
        tags.ClearTags();
        Assert.Empty(tags.Tags);
    }

    [AvaloniaFact]
    public void MdRatingBar_half_stars()
    {
        var rating = new MdRatingBar { MaxRating = 5, Value = 3.5, AllowHalf = true };
        Assert.Equal(3.5, rating.Value);
    }
}
