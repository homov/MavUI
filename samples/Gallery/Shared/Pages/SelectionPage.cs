using Avalonia.Controls;
using Avalonia.Layout;
using Mav;
using Mav.Controls;
using Mav.Theming;

namespace Mav.Gallery;

internal static class SelectionPage
{
    public static Control Build()
    {
        return GalleryUi.Page(
            GalleryUi.Section("Toggles and checks", "Switches, checkboxes (including three-state) and radio groups.", Toggles()),
            GalleryUi.Section("Sliders and segmented buttons", "Continuous, ranged and segmented value selection.", Sliders()),
            GalleryUi.Section("Chips and dropdowns", "Chips for filters/assist/input, chip groups, select, combo box and rating.", ChipsAndSelects()));
    }

    private static Control Toggles()
    {
        var toggle = new MavSwitch(true, showIcons: true)
        {
            OnIcon = MavIcons.Notifications,
            OffIcon = MavIcons.NotificationsOff,
        };
        var check = new MavCheckBox("Accept terms", isChecked: true);
        var three = new MavCheckBox("Three-state", isIndeterminate: true)
        {
            IsThreeState = true,
        };
        var radios = new MavRadioGroup();
        radios.AddOption("Admin", "admin", selected: true);
        radios.AddOption("Editor", "editor");
        radios.AddOption("Viewer", "viewer");
        var standaloneRadio = new MavRadioButton { Label = "Standalone radio", IsChecked = true };

        return GalleryUi.Showcase(
            "MavSwitch, MavCheckBox, MavRadioButton and MavRadioGroup",
            "Selection controls expose IsToggled/IsChecked and raise Toggled/CheckedChanged; all support keyboard Space/Enter.",
            GalleryUi.Row(
                new MavCard(MavCardVariant.Outlined, GalleryUi.Column(toggle, check, three)) { Width = 280 },
                new MavCard(MavCardVariant.Outlined, GalleryUi.Column(radios, standaloneRadio)) { Width = 280 }),
            "MavSwitch.IsToggled · MavCheckBox.IsThreeState · MavRadioButton.Label · MavRadioGroup.AddOption");
    }

    private static Control Sliders()
    {
        var slider = new MavSlider
        {
            Minimum = 0,
            Maximum = 100,
            Value = 64,
            ShowValueIndicator = true,
        };
        var range = new MavRangeSlider
        {
            Minimum = 0,
            Maximum = 100,
            StartValue = 20,
            EndValue = 80,
        };

        var single = new MavSegmentedButton();
        single.AddSegment("Day", selected: true);
        single.AddSegment("Week");
        single.AddSegment("Month");

        var multi = new MavSegmentedButton { MultiSelect = true };
        multi.AddSegment("Bold", MavIcons.FormatBold, selected: true);
        multi.AddSegment("Italic", MavIcons.FormatItalic);
        multi.AddSegment("Underline", MavIcons.FormatUnderlined);

        return GalleryUi.Showcase(
            "MavSlider, MavRangeSlider and MavSegmentedButton",
            "Sliders support steps, divisions, value indicators and keyboard arrows; segmented buttons can be single or multi-select.",
            GalleryUi.Column(
                slider,
                range,
                GalleryUi.Row(single, multi)),
            "MavSlider.Value · MavRangeSlider.StartValue/EndValue · MavSegmentedButton.MultiSelect");
    }

    private static Control ChipsAndSelects()
    {
        var chips = GalleryUi.Row(
            new MavChip("Assist", MavChipKind.Assist, MavIcons.TipsAndUpdates),
            new MavChip("Filter", MavChipKind.Filter, selected: true),
            new MavChip("Input", MavChipKind.Input, MavIcons.Close),
            new MavChip("Suggestion", MavChipKind.Suggestion));

        var chipGroup = new MavChipGroup { SingleSelection = true };
        chipGroup.AddChip("Design", MavChipKind.Filter, MavIcons.Palette, selected: true);
        chipGroup.AddChip("Engineering", MavChipKind.Filter, MavIcons.Code);
        chipGroup.AddChip("Research", MavChipKind.Filter, MavIcons.Science);

        var select = new MavSelect("Role", new[] { "Admin", "Editor", "Viewer" }) { SelectedIndex = 1 };
        var combo = new MavComboBox("City", new[] { "Berlin", "London", "Tokyo" }) { Text = "Berlin" };
        var rating = new MavRatingBar { MaxRating = 5, AllowHalf = true, Value = 4.5 };

        return GalleryUi.Showcase(
            "MavChip, MavChipGroup, MavSelect, MavComboBox and MavRatingBar",
            "Chips cover assist/filter/input/suggestion; select and combo box provide options; rating supports half stars.",
            GalleryUi.Column(
                chips,
                chipGroup,
                GalleryUi.Row(select, combo, rating)),
            "MavChip.Kind · MavChipGroup.SingleSelection · MavSelect.Options · MavComboBox.Items · MavRatingBar.AllowHalf");
    }
}
