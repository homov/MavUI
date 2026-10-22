using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Md3.Avalonia.Controls;
using Xunit;

namespace Md3.Avalonia.Tests;

public class VirtualizationTests
{
    private static (Window Window, MdScrollViewer Scroll, MdList List) CreateHost(
        int count,
        double viewportHeight = 300)
    {
        var items = Enumerable.Range(0, count).Select(i => $"Item {i}").ToList();
        var list = new MdList { ItemsSource = items, ShowDividers = true };
        var scroll = new MdScrollViewer { Height = viewportHeight, Content = list };
        var window = new Window { Width = 400, Height = viewportHeight, Content = scroll };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, scroll, list);
    }

    private static int LogicalCount(MdList list) =>
        list.Children.OfType<MdListItem>().Count();

    [AvaloniaFact]
    public void Large_items_source_realizes_only_a_window()
    {
        var (window, _, list) = CreateHost(1000);

        Assert.True(LogicalCount(list) < 1000, $"realized {LogicalCount(list)} of 1000");
        Assert.True(LogicalCount(list) > 0, "nothing realized");
        Assert.True(list.Children.Count >= 2, "expected top/bottom spacers");

        window.Close();
    }

    [AvaloniaFact]
    public void Scrolling_realizes_the_later_items()
    {
        var (window, scroll, list) = CreateHost(1000);
        var before = list.Children.OfType<MdListItem>().Select(i => i.Headline).ToHashSet();

        scroll.Offset = new Vector(0, 20000);
        Dispatcher.UIThread.RunJobs();

        var after = list.Children.OfType<MdListItem>().Select(i => i.Headline).ToHashSet();
        Assert.True(after.Count > 0);
        Assert.NotEqual(before, after);

        var indices = after
            .Where(h => h is not null && h.StartsWith("Item "))
            .Select(h => int.TryParse(h!.Substring(5), out var n) ? n : -1)
            .Where(n => n >= 0)
            .ToList();
        Assert.NotEmpty(indices);
        Assert.True(indices.Max() > 250, "expected items from the scrolled region, got: " +
                                        string.Join(", ", after.Take(5)));

        window.Close();
    }

    [AvaloniaFact]
    public void Manual_items_are_all_realized()
    {
        var list = new MdList();
        for (var i = 0; i < 100; i++)
            list.AddItem(new MdListItem($"Row {i}"));

        Assert.Equal(100, list.Children.Count);
    }

    [AvaloniaFact]
    public void Virtualized_dividers_follow_logical_indices()
    {
        var (window, _, list) = CreateHost(10, viewportHeight: 2000);
        var items = list.Children.OfType<MdListItem>().ToList();

        Assert.Equal(10, items.Count);
        for (var i = 0; i < items.Count - 1; i++)
            Assert.True(items[i].ShowDivider, $"item {i} should have a divider");
        Assert.False(items[^1].ShowDivider, "the last logical item must not have a divider");

        window.Close();
    }

    [AvaloniaFact]
    public void Changing_items_source_rebuilds_the_window()
    {
        var (window, _, list) = CreateHost(500);
        Assert.True(LogicalCount(list) < 500);

        list.ItemsSource = Enumerable.Range(0, 30).Select(i => $"New {i}").ToList();
        Dispatcher.UIThread.RunJobs();

        var headlines = list.Children.OfType<MdListItem>().Select(i => i.Headline).ToList();
        Assert.Contains("New 0", headlines);
        Assert.DoesNotContain(headlines, h => h is not null && h.StartsWith("Item "));

        window.Close();
    }

    [AvaloniaFact]
    public void Small_items_source_is_realized_eagerly_without_a_scroll_host()
    {
        var list = new MdList
        {
            ItemsSource = new[] { "One", "Two", "Three" },
        };

        Assert.Equal(3, list.Children.Count);
        Assert.Contains(list.Children.OfType<MdListItem>(), i => i.Headline == "Two");
    }

    private static (Window Window, MdScrollViewer Scroll, MdList List, ObservableCollection<string> Items)
        CreateMutableHost(int count, double viewportHeight = 2000)
    {
        var items = new ObservableCollection<string>(
            Enumerable.Range(0, count).Select(i => $"Row {i}"));
        var list = new MdList
        {
            ItemFactory = item => new MdListItem((string)item),
        };
        var scroll = new MdScrollViewer { Height = viewportHeight, Content = list };
        var window = new Window { Width = 400, Height = viewportHeight, Content = scroll };
        window.Show();
        list.ItemsSource = items;
        Dispatcher.UIThread.RunJobs();
        return (window, scroll, list, items);
    }

    [AvaloniaFact]
    public void Adding_an_item_keeps_unaffected_controls()
    {
        var (window, _, list, items) = CreateMutableHost(6);
        var before = list.Children.OfType<MdListItem>().ToList();

        items.Add("Row 6");
        Dispatcher.UIThread.RunJobs();

        var after = list.Children.OfType<MdListItem>().ToList();
        Assert.Equal(7, after.Count);
        for (var i = 0; i < before.Count; i++)
            Assert.Same(before[i], after[i]);
        Assert.Equal("Row 6", after[^1].Headline);

        window.Close();
    }

    [AvaloniaFact]
    public void Removing_an_item_keeps_neighbours_and_reindexes_them()
    {
        var (window, _, list, items) = CreateMutableHost(6);
        var before = list.Children.OfType<MdListItem>().ToList();

        items.RemoveAt(2);
        Dispatcher.UIThread.RunJobs();

        var after = list.Children.OfType<MdListItem>().ToList();
        Assert.Equal(5, after.Count);
        Assert.Same(before[0], after[0]);
        Assert.Same(before[1], after[1]);
        Assert.Same(before[3], after[2]);
        Assert.Same(before[4], after[3]);
        Assert.Equal("Row 3", after[2].Headline);

        window.Close();
    }

    [AvaloniaFact]
    public void Replacing_an_item_rebuilds_only_that_control()
    {
        var (window, _, list, items) = CreateMutableHost(6);
        var before = list.Children.OfType<MdListItem>().ToList();

        items[1] = "Replaced";
        Dispatcher.UIThread.RunJobs();

        var after = list.Children.OfType<MdListItem>().ToList();
        Assert.Equal(6, after.Count);
        Assert.NotSame(before[1], after[1]);
        Assert.Equal("Replaced", after[1].Headline);
        Assert.Same(before[0], after[0]);
        Assert.Same(before[2], after[2]);
        Assert.Same(before[5], after[5]);

        window.Close();
    }

    [AvaloniaFact]
    public void Moving_an_item_moves_its_control_and_keeps_others()
    {
        var (window, _, list, items) = CreateMutableHost(6);
        var before = list.Children.OfType<MdListItem>().ToList();

        items.Move(1, 3);
        Dispatcher.UIThread.RunJobs();

        var after = list.Children.OfType<MdListItem>().ToList();
        Assert.Equal(6, after.Count);
        Assert.Equal("Row 0", after[0].Headline);
        Assert.Equal("Row 2", after[1].Headline);
        Assert.Equal("Row 3", after[2].Headline);
        Assert.Equal("Row 1", after[3].Headline);
        Assert.Same(before[1], after[3]);
        Assert.Same(before[2], after[1]);
        Assert.Same(before[3], after[2]);

        window.Close();
    }

    [AvaloniaFact]
    public void Bulk_appends_and_tail_removals_keep_the_height_index_in_sync()
    {
        var items = new ObservableCollection<string>();
        var list = new MdList
        {
            ItemFactory = item => new MdListItem((string)item),
            ShowDividers = true,
        };
        var scroll = new MdScrollViewer { Height = 300, Content = list };
        var window = new Window { Width = 400, Height = 300, Content = scroll };
        window.Show();
        list.ItemsSource = items;
        Dispatcher.UIThread.RunJobs();

        for (var i = 0; i < 500; i++)
            items.Add($"Row {i}");
        Dispatcher.UIThread.RunJobs();

        scroll.Offset = new Vector(0, 500 * 64);
        Dispatcher.UIThread.RunJobs();

        var headlines = list.Children.OfType<MdListItem>()
            .Select(i => i.Headline)
            .ToHashSet();
        Assert.Contains("Row 499", headlines);

        var realized = list.Children.OfType<MdListItem>().ToList();
        Assert.False(realized[^1].ShowDivider);
        Assert.True(realized[^2].ShowDivider);

        for (var i = 0; i < 100; i++)
            items.RemoveAt(items.Count - 1);
        Dispatcher.UIThread.RunJobs();

        scroll.Offset = new Vector(0, 400 * 64);
        Dispatcher.UIThread.RunJobs();

        headlines = list.Children.OfType<MdListItem>()
            .Select(i => i.Headline)
            .ToHashSet();
        Assert.Contains("Row 399", headlines);
        Assert.DoesNotContain("Row 499", headlines);

        window.Close();
    }

    [AvaloniaFact]
    public void Incremental_changes_preserve_the_scroll_offset()
    {
        var (window, scroll, _, items) = CreateMutableHost(200, viewportHeight: 300);
        scroll.Offset = new Vector(0, 2400);
        Dispatcher.UIThread.RunJobs();
        var offset = scroll.Offset;

        items.Add("Tail");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(offset.Y, scroll.Offset.Y);

        items.RemoveAt(0);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(offset.Y, scroll.Offset.Y);

        window.Close();
    }
}
