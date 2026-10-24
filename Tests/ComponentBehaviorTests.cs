using System.Collections.Generic;
using Avalonia.Headless.XUnit;
using Mav.Controls;
using Xunit;

namespace Mav.Tests;

// Behavior of MD3 components that had no direct test coverage before.
public class ComponentBehaviorTests
{
    [AvaloniaFact]
    public void A_radio_checked_before_it_is_shown_paints_checked_at_once()
    {
        // An items host or a binding sets IsChecked while the radio has no top level yet. Animating
        // the dot from 0 there left the current choice looking unselected until a frame ran.
        var radio = new MavRadioButton("Admin");

        radio.IsChecked = true;
        Assert.Equal(1.0, radio.DotProgress, 3);

        radio.IsChecked = false;
        Assert.Equal(0.0, radio.DotProgress, 3);
    }

    [AvaloniaFact]
    public void Banner_shows_the_message_it_was_given()
    {
        var banner = new MavBanner { Message = "Saved" };

        Assert.Equal("Saved", banner.Message);
    }

    [AvaloniaFact]
    public void Banner_Show_and_Dismiss_toggle_visibility()
    {
        var banner = new MavBanner { Message = "Offline" };

        banner.Show();
        Assert.True(banner.IsVisible);

        banner.Dismiss();
        Assert.False(banner.IsVisible);
    }

    [AvaloniaFact]
    public void NavigationBar_AddDestination_keeps_insertion_order()
    {
        var bar = new MavNavigationBar();

        bar.AddDestination("Home", "home");
        bar.AddDestination("Search", "search");

        Assert.Equal(2, bar.Destinations.Count);
        Assert.Equal("Search", bar.Destinations[1].Label);
    }

    [AvaloniaFact]
    public void NavigationBar_SelectedIndex_is_clamped_to_the_destinations()
    {
        var bar = new MavNavigationBar();
        bar.AddDestination("A", "a");
        bar.AddDestination("B", "b");
        bar.AddDestination("C", "c");

        bar.SelectedIndex = 10;
        Assert.Equal(2, bar.SelectedIndex);

        bar.SelectedIndex = -5;
        Assert.Equal(0, bar.SelectedIndex);
    }

    [AvaloniaFact]
    public void NavigationBar_raises_SelectedIndexChanged_only_when_the_index_changes()
    {
        var bar = new MavNavigationBar();
        bar.AddDestination("A", "a");
        bar.AddDestination("B", "b");
        var raised = new List<int>();
        bar.SelectedIndexChanged += (_, index) => raised.Add(index);

        bar.SelectedIndex = 1;
        bar.SelectedIndex = 1;

        Assert.Equal(new[] { 1 }, raised);
    }

    [AvaloniaFact]
    public void NavigationRail_SelectedIndex_is_clamped_to_the_destinations()
    {
        var rail = new MavNavigationRail();
        rail.AddDestination("A", "a");
        rail.AddDestination("B", "b");

        rail.SelectedIndex = 7;
        Assert.Equal(1, rail.SelectedIndex);

        rail.SelectedIndex = -1;
        Assert.Equal(0, rail.SelectedIndex);
    }

    [AvaloniaFact]
    public void SearchBar_raises_TextChanged_with_the_new_text()
    {
        var search = new MavSearchBar();
        string? last = null;
        search.TextChanged += (_, text) => last = text;

        search.Text = "kiwi";

        Assert.Equal("kiwi", last);
    }

    [AvaloniaFact]
    public void SearchBar_turns_null_text_into_an_empty_string()
    {
        var search = new MavSearchBar { Text = "old" };

        search.Text = null!;

        Assert.Equal(string.Empty, search.Text);
    }
}
