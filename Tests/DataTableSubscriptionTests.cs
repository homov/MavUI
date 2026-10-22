using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Mav.Controls;
using Xunit;

namespace Mav.Tests;

/// <summary>
/// Item-level PropertyChanged subscriptions in <see cref="MavDataTable"/> are reference-counted by hand
/// (item 1.3). These tests check that repeated add/remove cycles leave no subscriptions behind.
/// </summary>
public class DataTableSubscriptionTests
{
    private sealed class CountingRow : INotifyPropertyChanged
    {
        private PropertyChangedEventHandler? _handlers;

        public CountingRow(int index) => Index = index;

        public int Index { get; }

        // Number of live handlers attached to this item.
        public int Subscribers => _handlers?.GetInvocationList().Length ?? 0;

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => _handlers += value;
            remove => _handlers -= value;
        }
    }

    [AvaloniaFact]
    public void Add_and_remove_cycles_leave_no_item_subscriptions()
    {
        var rows = new ObservableCollection<CountingRow>(Enumerable.Range(0, 20).Select(i => new CountingRow(i)));
        var table = new MavDataTable { ItemsSource = rows, Layout = MavDataTableLayout.Table };
        table.AddColumn("Index", r => ((CountingRow)r).Index, isNumeric: true);

        var window = new Window { Width = 500, Height = 400, Content = table };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.All(rows, r => Assert.Equal(1, r.Subscribers));

        var created = rows.ToList();
        for (var cycle = 0; cycle < 30; cycle++)
        {
            for (var i = 0; i < 10; i++)
                rows.Add(new CountingRow(1000 + cycle * 10 + i));
            Dispatcher.UIThread.RunJobs();

            while (rows.Count > 20)
                rows.RemoveAt(rows.Count - 1);
            Dispatcher.UIThread.RunJobs();
        }

        // Only the 20 original rows are still in the collection; the rest must be unsubscribed.
        Assert.All(rows, r => Assert.Equal(1, r.Subscribers));
        foreach (var removed in created.Where(r => !rows.Contains(r)))
            Assert.Equal(0, removed.Subscribers);

        // Removing every row, then detaching the table, must also clear everything.
        var all = rows.ToList();
        rows.Clear();
        Dispatcher.UIThread.RunJobs();
        Assert.All(all, r => Assert.Equal(0, r.Subscribers));

        window.Content = null;
        window.Close();
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void Detaching_the_table_removes_every_item_subscription()
    {
        var rows = new ObservableCollection<CountingRow>(Enumerable.Range(0, 15).Select(i => new CountingRow(i)));
        var table = new MavDataTable { ItemsSource = rows, Layout = MavDataTableLayout.Table };
        table.AddColumn("Index", r => ((CountingRow)r).Index, isNumeric: true);

        var window = new Window { Width = 500, Height = 400, Content = table };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.All(rows, r => Assert.Equal(1, r.Subscribers));

        window.Content = null;
        Dispatcher.UIThread.RunJobs();
        Assert.All(rows, r => Assert.Equal(0, r.Subscribers));

        window.Close();
    }
}
