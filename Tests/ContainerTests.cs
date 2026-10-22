using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;
using Xunit;

namespace Md3.Avalonia.Tests;

public class ContainerTests
{
    [AvaloniaFact]
    public void MdCard_filled()
    {
        var card = new MdCard(MdCardVariant.Filled, new MdLabel { Text = "Body" });
        Assert.Equal(MdCardVariant.Filled, card.Variant);
    }

    [AvaloniaFact]
    public void MdAvatar_initials()
    {
        var av = new MdAvatar { Initials = "AB" };
        Assert.Equal("AB", av.Initials);
    }

    [AvaloniaFact]
    public void MdBadge_count()
    {
        var badge = new MdBadge { Text = "3", Count = 3 };
        Assert.Equal(3, badge.Count);
    }

    [AvaloniaFact]
    public void MdDivider_horizontal()
    {
        var d = new MdDivider();
        Assert.False(d.IsVertical);
    }

    [AvaloniaFact]
    public void MdLabel_role()
    {
        var l = new MdLabel { Text = "Title", Role = MdTypeRole.HeadlineMedium };
        Assert.Equal(MdTypeRole.HeadlineMedium, l.Role);
    }

    [AvaloniaFact]
    public void MdSafeArea_edges()
    {
        var safe = new MdSafeArea { Edges = MdSafeAreaEdges.Top, Body = new MdLabel { Text = "x" } };
        Assert.Equal(MdSafeAreaEdges.Top, safe.Edges);
    }

    private static (Window Window, MdSplitView Split) ShowSplit(MdSplitView split)
    {
        var window = new Window
        {
            Width = 800,
            Height = 500,
            Content = split,
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, split);
    }

    [AvaloniaFact]
    public void MdSplitView_applies_initial_ratio_once()
    {
        var split = new MdSplitView
        {
            InitialRatio = 0.5,
            Pane = new MdLabel { Text = "Pane" },
            DetailContent = new MdLabel { Text = "Detail" },
        };
        var (window, _) = ShowSplit(split);

        Assert.Equal(400, split.PaneWidth, 1);

        window.Width = 600;
        for (var i = 0; i < 3; i++)
            Dispatcher.UIThread.RunJobs();

        Assert.Equal(400, split.PaneWidth, 1);
        window.Close();
    }

    [AvaloniaFact]
    public void MdSplitView_keeps_explicit_pane_width_over_initial_ratio()
    {
        var split = new MdSplitView
        {
            PaneWidth = 180,
            InitialRatio = 0.5,
            Pane = new MdLabel { Text = "Pane" },
            DetailContent = new MdLabel { Text = "Detail" },
        };
        var (window, _) = ShowSplit(split);

        Assert.Equal(180, split.PaneWidth, 1);
        window.Close();
    }
}
