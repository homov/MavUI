using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Theming;
using Xunit;

namespace Md3.Avalonia.Tests;

public class VirtualizationBenchmarks
{
    private readonly ITestOutputHelper _output;

    public VirtualizationBenchmarks(ITestOutputHelper output) => _output = output;

    [AvaloniaFact]
    public void Thousand_item_list_first_layout()
    {
        var items = Enumerable.Range(0, 1000).Select(i => $"Item {i}").ToList();
        var list = new MdList { ItemsSource = items, ShowDividers = true };
        var scroll = new MdScrollViewer { Height = 300, Content = list };
        var window = new Window { Width = 400, Height = 300, Content = scroll };

        var sw = Stopwatch.StartNew();
        window.Show();
        Dispatcher.UIThread.RunJobs();
        sw.Stop();

        var realized = list.Children.OfType<MdListItem>().Count();
        _output.WriteLine(
            $"MdList: 1000 items, viewport 300 -> {realized} realized, " +
            $"first layout {sw.Elapsed.TotalMilliseconds:F1} ms");

        Assert.True(realized < 100, $"expected virtualization, realized {realized}");
        Assert.True(sw.Elapsed.TotalSeconds < 5, "first layout took too long");

        window.Close();
    }

    [AvaloniaFact]
    public void Thousand_row_table_first_layout()
    {
        var rows = Enumerable.Range(0, 1000).Select(i => new { Index = i, Name = $"Row {i}" }).ToList();
        var table = new MdDataTable
        {
            ItemsSource = rows,
            ShowPagination = false,
            MaxBodyHeight = 300,
            Layout = MdDataTableLayout.Table,
        };
        table.AddColumn("Index", r => ((dynamic)r).Index, isNumeric: true);
        table.AddColumn("Name", r => ((dynamic)r).Name);

        var window = new Window { Width = 500, Height = 400, Content = table };

        var sw = Stopwatch.StartNew();
        window.Show();
        Dispatcher.UIThread.RunJobs();
        sw.Stop();

        var realized = table.GetVisualDescendants()
            .OfType<Md3.Avalonia.Primitives.MdInkSurface>()
            .Count(s => s.Content is Grid);
        _output.WriteLine(
            $"MdDataTable: 1000 rows, body 300 -> {realized} realized, " +
            $"first layout {sw.Elapsed.TotalMilliseconds:F1} ms");

        Assert.True(realized < 300, $"expected chunked rendering, realized {realized}");
        Assert.True(sw.Elapsed.TotalSeconds < 5, "first layout took too long");

        window.Close();
    }

    [AvaloniaFact]
    public void Five_thousand_bulk_appends_to_a_virtualized_list()
    {
        var items = new ObservableCollection<string>();
        var list = new MdList
        {
            ItemFactory = item => new MdListItem((string)item),
        };
        var scroll = new MdScrollViewer { Height = 300, Content = list };
        var window = new Window { Width = 400, Height = 300, Content = scroll };
        window.Show();
        list.ItemsSource = items;
        Dispatcher.UIThread.RunJobs();

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < 5000; i++)
            items.Add($"Row {i}");
        Dispatcher.UIThread.RunJobs();
        sw.Stop();

        var realized = list.Children.OfType<MdListItem>().Count();
        _output.WriteLine(
            $"MdList: 5000 incremental appends, viewport 300 -> {realized} realized, " +
            $"{sw.Elapsed.TotalMilliseconds:F1} ms");

        Assert.True(realized < 200, $"expected virtualization, realized {realized}");
        Assert.True(sw.Elapsed.TotalSeconds < 8, "5000 incremental appends took too long");

        window.Close();
    }

    [AvaloniaFact]
    public void Five_thousand_bulk_appends_to_a_plain_list()
    {
        var items = new ObservableCollection<string>();
        var list = new MdList
        {
            ItemFactory = item => new MdListItem((string)item),
        };
        var window = new Window { Width = 400, Height = 600, Content = list };
        window.Show();
        list.ItemsSource = items;
        Dispatcher.UIThread.RunJobs();

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < 5000; i++)
            items.Add($"Row {i}");
        Dispatcher.UIThread.RunJobs();
        sw.Stop();

        _output.WriteLine(
            $"MdList: 5000 plain incremental appends -> {list.Children.Count} children, " +
            $"{sw.Elapsed.TotalMilliseconds:F1} ms");

        Assert.Equal(5000, list.Children.Count);
        Assert.True(sw.Elapsed.TotalSeconds < 8, "5000 plain appends took too long");

        window.Close();
    }

    [AvaloniaFact]
    public void Text_field_typing_and_theme_switch_stay_fast()
    {
        var field = new MdTextField("Name")
        {
            MaxLength = 200,
            ShowCounter = true,
        };
        var area = new MdTextArea("Notes")
        {
            MaxLength = 200,
            ShowCounter = true,
        };
        var host = new StackPanel { Spacing = 16 };
        host.Children.Add(field);
        host.Children.Add(area);

        var window = new Window { Width = 420, Height = 500, Content = host };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < 2000; i++)
        {
            var text = new string('a', i % 40);
            field.Text = text;
            area.Text = text;
        }
        Dispatcher.UIThread.RunJobs();
        sw.Stop();

        _output.WriteLine($"Text fields: 2000 text updates -> {sw.Elapsed.TotalMilliseconds:F1} ms");
        Assert.True(sw.Elapsed.TotalSeconds < 8, "text updates took too long");

        field.Text = "hello";
        area.Text = "hello";
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(
            "5/200",
            field.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text));
        Assert.Contains(
            "5/200",
            area.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text));

        var originalMode = MdTheme.Mode;
        sw.Restart();
        for (var i = 0; i < 20; i++)
        {
            MdTheme.Mode = i % 2 == 0
                ? (originalMode == MdThemeMode.Dark ? MdThemeMode.Light : MdThemeMode.Dark)
                : originalMode;
        }
        Dispatcher.UIThread.RunJobs();
        sw.Stop();

        _output.WriteLine($"Text fields: 20 theme switches -> {sw.Elapsed.TotalMilliseconds:F1} ms");
        Assert.True(sw.Elapsed.TotalSeconds < 8, "theme switches took too long");

        MdTheme.Mode = originalMode;
        Dispatcher.UIThread.RunJobs();
        window.Close();
    }
}
