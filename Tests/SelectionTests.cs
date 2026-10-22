using Avalonia.Headless.XUnit;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Xunit;

namespace Md3.Avalonia.Tests;

public class SelectionTests
{
    [AvaloniaFact]
    public void MdSwitch_toggled()
    {
        var s = new MdSwitch { IsToggled = true, ShowIcons = true };
        Assert.True(s.IsToggled);
        s.Toggled += (_, on) => Assert.True(on);
    }

    [AvaloniaFact]
    public void MdCheckBox_indeterminate()
    {
        var c = new MdCheckBox("Accept") { IsIndeterminate = true, IsThreeState = true };
        Assert.Equal(MdCheckState.Indeterminate, c.State);
    }

    [AvaloniaFact]
    public void MdRadioGroup_selection()
    {
        var group = new MdRadioGroup();
        group.AddOption("Admin", "admin", selected: true);
        Assert.Equal(0, group.SelectedIndex);
    }

    [AvaloniaFact]
    public void MdSlider_value_changed()
    {
        var slider = new MdSlider { Minimum = 0, Maximum = 100, Value = 30, ShowValueLabel = true };
        Assert.Equal(30, slider.Value);
    }

    [AvaloniaFact]
    public void MdSegmentedButton_multi()
    {
        var seg = new MdSegmentedButton { MultiSelect = true };
        seg.AddSegment("A");
        seg.AddSegment("B", selected: true);
        Assert.True(seg.MultiSelect);
    }

    [AvaloniaFact]
    public void MdChip_removable()
    {
        var chip = new MdChip("Delete", MdChipKind.Filter) { ShowCloseIcon = true };
        Assert.True(chip.ShowCloseIcon);
    }

    [AvaloniaFact]
    public void MdChipGroup_single_selection()
    {
        var group = new MdChipGroup { SingleSelection = true };
        group.AddChip("Red", selected: true);
        Assert.Equal(0, group.SelectedIndex);
    }

    [AvaloniaFact]
    public void MdSelect_option()
    {
        var sel = new MdSelect("Role", new[] { "A", "B", "C" }) { SelectedIndex = 1 };
        Assert.Equal("B", sel.SelectedItem);
    }

    [AvaloniaFact]
    public void MdComboBox_text()
    {
        var combo = new MdComboBox("Role", new[] { "Dev", "QA" }) { Text = "Dev" };
        Assert.Equal("Dev", combo.Text);
    }
}
