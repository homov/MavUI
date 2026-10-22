using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mav.Controls;
using Xunit;

namespace Mav.Tests;

public class DataTableCompactTests
{
    private sealed class Row
    {
        public Row(int score, string name)
        {
            Score = score;
            Name = name;
        }

        public int Score { get; }
        public string Name { get; }
    }

    private static (Window Window, MavDataTable Table) CreateHost(
        int count,
        double width,
        MavDataTableLayout layout = MavDataTableLayout.Auto)
    {
        var rows = Enumerable.Range(0, count)
            .Select(i => new Row(i, $"Row {i}"))
            .ToList();
        var table = new MavDataTable
        {
            ItemsSource = rows,
            Layout = layout,
        };
        table.AddColumn("Name", r => ((Row)r).Name);
        table.AddColumn("Score", r => ((Row)r).Score, isNumeric: true);

        var window = new Window { Width = width, Height = 700, Content = table };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, table);
    }

    private static int CardCount(MavDataTable table) =>
        table.GetVisualDescendants().OfType<MavCard>().Count();

    [AvaloniaFact]
    public void Auto_uses_cards_on_a_phone_width()
    {
        var (window, table) = CreateHost(20, width: 360);

        Assert.True(table.IsCompact);
        Assert.Equal(MavDataTableLayout.Cards, table.EffectiveLayout);
        Assert.Equal(5, CardCount(table)); // default page size

        window.Close();
    }

    [AvaloniaFact]
    public void Auto_uses_the_table_on_a_desktop_width()
    {
        var (window, table) = CreateHost(20, width: 900);

        Assert.False(table.IsCompact);
        Assert.Equal(MavDataTableLayout.Table, table.EffectiveLayout);
        Assert.Equal(0, CardCount(table));

        window.Close();
    }

    [AvaloniaFact]
    public void Layout_overrides_the_breakpoint()
    {
        var (wideWindow, wideTable) = CreateHost(10, width: 900, MavDataTableLayout.Cards);
        Assert.True(wideTable.IsCompact);
        Assert.Equal(5, CardCount(wideTable));
        wideWindow.Close();

        var (narrowWindow, narrowTable) = CreateHost(10, width: 360, MavDataTableLayout.Table);
        Assert.False(narrowTable.IsCompact);
        Assert.Equal(0, CardCount(narrowTable));
        narrowWindow.Close();
    }

    [AvaloniaFact]
    public void Resizing_across_the_breakpoint_switches_layout()
    {
        var (window, table) = CreateHost(10, width: 900);
        Assert.Equal(MavDataTableLayout.Table, table.EffectiveLayout);

        window.Width = 400;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(MavDataTableLayout.Cards, table.EffectiveLayout);
        Assert.True(CardCount(table) > 0);

        window.Close();
    }

    [AvaloniaFact]
    public void Card_mode_offers_sort_chips_for_sortable_columns()
    {
        var (window, table) = CreateHost(10, width: 360);

        var chips = table.GetVisualDescendants().OfType<MavChip>().ToList();
        Assert.Equal(2, chips.Count);
        Assert.Contains(chips, c => c.Text == "Name");
        Assert.Contains(chips, c => c.Text == "Score");

        window.Close();
    }

    [AvaloniaFact]
    public void Card_mode_shows_every_column_as_a_label_value_pair()
    {
        var (window, table) = CreateHost(10, width: 360);

        var texts = table.GetVisualDescendants()
            .OfType<TextBlock>()
            .Select(t => t.Text)
            .ToList();

        Assert.Contains("Row 0", texts);   // headline
        Assert.Contains("Name", texts);    // label
        Assert.Contains("Score", texts);   // label
        Assert.Contains("0", texts);       // score value

        window.Close();
    }

    [AvaloniaFact]
    public void Cards_expose_one_selection_checkbox_per_row()
    {
        var rows = Enumerable.Range(0, 20)
            .Select(i => new Row(i, $"Row {i}"))
            .ToList();
        var table = new MavDataTable
        {
            ItemsSource = rows,
            IsSelectable = true,
        };
        table.AddColumn("Name", r => ((Row)r).Name);
        table.AddColumn("Score", r => ((Row)r).Score, isNumeric: true);

        var window = new Window { Width = 360, Height = 700, Content = table };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(5, table.GetVisualDescendants().OfType<MavCheckBox>().Count());

        window.Close();
    }
}
