using Avalonia.Headless.XUnit;
using Mav.Controls;
using Xunit;

namespace Mav.Tests;

// Behavior shared by the navigation entries through MavNavigationItemBase.
public class NavigationItemBaseTests
{
    [AvaloniaFact]
    public void Destination_types_derive_from_the_shared_base()
    {
        Assert.IsAssignableFrom<MavNavigationItemBase>(new MavNavigationDestination());
        Assert.IsAssignableFrom<MavNavigationItemBase>(new MavSuiteDestination());
        Assert.IsAssignableFrom<MavNavigationItemBase>(new MavDrawerItem());
    }

    [AvaloniaFact]
    public void Label_change_raises_ItemChanged_on_every_entry_type()
    {
        var items = new MavNavigationItemBase[]
        {
            new MavNavigationDestination("A", "a"),
            new MavSuiteDestination("A", "a"),
            new MavDrawerItem("A", "a"),
        };

        foreach (var item in items)
        {
            var raised = 0;
            item.ItemChanged += (_, _) => raised++;

            item.Label = "B";

            Assert.Equal(1, raised);
        }
    }

    [AvaloniaFact]
    public void SelectedIcon_change_raises_ItemChanged_on_every_entry_type()
    {
        var items = new MavNavigationItemBase[]
        {
            new MavNavigationDestination("A", "a"),
            new MavSuiteDestination("A", "a"),
            new MavDrawerItem("A", "a"),
        };

        foreach (var item in items)
        {
            var raised = 0;
            item.ItemChanged += (_, _) => raised++;

            item.SelectedIcon = "a-filled";

            Assert.Equal(1, raised);
        }
    }

    [AvaloniaFact]
    public void Navigation_destination_defaults_to_the_home_icon_and_drawer_item_to_the_folder_icon()
    {
        Assert.Equal(MavIcons.Home, new MavNavigationDestination().Icon);
        Assert.Equal(MavIcons.Home, new MavSuiteDestination().Icon);
        Assert.Equal(MavIcons.Folder, new MavDrawerItem().Icon);
    }

    [AvaloniaFact]
    public void BadgeText_is_null_by_default()
    {
        Assert.Null(new MavNavigationDestination("A", "a").BadgeText);
        Assert.Null(new MavDrawerItem("A", "a").BadgeText);
    }

    [AvaloniaFact]
    public void Showing_the_badge_dot_gives_an_empty_badge_text_when_none_is_set()
    {
        var destination = new MavNavigationDestination("A", "a");

        destination.ShowBadgeDot = true;

        Assert.Equal(string.Empty, destination.BadgeText);
    }
}
