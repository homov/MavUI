using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
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

    /// <summary>
    /// A field filled in before it is shown (a prefilled sign-in form) used to keep its label at the
    /// rest position, drawn on top of the text, because the animation never ran without a frame clock.
    /// </summary>
    [AvaloniaFact]
    public void MavTextField_floats_the_label_when_text_is_set_before_showing()
    {
        var field = new MavTextField("Email", MavTextFieldVariant.Outlined) { Text = "user@example.test" };
        var window = new Window { Width = 390, Height = 200, Content = field };
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();

            var label = field.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.Text == "Email");
            Assert.NotNull(label);
            // Floated label: 12px, above the top border. At rest: 16px, on the text baseline.
            Assert.True(label!.FontSize < 13, $"The label stayed at {label.FontSize}px with text in the field.");
            Assert.True(label.Margin.Top < 0, $"The label stayed at y={label.Margin.Top} with text in the field.");
        }
        finally
        {
            window.Close();
        }
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
