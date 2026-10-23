using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Mav.Controls;
using Xunit;

namespace Mav.Tests;

/// <summary>
/// MavItems replaces Avalonia's ItemsControl in a theme-free app: a templated control without a
/// template draws nothing, which is exactly how the gallery lost its colour swatches, type scale,
/// shape samples, contrast list and palette buttons.
/// </summary>
public class ItemsHostTests
{
    private static IDataTemplate Template() =>
        new FuncDataTemplate<string>((item, _) => new TextBlock { Text = item });

    [AvaloniaFact]
    public void Builds_one_child_per_item_with_the_template()
    {
        var items = new MavItems
        {
            ItemTemplate = Template(),
            ItemsSource = new[] { "one", "two", "three" },
        };

        Assert.Equal(3, items.Children.Count);
        Assert.Equal(new[] { "one", "two", "three" },
            items.Children.Cast<TextBlock>().Select(text => text.Text).ToArray());
        Assert.Equal("two", items.Children[1].DataContext);
    }

    [AvaloniaFact]
    public void Rebuilds_when_the_collection_changes()
    {
        var source = new ObservableCollection<string> { "one" };
        var items = new MavItems { ItemTemplate = Template(), ItemsSource = source };
        Assert.Single(items.Children);

        source.Add("two");
        Assert.Equal(2, items.Children.Count);

        source.Clear();
        Assert.Empty(items.Children);
    }

    [AvaloniaFact]
    public void Accepts_controls_and_plain_objects_without_a_template()
    {
        var button = new Button { Content = "b" };
        var items = new MavItems { ItemsSource = new object[] { button, 42 } };

        Assert.Equal(2, items.Children.Count);
        Assert.Same(button, items.Children[0]);
        Assert.Equal("42", ((TextBlock)items.Children[1]).Text);
    }

    [AvaloniaFact]
    public void Keeps_children_added_by_hand_when_there_is_no_source()
    {
        var items = new MavItems();
        items.Children.Add(new TextBlock { Text = "static" });
        Assert.Single(items.Children);
    }

    [AvaloniaFact]
    public void Wraps_items_at_the_available_width()
    {
        var items = new MavItems
        {
            Wrap = true,
            ItemTemplate = new FuncDataTemplate<string>((_, _) => new Border { Width = 100, Height = 20 }),
            ItemsSource = Enumerable.Range(0, 5).Select(i => i.ToString()).ToArray(),
        };
        var window = new Window { Width = 250, Content = items };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // 100 px items in a 250 px box: two per row, three rows, 20 px each.
        Assert.Equal(200, items.Children[0].Bounds.Y);
        Assert.Equal(100, items.Children[1].Bounds.X);
        Assert.Equal(250, items.Children[2].Bounds.X);
        window.Close();
    }
}
