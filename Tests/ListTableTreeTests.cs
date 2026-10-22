using Avalonia.Headless.XUnit;
using Md3.Avalonia.Controls;
using Xunit;

namespace Md3.Avalonia.Tests;

public class ListTableTreeTests
{
    [AvaloniaFact]
    public void MdList_add_item_and_subheader()
    {
        var list = new MdList { ShowDividers = true };
        list.AddSubheader("Today");
        list.AddItem(new MdListItem("Design review") { LeadingIcon = MdIcons.Event });
        Assert.True(list.Children.Count > 0);
    }

    [AvaloniaFact]
    public void MdDataTable_columns_and_rows()
    {
        var table = new MdDataTable { ItemsSource = new[] { new { Name = "A", Score = 1 } } };
        table.AddColumn("Name", r => ((dynamic)r).Name);
        table.AddColumn("Score", r => ((dynamic)r).Score, isNumeric: true);
        Assert.True(table.Columns.Count > 0);
    }

    [AvaloniaFact]
    public void MdTreeView_root_and_child()
    {
        var tree = new MdTreeView();
        tree.Add("Root");
        Assert.True(tree.Roots.Count > 0);
    }

    [AvaloniaFact]
    public void MdCarousel_item()
    {
        var carousel = new MdCarousel();
        carousel.AddItem("Title", "Sub", MdIcons.Home);
        Assert.True(carousel.Items.Count > 0);
    }
}
