using Avalonia.Headless.XUnit;
using Mav.Controls;
using Xunit;

namespace Mav.Tests;

public class ListTests
{
    [AvaloniaFact]
    public void ShowDividers_applies_to_items_added_after_the_property_was_set()
    {
        var list = new MavList { ShowDividers = true };
        var first = new MavListItem("First");
        var second = new MavListItem("Second");
        var third = new MavListItem("Third");

        list.AddItem(first);
        list.AddItem(second);
        list.AddItem(third);

        Assert.True(first.ShowDivider);
        Assert.True(second.ShowDivider);
        Assert.False(third.ShowDivider);
    }

    [AvaloniaFact]
    public void Disabling_ShowDividers_clears_managed_dividers()
    {
        var list = new MavList { ShowDividers = true };
        var first = new MavListItem("First");
        var second = new MavListItem("Second");
        list.AddItem(first);
        list.AddItem(second);

        list.ShowDividers = false;

        Assert.False(first.ShowDivider);
        Assert.False(second.ShowDivider);
    }

    [AvaloniaFact]
    public void Explicit_item_dividers_are_kept_when_the_list_does_not_manage_them()
    {
        var list = new MavList();
        var item = new MavListItem("Row") { ShowDivider = true };

        list.AddItem(item);

        Assert.True(item.ShowDivider);
    }
}
