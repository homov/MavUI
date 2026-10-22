using Avalonia.Headless.XUnit;
using Mav.Controls;
using Xunit;

namespace Mav.Tests;

// Defaults, clamping and state rules of controls that had no direct tests.
// Only properties and events are checked; no animation is awaited.
public class ControlStateTests
{
    [AvaloniaFact]
    public void BreadcrumbBar_SetPath_trims_segments_and_skips_blank_ones()
    {
        var bar = new MavBreadcrumbBar();

        bar.SetPath(" Home ", " ", "Docs");

        Assert.Equal(2, bar.Items.Count);
        Assert.Equal("Home", bar.Items[0].Text);
        Assert.Equal("Docs", bar.Items[1].Text);
    }

    [AvaloniaFact]
    public void BreadcrumbBar_NavigateToIndex_drops_the_segments_after_it()
    {
        var bar = new MavBreadcrumbBar();
        bar.SetPath("A", "B", "C");

        bar.NavigateToIndex(1);

        Assert.Equal(2, bar.Items.Count);
        Assert.Equal("B", bar.Items[1].Text);
    }

    [AvaloniaFact]
    public void BreadcrumbBar_NavigateToIndex_ignores_an_out_of_range_index()
    {
        var bar = new MavBreadcrumbBar();
        bar.SetPath("A", "B", "C");

        bar.NavigateToIndex(5);

        Assert.Equal(3, bar.Items.Count);
    }

    [AvaloniaFact]
    public void RadialGauge_Value_is_clamped_to_Minimum_and_Maximum()
    {
        var gauge = new MavRadialGauge();
        Assert.Equal(65.0, gauge.Value);

        gauge.Value = 150;
        Assert.Equal(100.0, gauge.Value);

        gauge.Value = -10;
        Assert.Equal(0.0, gauge.Value);
    }

    [AvaloniaFact]
    public void SegmentedProgressBar_defaults_to_five_segments_and_60_percent()
    {
        var bar = new MavSegmentedProgressBar();

        Assert.Equal(0.6, bar.Value);
        Assert.Equal(5, bar.SegmentCount);
    }

    [AvaloniaFact]
    public void SettingsCard_Title_and_Header_are_the_same_value()
    {
        var card = new MavSettingsCard { Title = "Wi-Fi" };
        Assert.Equal("Wi-Fi", card.Header);

        card.Header = "Bluetooth";
        Assert.Equal("Bluetooth", card.Title);
    }

    [AvaloniaFact]
    public void SettingsCard_IsActionCard_mirrors_IsActionable()
    {
        var card = new MavSettingsCard();

        card.IsActionCard = true;

        Assert.True(card.IsActionable);
    }

    [AvaloniaFact]
    public void SettingsExpander_is_collapsed_by_default()
    {
        var expander = new MavSettingsExpander { Title = "Advanced" };

        Assert.False(expander.IsExpanded);
        Assert.Equal("Advanced", expander.Header);
    }

    [AvaloniaFact]
    public void SettingsExpander_raises_ExpandedChanged_with_the_new_state()
    {
        var expander = new MavSettingsExpander { Title = "Advanced" };
        bool? raised = null;
        expander.ExpandedChanged += (_, expanded) => raised = expanded;

        expander.IsExpanded = true;

        Assert.True(raised);
    }

    [AvaloniaFact]
    public void EmptyState_uses_the_given_title()
    {
        var state = new MavEmptyState("No messages");

        Assert.Equal("No messages", state.Title);
    }

    [AvaloniaFact]
    public void ContextualActionBar_SelectedCount_below_zero_becomes_zero()
    {
        var bar = new MavContextualActionBar();

        bar.SelectedCount = -4;

        Assert.Equal(0, bar.SelectedCount);
    }

    [AvaloniaFact]
    public void ContextualActionBar_follows_the_selection_when_AutoHideWhenEmpty_is_set()
    {
        var bar = new MavContextualActionBar { AutoHideWhenEmpty = true };

        bar.SelectedCount = 3;
        Assert.True(bar.IsVisible);

        bar.SelectedCount = 0;
        Assert.False(bar.IsVisible);
    }
}
