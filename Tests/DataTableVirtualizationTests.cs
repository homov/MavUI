using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Primitives;
using Xunit;

namespace Md3.Avalonia.Tests;

public class DataTableVirtualizationTests
{
    private sealed class Row
    {
        public Row(int index, string name)
        {
            Index = index;
            Name = name;
        }

        public int Index { get; }
        public string Name { get; }
    }

    private sealed class ObservableRow : INotifyPropertyChanged
    {
        private string _name;

        public ObservableRow(int index, string name)
        {
            Index = index;
            _name = name;
        }

        public int Index { get; }

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                    return;
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private static (Window Window, MdDataTable Table, MdScrollViewer Scroll) CreateHost(
        int count,
        bool pagination,
        double maxBodyHeight = 300,
        bool selectable = false)
    {
        var rows = Enumerable.Range(0, count).Select(i => new Row(i, $"Row {i}")).ToList();
        var table = new MdDataTable
        {
            ItemsSource = rows,
            ShowPagination = pagination,
            MaxBodyHeight = maxBodyHeight,
            Layout = MdDataTableLayout.Table,
            IsSelectable = selectable,
        };
        table.AddColumn("Index", r => ((Row)r).Index, isNumeric: true);
        table.AddColumn("Name", r => ((Row)r).Name);

        var window = new Window { Width = 500, Height = 400, Content = table };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var scroll = table.GetVisualDescendants().OfType<MdScrollViewer>().First();
        return (window, table, scroll);
    }

    private static int RowCount(MdDataTable table) =>
        table.GetVisualDescendants()
            .OfType<MdInkSurface>()
            .Count(s => s.Content is Grid); // rows host a Grid; icon buttons host an MdIcon

    [AvaloniaFact]
    public void Large_table_renders_a_chunk_instead_of_every_row()
    {
        var (window, table, _) = CreateHost(1000, pagination: false);

        var rendered = RowCount(table);
        Assert.True(rendered > 0, "no rows rendered");
        Assert.True(rendered < 300, $"rendered {rendered} rows up front");

        window.Close();
    }

    [AvaloniaFact]
    public void Scrolling_the_body_appends_more_rows()
    {
        var (window, table, scroll) = CreateHost(1000, pagination: false);
        var before = RowCount(table);

        scroll.Offset = new Vector(0, Math.Max(0, scroll.Extent.Height - scroll.Viewport.Height));
        Dispatcher.UIThread.RunJobs();

        Assert.True(RowCount(table) > before, "expected the next chunk to be appended");

        window.Close();
    }

    [AvaloniaFact]
    public void Selecting_a_row_does_not_rebuild_the_body_or_reset_scroll()
    {
        var (window, table, scroll) = CreateHost(1000, pagination: false, selectable: true);
        scroll.Offset = new Vector(0, 20000);
        Dispatcher.UIThread.RunJobs();
        var offset = scroll.Offset;

        var rowSurface = table.GetVisualDescendants()
            .OfType<MdInkSurface>()
            .First(surface => surface.Content is Grid);

        table.SelectedItem = ((System.Collections.IEnumerable)table.ItemsSource!).Cast<object>().ElementAt(300);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(offset, scroll.Offset);
        Assert.Contains(rowSurface, table.GetVisualDescendants());
        Assert.Single(table.SelectedItems);

        window.Close();
    }

    [AvaloniaFact]
    public void Paginated_table_renders_its_page()
    {
        var (window, table, _) = CreateHost(1000, pagination: true);

        // Default PageSize is 5.
        Assert.Equal(5, RowCount(table));

        window.Close();
    }

    [AvaloniaFact]
    public void Table_without_inner_viewport_renders_all_page_rows()
    {
        var rows = Enumerable.Range(0, 40).Select(i => new Row(i, $"Row {i}")).ToList();
        var table = new MdDataTable
        {
            ItemsSource = rows,
            ShowPagination = false,
            Layout = MdDataTableLayout.Table,
        };
        table.AddColumn("Name", r => ((Row)r).Name);

        var window = new Window { Width = 500, Height = 800, Content = table };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(40, RowCount(table));

        window.Close();
    }

    private static (Window Window, MdDataTable Table, MdScrollViewer Scroll, ObservableCollection<Row> Rows)
        CreateMutableHost(int count)
    {
        var rows = new ObservableCollection<Row>(
            Enumerable.Range(0, count).Select(i => new Row(i, $"Row {i}")));
        var table = new MdDataTable
        {
            ItemsSource = rows,
            ShowPagination = false,
            MaxBodyHeight = 300,
            Layout = MdDataTableLayout.Table,
        };
        table.AddColumn("Index", r => ((Row)r).Index, isNumeric: true);
        table.AddColumn("Name", r => ((Row)r).Name);

        var window = new Window { Width = 500, Height = 400, Content = table };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var scroll = table.GetVisualDescendants().OfType<MdScrollViewer>().First();
        return (window, table, scroll, rows);
    }

    private static HashSet<MdInkSurface> RowSurfaces(MdDataTable table) =>
        table.GetVisualDescendants()
            .OfType<MdInkSurface>()
            .Where(s => s.Content is Grid)
            .ToHashSet();

