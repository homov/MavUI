using Avalonia.Controls;
using Avalonia.Media;
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
        var roles = new MavRadioGroup().Choices(
            (L("Selection.Role.Admin", "Admin"), "admin", true),
            (L("Selection.Role.Editor", "Editor"), "editor", false),
            (L("Selection.Role.Viewer", "Viewer"), "viewer", false));

        var single = new MavSegmentedButton()
            .AddSegment(L("Selection.Segment.Day", "Day"), selected: true)
            .AddSegment(L("Selection.Segment.Week", "Week"))
            .AddSegment(L("Selection.Segment.Month", "Month"));

        var multi = new MavSegmentedButton()
            .MultiSelect(true)
            .AddSegment("Bold", MavIcons.FormatBold, selected: true)
            .AddSegment("Italic", MavIcons.FormatItalic)
            .AddSegment("Underline", MavIcons.FormatUnderlined);

        var chips = new MavChipGroup()
            .SingleSelection(true)
            .AddChip(L("Selection.Chip.Design", "Design"), MavChipKind.Filter, MavIcons.Palette, selected: true)
            .AddChip(L("Selection.Chip.Engineering", "Engineering"), MavChipKind.Filter, MavIcons.Code)
            .AddChip(L("Selection.Chip.Research", "Research"), MavChipKind.Filter, MavIcons.Science);

        var roleSelect = new MavSelect()
            .Label(L("Selection.Select.Role", "Role"))
            .Sized(220)
            .ItemsSource(new[]
            {
                L("Selection.Role.Admin", "Admin"),
                L("Selection.Role.Editor", "Editor"),
                L("Selection.Role.Viewer", "Viewer"),
            })
            .SelectedIndex(1);

        var cityCombo = new MavComboBox()
            .Label(L("Selection.Combo.City", "City"))
            .Sized(220)
            .ItemsSource(new[] { "Berlin", "London", "Tokyo" })
            .Text("Berlin");

        return Page(
            Section(
                "Selection.Toggles.Section", "Toggles and checks",
                "Selection.Toggles.Section.Body", "Switches, checkboxes (including three-state) and radio groups.",
                Card(
                    "MavSwitch, MavCheckBox, MavRadioButton and MavRadioGroup",
                    "Selection.Toggles.Card.Body", "Selection controls expose IsToggled/IsChecked and raise Toggled/CheckedChanged; all support keyboard Space/Enter.",
                    "MavSwitch.IsToggled · MavCheckBox.IsThreeState · MavRadioButton.Label · MavRadioGroup.AddOption",
                    Adaptive(260,
                        Plain(new StackPanel().Spacing(10).Children(
                            new MavSwitch().IsToggled(true).ShowIcons(true)
                                .OnIcon(MavIcons.Notifications)
                                .OffIcon(MavIcons.NotificationsOff),
                            new MavCheckBox().Label(L("Selection.Check.Terms", "Accept terms")).IsChecked(true),
                            new MavCheckBox().Label(L("Selection.Check.Three", "Three-state")).IsIndeterminate(true).IsThreeState(true))),
                        Plain(new StackPanel().Spacing(10).Children(
                            roles,
                            new MavRadioButton().Label(L("Selection.Radio.Standalone", "Standalone radio")).IsChecked(true)))))),
            Section(
                "Selection.Sliders.Section", "Sliders and segmented buttons",
                "Selection.Sliders.Section.Body", "Continuous, ranged and segmented value selection.",
                Card(
                    "MavSlider, MavRangeSlider and MavSegmentedButton",
                    "Selection.Sliders.Card.Body", "Sliders support steps, divisions, value indicators and keyboard arrows; segmented buttons can be single or multi-select.",
                    "MavSlider.Value · MavRangeSlider.StartValue/EndValue · MavSegmentedButton.MultiSelect",
                    new MavSlider().Minimum(0).Maximum(100).Value(64).ShowValueIndicator(true),
                    new MavRangeSlider().Minimum(0).Maximum(100).StartValue(20).EndValue(80),
                    Row(single, multi))),
            Section(
                "Selection.Chips.Section", "Chips and dropdowns",
                "Selection.Chips.Section.Body", "Chips for filters, assist and input; chip groups, select, combo box and rating.",
                Card(
                    "MavChip, MavChipGroup, MavSelect, MavComboBox and MavRatingBar",
                    "Selection.Chips.Card.Body", "Chips cover assist, filter, input and suggestion; select and combo box provide options; rating supports half stars.",
                    "MavChip.Kind · MavChipGroup.SingleSelection · MavSelect.Options · MavComboBox.Items · MavRatingBar.AllowHalf",
                    Row(
                        new MavChip("Assist", MavChipKind.Assist, MavIcons.TipsAndUpdates),
                        new MavChip("Filter", MavChipKind.Filter, null).IsSelected(true),
                        new MavChip("Input", MavChipKind.Input, MavIcons.Close),
                        new MavChip("Suggestion", MavChipKind.Suggestion, null)),
                    chips,
                    Row(roleSelect, cityCombo, new MavRatingBar().Value(4.5).MaxRating(5).AllowHalf(true)))));
    }

    /// <summary>Adds radio options (label, value, selected) and returns the group for chaining.</summary>
    private static MavRadioGroup Choices(this MavRadioGroup group, params (string Label, string Value, bool Selected)[] options)
    {
        foreach (var option in options)
            group.AddOption(option.Label, option.Value, option.Selected);
        return group;
    }
}
