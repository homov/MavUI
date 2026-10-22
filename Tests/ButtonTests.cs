using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Mav;
using Mav.Controls;
using Mav.Primitives;
using Xunit;

namespace Mav.Tests;

public class ButtonTests
{
    [AvaloniaFact]
    public void MavButton_created_and_laid_out_without_crash()
    {
        var btn = new MavButton("Save", MavButtonVariant.Filled, MavIcons.Save);
        btn.Width = 120; btn.Height = 48;
        Assert.NotNull(btn);
        Assert.Equal("Save", btn.Text);
    }

    [AvaloniaFact]
    public void MavIconButton_toggled()
    {
        var b = new MavIconButton(MavIcons.Favorite, MavIconButtonVariant.Filled, isToggle: true);
        Assert.True(b.IsToggle);
        b.IsSelected = true;
        Assert.True(b.IsSelected);
    }

    [AvaloniaFact]
    public void MavFab_and_ExtendedFab()
    {
        var fab = new MavFab(MavIcons.Add, MavFabSize.Regular, MavFabColor.Primary);
        Assert.Equal(MavIcons.Add, fab.Icon);
        var ext = new MavExtendedFab("Add", MavIcons.Add);
        Assert.Equal("Add", ext.Text);
    }

    [AvaloniaFact]
    public void MavButtonGroup_and_SplitButton()
    {
        var group = new MavButtonGroup { Kind = MavButtonGroupKind.Connected };
        group.AddButton(new MavButton("Day"));
        Assert.Single(group.Children);

        var split = new MavSplitButton { Text = "Save", Menu = new MavMenu() };
        Assert.NotNull(split.Menu);
    }
}