    [AvaloniaFact]
    public void Adding_a_row_keeps_existing_row_controls_and_scroll()
    {
        var (window, table, scroll, rows) = CreateMutableHost(100);
        scroll.Offset = new Vector(0, 1200);
        Dispatcher.UIThread.RunJobs();
        var offset = scroll.Offset;
        var before = RowSurfaces(table);
        Assert.Equal(100, before.Count);

        rows.Add(new Row(1000, "Added"));
        Dispatcher.UIThread.RunJobs();

        var after = RowSurfaces(table);
        Assert.Equal(101, after.Count);
        Assert.All(before, surface => Assert.Contains(surface, after));
        Assert.Equal(offset.Y, scroll.Offset.Y);

        window.Close();
    }

    [AvaloniaFact]
    public void Replacing_a_row_rebuilds_only_that_row_and_keeps_scroll()
    {
        var (window, table, scroll, rows) = CreateMutableHost(100);
        scroll.Offset = new Vector(0, 1200);
        Dispatcher.UIThread.RunJobs();
        var offset = scroll.Offset;
        var before = RowSurfaces(table);

        rows[5] = new Row(500, "Changed");
        Dispatcher.UIThread.RunJobs();

        var after = RowSurfaces(table);
        Assert.Equal(100, after.Count);
        Assert.Equal(99, after.Intersect(before).Count());
        Assert.Single(after.Except(before));
        Assert.Equal(offset.Y, scroll.Offset.Y);

        window.Close();
    }

    [AvaloniaFact]
    public void Large_table_incremental_replace_keeps_rendered_controls_and_scroll()
    {
        var (window, table, scroll, rows) = CreateMutableHost(1000);
        scroll.Offset = new Vector(0, 5000);
        Dispatcher.UIThread.RunJobs();
        var offset = scroll.Offset;
        var before = RowSurfaces(table);
        Assert.True(before.Count >= 100, $"only {before.Count} rows rendered");

        rows[0] = new Row(5000, "Changed");
        Dispatcher.UIThread.RunJobs();

        var after = RowSurfaces(table);
        Assert.Equal(before.Count, after.Count);
        Assert.Equal(before.Count - 1, after.Intersect(before).Count());
        Assert.Single(after.Except(before));
        Assert.Equal(offset.Y, scroll.Offset.Y);

        window.Close();
    }

    [AvaloniaFact]
    public void Item_property_change_updates_only_the_affected_rendered_row()
    {
        var rows = new ObservableCollection<ObservableRow>(
            Enumerable.Range(0, 100).Select(i => new ObservableRow(i, $"Row {i}")));
        var table = new MdDataTable
        {
            ItemsSource = rows,
            ShowPagination = false,
            MaxBodyHeight = 300,
            Layout = MdDataTableLayout.Table,
        };
        table.AddColumn("Index", r => ((ObservableRow)r).Index, isNumeric: true);
        table.AddColumn("Name", r => ((ObservableRow)r).Name);

        var window = new Window { Width = 500, Height = 400, Content = table };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var scroll = table.GetVisualDescendants().OfType<MdScrollViewer>().First();
        scroll.Offset = new Vector(0, 1200);
        Dispatcher.UIThread.RunJobs();
        var offset = scroll.Offset;
        var before = RowSurfaces(table);

        rows[5].Name = "Changed";
        Dispatcher.UIThread.RunJobs();

        var after = RowSurfaces(table);
        Assert.Equal(before.Count, after.Count);
        Assert.Equal(before.Count - 1, after.Intersect(before).Count());
        Assert.Single(after.Except(before));
        Assert.Contains(
            "Changed",
            table.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text));
        Assert.Equal(offset.Y, scroll.Offset.Y);

        window.Close();
    }

    [AvaloniaFact]
    public void Appending_without_an_inner_viewport_keeps_existing_rows()
    {
        var rows = new ObservableCollection<Row>(
            Enumerable.Range(0, 20).Select(i => new Row(i, $"Row {i}")));
        var table = new MdDataTable
        {
            ItemsSource = rows,
            ShowPagination = false,
            Layout = MdDataTableLayout.Table,
        };
        table.AddColumn("Name", r => ((Row)r).Name);

        var window = new Window { Width = 500, Height = 800, Content = table };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var before = RowSurfaces(table);
        Assert.Equal(20, before.Count);

        rows.Add(new Row(20, "Added"));
        Dispatcher.UIThread.RunJobs();

        var after = RowSurfaces(table);
        Assert.Equal(21, after.Count);
        Assert.All(before, surface => Assert.Contains(surface, after));

        window.Close();
    }

    [AvaloniaFact]
    public void Removing_and_moving_rows_reuse_unaffected_controls()
    {
        var (window, table, scroll, rows) = CreateMutableHost(100);
        scroll.Offset = new Vector(0, 1200);
        Dispatcher.UIThread.RunJobs();
        var offset = scroll.Offset;
        var before = RowSurfaces(table);

        rows.RemoveAt(3);
        Dispatcher.UIThread.RunJobs();

        var afterRemove = RowSurfaces(table);
        Assert.Equal(99, afterRemove.Count);
        Assert.Equal(99, afterRemove.Intersect(before).Count());
        Assert.Equal(offset.Y, scroll.Offset.Y);

        rows.Move(0, 10);
        Dispatcher.UIThread.RunJobs();

        var afterMove = RowSurfaces(table);
        Assert.Equal(99, afterMove.Count);
        Assert.All(afterRemove, surface => Assert.Contains(surface, afterMove));
        Assert.Equal(offset.Y, scroll.Offset.Y);

        window.Close();
    }
}
