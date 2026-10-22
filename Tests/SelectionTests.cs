using Avalonia.Headless.XUnit;
using Mav;
using Mav.Controls;
using Xunit;

namespace Mav.Tests;

public class SelectionTests
{
    [AvaloniaFact]
    public void MavSwitch_toggled()
    {
        var s = new MavSwitch { IsToggled = true, ShowIcons = true };
        Assert.True(s.IsToggled);
        s.Toggled += (_, on) => Assert.True(on);
    }

    [AvaloniaFact]
    public void MavCheckBox_indeterminate()
    {
        var c = new MavCheckBox("Accept") { IsIndeterminate = true, IsThreeState = true };
        Assert.Equal(MavCheckState.Indeterminate, c.State);
    }

    [AvaloniaFact]
    public void MavRadioGroup_selection()
    {
        var group = new MavRadioGroup();
        group.AddOption("Admin", "admin", selected: true);
        Assert.Equal(0, group.SelectedIndex);
    }

    [AvaloniaFact]
    public void MavSlider_value_changed()
    {
        var slider = new MavSlider { Minimum = 0, Maximum = 100, Value = 30, ShowValueLabel = true };
        Assert.Equal(30, slider.Value);
    }

    [AvaloniaFact]
    public void MavSegmentedButton_multi()
    {
        var seg = new MavSegmentedButton { MultiSelect = true };
        seg.AddSegment("A");
        seg.AddSegment("B", selected: true);
        Assert.True(seg.MultiSelect);
    }

    [AvaloniaFact]
    public void MavChip_removable()
    {
        var chip = new MavChip("Delete", MavChipKind.Filter) { ShowCloseIcon = true };
        Assert.True(chip.ShowCloseIcon);
    }

    [AvaloniaFact]
    public void MavChipGroup_single_selection()
    {
        var group = new MavChipGroup { SingleSelection = true };
        group.AddChip("Red", selected: true);
        Assert.Equal(0, group.SelectedIndex);
    }

    [AvaloniaFact]
    public void MavSelect_option()
    {
        var sel = new MavSelect("Role", new[] { "A", "B", "C" }) { SelectedIndex = 1 };
        Assert.Equal("B", sel.SelectedItem);
    }

    [AvaloniaFact]
    public void MavComboBox_text()
    {
        var combo = new MavComboBox("Role", new[] { "Dev", "QA" }) { Text = "Dev" };
        Assert.Equal("Dev", combo.Text);
    }
}
