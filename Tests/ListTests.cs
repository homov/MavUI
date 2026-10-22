using Avalonia.Headless.XUnit;
using Md3.Avalonia.Controls;
using Xunit;

namespace Md3.Avalonia.Tests;

public class ListTests
{
    [AvaloniaFact]
    public void ShowDividers_applies_to_items_added_after_the_property_was_set()
    {
        var list = new MdList { ShowDividers = true };
        var first = new MdListItem("First");
        var second = new MdListItem("Second");
        var third = new MdListItem("Third");

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
        var list = new MdList { ShowDividers = true };
        var first = new MdListItem("First");
        var second = new MdListItem("Second");
        list.AddItem(first);
        list.AddItem(second);

        list.ShowDividers = false;

        Assert.False(first.ShowDivider);
        Assert.False(second.ShowDivider);
    }

    [AvaloniaFact]
    public void Explicit_item_dividers_are_kept_when_the_list_does_not_manage_them()
    {
        var list = new MdList();
        var item = new MdListItem("Row") { ShowDivider = true };

        list.AddItem(item);

        Assert.True(item.ShowDivider);
    }
}
