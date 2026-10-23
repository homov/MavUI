using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Mav;
using Mav.Controls;
using Mav.Primitives;
using Mav.Theming;
using Xunit;

namespace Mav.Tests;

public class ContainerTests
{
    [AvaloniaFact]
    public void MavCard_filled()
    {
        var card = new MavCard(MavCardVariant.Filled, new MavLabel { Text = "Body" });
        Assert.Equal(MavCardVariant.Filled, card.Variant);
    }

    [AvaloniaFact]
    public void MavAvatar_initials()
    {
        var av = new MavAvatar { Initials = "AB" };
        Assert.Equal("AB", av.Initials);
    }

    [AvaloniaFact]
    public void MavBadge_count()
    {
        var badge = new MavBadge { Text = "3", Count = 3 };
        Assert.Equal(3, badge.Count);
    }

    [AvaloniaFact]
    public void MavDivider_horizontal()
    {
        var d = new MavDivider();
        Assert.False(d.IsVertical);
    }

    [AvaloniaFact]
    public void MavLabel_role()
    {
        var l = new MavLabel { Text = "Title", Role = MavTypeRole.HeadlineMedium };
        Assert.Equal(MavTypeRole.HeadlineMedium, l.Role);
    }

    [AvaloniaFact]
    public void MavSafeArea_edges()
    {
        var safe = new MavSafeArea { Edges = MavSafeAreaEdges.Top, Child = new MavLabel { Text = "x" } };
        Assert.Equal(MavSafeAreaEdges.Top, safe.Edges);
    }

    private static (Window Window, MavSplitView Split) ShowSplit(MavSplitView split)
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
    public void MavSplitView_applies_initial_ratio_once()
    {
        var split = new MavSplitView
        {
            InitialRatio = 0.5,
            Pane = new MavLabel { Text = "Pane" },
            DetailContent = new MavLabel { Text = "Detail" },
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
    public void MavSplitView_keeps_explicit_pane_width_over_initial_ratio()
    {
        var split = new MavSplitView
        {
            PaneWidth = 180,
            InitialRatio = 0.5,
            Pane = new MavLabel { Text = "Pane" },
            DetailContent = new MavLabel { Text = "Detail" },
        };
        var (window, _) = ShowSplit(split);

        Assert.Equal(180, split.PaneWidth, 1);
        window.Close();
    }
}
