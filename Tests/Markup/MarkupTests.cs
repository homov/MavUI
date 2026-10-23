using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Mav.Controls;
using Mav.Markup;
using Xunit;

namespace Mav.Tests;

public class MarkupTests
{
    [AvaloniaFact]
    public void GeneratedPropertySetterMatchesClrProperty()
    {
        var viaMarkup = new MavButton().Text("Save").Width(120);
        var viaProperties = new MavButton { Text = "Save", Width = 120 };

        Assert.Equal(viaProperties.Text, viaMarkup.Text);
        Assert.Equal(120, viaMarkup.Width);
    }

    [AvaloniaFact]
    public void InheritedPropertySetterComesFromBaseMarkup()
    {
        var button = new MavButton().IsEnabled(false);

        Assert.False(button.IsEnabled);
    }

    [AvaloniaFact]
    public void GeneratedEventSetterSubscribesToTheEvent()
    {
        var button = new MavButton();
        var raised = 0;
        button.OnClicked((sender, _) =>
        {
            Assert.Same(button, sender);
            raised++;
        });

        RaiseClicked(button);

        Assert.Equal(1, raised);
    }

    [AvaloniaFact]
    public void NameSetsTheName()
    {
        var border = new Border().Name("frame");

        Assert.Equal("frame", border.Name);
    }

    [AvaloniaFact]
    public void ChildrenAddsChildrenInOrder()
    {
        var first = new Border();
        var second = new Border();
        var panel = new StackPanel().Children(first, second);

        Assert.Equal(2, panel.Children.Count);
        Assert.Same(first, panel.Children[0]);
        Assert.Same(second, panel.Children[1]);
    }

    [AvaloniaFact]
    public void ContentSetsContent()
    {
        var control = new ContentControl().Content("hello");

        Assert.Equal("hello", control.Content);
    }

    [AvaloniaFact]
    public void ChildSetsDecoratorChild()
    {
        var child = new Border();
        var border = new Border().Child(child);

        Assert.Same(child, border.Child);
    }

    [AvaloniaFact]
    public void ItemsAddsItems()
    {
        var control = new ItemsControl().Items(new object[] { "a", "b" });

        Assert.Equal(2, control.ItemCount);
    }

    [AvaloniaFact]
    public void PropertySetsAnyStyledProperty()
    {
        var button = new MavButton().Property(Layoutable.WidthProperty, 64.0);

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
