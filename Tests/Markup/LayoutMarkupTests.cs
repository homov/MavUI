using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Mav.Markup;
using Xunit;

namespace Mav.Tests;

public class LayoutMarkupTests
{
    [AvaloniaFact]
    public void GridDefinitionsFromStringMatchSizes()
    {
        var grid = new Grid().RowDefinitions("Auto,*,2*").ColumnDefinitions("100,*");

        Assert.Equal(3, grid.RowDefinitions.Count);
        Assert.Equal(GridUnitType.Auto, grid.RowDefinitions[0].Height.GridUnitType);
        Assert.Equal(GridUnitType.Star, grid.RowDefinitions[1].Height.GridUnitType);
        Assert.Equal(2, grid.RowDefinitions[2].Height.Value);
        Assert.Equal(2, grid.ColumnDefinitions.Count);
        Assert.Equal(100, grid.ColumnDefinitions[0].Width.Value);
        Assert.Equal(GridUnitType.Pixel, grid.ColumnDefinitions[0].Width.GridUnitType);
    }

    [AvaloniaFact]
    public void GridAttachedPropertiesAreSetOnChild()
    {
        var child = new Border().Row(1).Column(2).RowSpan(2);
        var grid = new Grid().Children(child);

        Assert.Equal(1, Grid.GetRow(child));
        Assert.Equal(2, Grid.GetColumn(child));
        Assert.Equal(2, Grid.GetRowSpan(child));
        Assert.Same(child, grid.Children[0]);
    }

    [AvaloniaFact]
    public void CanvasAttachedPropertiesAreSetOnChild()
    {
        var child = new Border().Left(10).Top(20);

        new Canvas().Children(child);

        Assert.Equal(10, Canvas.GetLeft(child));
        Assert.Equal(20, Canvas.GetTop(child));
    }

    [AvaloniaFact]
    public void DockPanelAttachedPropertyIsSetOnChild()
    {
        var child = new Border().Dock(Dock.Top);

        new DockPanel().LastChildFill(true).Children(child);

        Assert.Equal(Dock.Top, DockPanel.GetDock(child));
    }

    [AvaloniaFact]
    public void StackPanelPropertiesAreSet()
    {
        var panel = new StackPanel().Orientation(Orientation.Horizontal).Spacing(4);

        Assert.Equal(Orientation.Horizontal, panel.Orientation);
        Assert.Equal(4, panel.Spacing);
    }

    [AvaloniaFact]
    public void TextBlockTextIsSet()
    {
        var text = new TextBlock().Text("Hello");

        Assert.Equal("Hello", text.Text);
    }
}
