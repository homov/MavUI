using Avalonia.Controls;
using Mav;
using Mav.Controls;
using Mav.Theming;
using static Mav.ControlsGalleryMarkup.Pages.PageParts;

namespace Mav.ControlsGalleryMarkup.Pages;

/// <summary>Toggles, checks, sliders, segmented buttons, chips and dropdowns. Mirrors the AXAML Selection page.</summary>
public static class SelectionPage
{
    public static Control Build()
    {
        var roles = new MavRadioGroup();
        roles.AddOption(L("Selection.Role.Admin", "Admin"), "admin", selected: true);
        roles.AddOption(L("Selection.Role.Editor", "Editor"), "editor");
        roles.AddOption(L("Selection.Role.Viewer", "Viewer"), "viewer");

        var single = new MavSegmentedButton();
        single.AddSegment(L("Selection.Segment.Day", "Day"), selected: true);
        single.AddSegment(L("Selection.Segment.Week", "Week"));
        single.AddSegment(L("Selection.Segment.Month", "Month"));

        var multi = new MavSegmentedButton(multiSelect: true);
        multi.AddSegment("Bold", MavIcons.FormatBold, selected: true);
        multi.AddSegment("Italic", MavIcons.FormatItalic);
        multi.AddSegment("Underline", MavIcons.FormatUnderlined);

        var chips = new MavChipGroup(singleSelection: true);
        chips.AddChip(L("Selection.Chip.Design", "Design"), MavChipKind.Filter, MavIcons.Palette, selected: true);
        chips.AddChip(L("Selection.Chip.Engineering", "Engineering"), MavChipKind.Filter, MavIcons.Code);
        chips.AddChip(L("Selection.Chip.Research", "Research"), MavChipKind.Filter, MavIcons.Science);

        var roleSelect = new MavSelect { Label = L("Selection.Select.Role", "Role"), Width = 220 };
        roleSelect.ItemsSource = new[]
        {
            L("Selection.Role.Admin", "Admin"),
            L("Selection.Role.Editor", "Editor"),
            L("Selection.Role.Viewer", "Viewer"),
        };
        roleSelect.SelectedIndex = 1;

        var cityCombo = new MavComboBox { Label = L("Selection.Combo.City", "City"), Width = 220 };
        cityCombo.ItemsSource = new[] { "Berlin", "London", "Tokyo" };
        cityCombo.Text = "Berlin";

        return Page(
            Section(
                "Selection.Toggles.Section", "Toggles and checks",
                "Selection.Toggles.Section.Body", "Switches, checkboxes (including three-state) and radio groups.",
                Card(
                    "MavSwitch, MavCheckBox, MavRadioButton and MavRadioGroup",
                    "Selection.Toggles.Card.Body", "Selection controls expose IsToggled/IsChecked and raise Toggled/CheckedChanged; all support keyboard Space/Enter.",
                    "MavSwitch.IsToggled · MavCheckBox.IsThreeState · MavRadioButton.Label · MavRadioGroup.AddOption",
                    AdaptiveWrap(260,
                        Plain(new StackPanel().Spacing(10).Children(
                            new MavSwitch(true, true) { OnIcon = MavIcons.Notifications, OffIcon = MavIcons.NotificationsOff },
                            new MavCheckBox(L("Selection.Check.Terms", "Accept terms"), true),
                            new MavCheckBox(L("Selection.Check.Three", "Three-state"), false, true) { IsThreeState = true }))),
                        Plain(new StackPanel().Spacing(10).Children(
                            roles,
                            new MavRadioButton(L("Selection.Radio.Standalone", "Standalone radio"), true))))),
            Section(
                "Selection.Sliders.Section", "Sliders and segmented buttons",
                "Selection.Sliders.Section.Body", "Continuous, ranged and segmented value selection.",
                Card(
                    "MavSlider, MavRangeSlider and MavSegmentedButton",
                    "Selection.Sliders.Card.Body", "Sliders support steps, divisions, value indicators and keyboard arrows; segmented buttons can be single or multi-select.",
                    "MavSlider.Value · MavRangeSlider.StartValue/EndValue · MavSegmentedButton.MultiSelect",
                    new MavSlider { Minimum = 0, Maximum = 100, Value = 64, ShowValueIndicator = true },
                    new MavRangeSlider(0, 100, 20, 80),
                    Row(Tight(single), Tight(multi)))),
            Section(
                "Selection.Chips.Section", "Chips and dropdowns",
                "Selection.Chips.Section.Body", "Chips for filters, assist and input; chip groups, select, combo box and rating.",
                Card(
                    "MavChip, MavChipGroup, MavSelect, MavComboBox and MavRatingBar",
                    "Selection.Chips.Card.Body", "Chips cover assist, filter, input and suggestion; select and combo box provide options; rating supports half stars.",
                    "MavChip.Kind · MavChipGroup.SingleSelection · MavSelect.Options · MavComboBox.Items · MavRatingBar.AllowHalf",
                    Row(
                        new MavChip("Assist", MavChipKind.Assist, MavIcons.TipsAndUpdates),
                        new MavChip("Filter", MavChipKind.Filter, null) { IsSelected = true },
                        new MavChip("Input", MavChipKind.Input, MavIcons.Close),
                        new MavChip("Suggestion", MavChipKind.Suggestion, null)),
                    chips,
                    Row(roleSelect, cityCombo, new MavRatingBar(4.5, 5, true)))));
    }
}
