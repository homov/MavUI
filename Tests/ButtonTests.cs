using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Primitives;
using Xunit;

namespace Md3.Avalonia.Tests;

public class ButtonTests
{
    [AvaloniaFact]
    public void MdButton_created_and_laid_out_without_crash()
    {
        var btn = new MdButton("Save", MdButtonVariant.Filled, MdIcons.Save);
        btn.Width = 120; btn.Height = 48;
        Assert.NotNull(btn);
        Assert.Equal("Save", btn.Text);
    }

    [AvaloniaFact]
    public void MdIconButton_toggled()
    {
        var b = new MdIconButton(MdIcons.Favorite, MdIconButtonVariant.Filled, isToggle: true);
        Assert.True(b.IsToggle);
        b.IsSelected = true;
        Assert.True(b.IsSelected);
    }

    [AvaloniaFact]
    public void MdFab_and_ExtendedFab()
    {
        var fab = new MdFab(MdIcons.Add, MdFabSize.Regular, MdFabColor.Primary);
        Assert.Equal(MdIcons.Add, fab.Icon);
        var ext = new MdExtendedFab("Add", MdIcons.Add);
        Assert.Equal("Add", ext.Text);
    }

    [AvaloniaFact]
    public void MdButtonGroup_and_SplitButton()
    {
        var group = new MdButtonGroup { Kind = MdButtonGroupKind.Connected };
        group.AddButton(new MdButton("Day"));
        Assert.Single(group.Children);

        var split = new MdSplitButton { Text = "Save", Menu = new MdMenu() };
        Assert.NotNull(split.Menu);
    }
}
