using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Mav.Controls;
using Mav.Markup;
using Xunit;

namespace Mav.Tests;

public class MarkupTests
{
    [AvaloniaFact]
    public void Generated_property_setter_matches_clr_property()
    {
        var viaMarkup = new MavButton().WithText("Save").WithWidth(120);
        var viaProperties = new MavButton { Text = "Save", Width = 120 };

        Assert.Equal(viaProperties.Text, viaMarkup.Text);
        Assert.Equal(120, viaMarkup.Width);
    }

    [AvaloniaFact]
    public void Inherited_property_setter_comes_from_base_markup()
    {
        var button = new MavButton().WithIsEnabled(false);

        Assert.False(button.IsEnabled);
    }

    [AvaloniaFact]
    public void Generated_event_setter_subscribes_to_the_event()
    {
        var button = new MavButton();
        var raised = 0;
        button.WithClicked((sender, _) =>
        {
            Assert.Same(button, sender);
            raised++;
        });

        RaiseClicked(button);

        Assert.Equal(1, raised);
    }

    [AvaloniaFact]
    public void WithName_sets_the_name()
    {
        var border = new Border().WithName("frame");

        Assert.Equal("frame", border.Name);
    }

    [AvaloniaFact]
    public void WithChildren_adds_children_in_order()
    {
        var first = new Border();
        var second = new Border();
        var panel = new StackPanel().WithChildren(first, second);

        Assert.Equal(2, panel.Children.Count);
        Assert.Same(first, panel.Children[0]);
        Assert.Same(second, panel.Children[1]);
    }

    [AvaloniaFact]
    public void WithContent_sets_content()
    {
        var control = new ContentControl().WithContent("hello");

        Assert.Equal("hello", control.Content);
    }

    [AvaloniaFact]
    public void WithChild_sets_decorator_child()
    {
        var child = new Border();
        var border = new Border().WithChild(child);

        Assert.Same(child, border.Child);
    }

    [AvaloniaFact]
    public void WithItems_adds_items()
    {
        var control = new ItemsControl().WithItems(new object[] { "a", "b" });

        Assert.Equal(2, control.ItemCount);
    }

    [AvaloniaFact]
    public void WithProperty_sets_any_styled_property()
    {
        var button = new MavButton().WithProperty(Layoutable.WidthProperty, 64.0);

        Assert.Equal(64, button.Width);
    }

    /// <summary>Raises the field-like <c>Clicked</c> event of <see cref="MavButton"/> from the test.</summary>
    private static void RaiseClicked(MavButton button)
    {
        var field = typeof(MavButton).GetField("Clicked", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        var handler = (Delegate?)field!.GetValue(button);
        handler?.DynamicInvoke(button, EventArgs.Empty);
    }
}
