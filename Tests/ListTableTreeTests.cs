using Avalonia.Headless.XUnit;
using Mav.Controls;
using Xunit;

namespace Mav.Tests;

public class ListTableTreeTests
{
    [AvaloniaFact]
    public void MavList_add_item_and_subheader()
    {
        var list = new MavList { ShowDividers = true };
        list.AddSubheader("Today");
        list.AddItem(new MavListItem("Design review") { LeadingIcon = MavIcons.Event });
        Assert.True(list.Children.Count > 0);
    }

    [AvaloniaFact]
    public void MavDataTable_columns_and_rows()
    {
        var table = new MavDataTable { ItemsSource = new[] { new { Name = "A", Score = 1 } } };
        table.AddColumn("Name", r => ((dynamic)r).Name);
        table.AddColumn("Score", r => ((dynamic)r).Score, isNumeric: true);
        Assert.True(table.Columns.Count > 0);
    }

    [AvaloniaFact]
    public void MavTreeView_root_and_child()
    {
        var tree = new MavTreeView();
        tree.Add("Root");
        Assert.True(tree.Roots.Count > 0);
    }

    [AvaloniaFact]
    public void MavCarousel_item()
    {
        var carousel = new MavCarousel();
        carousel.AddItem("Title", "Sub", MavIcons.Home);
        Assert.True(carousel.Items.Count > 0);
    }
}
