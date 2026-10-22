using Avalonia.Controls;
using Avalonia.Layout;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Theming;

namespace Md3.Gallery;

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
        var toggle = new MdSwitch(true, showIcons: true)
        {
            OnIcon = MdIcons.Notifications,
            OffIcon = MdIcons.NotificationsOff,
        };
        var check = new MdCheckBox("Accept terms", isChecked: true);
        var three = new MdCheckBox("Three-state", isIndeterminate: true)
        {
            IsThreeState = true,
        };
        var radios = new MdRadioGroup();
        radios.AddOption("Admin", "admin", selected: true);
        radios.AddOption("Editor", "editor");
        radios.AddOption("Viewer", "viewer");
        var standaloneRadio = new MdRadioButton { Label = "Standalone radio", IsChecked = true };

        return GalleryUi.Showcase(
            "MdSwitch, MdCheckBox, MdRadioButton and MdRadioGroup",
            "Selection controls expose IsToggled/IsChecked and raise Toggled/CheckedChanged; all support keyboard Space/Enter.",
            GalleryUi.Row(
                new MdCard(MdCardVariant.Outlined, GalleryUi.Column(toggle, check, three)) { Width = 280 },
                new MdCard(MdCardVariant.Outlined, GalleryUi.Column(radios, standaloneRadio)) { Width = 280 }),
            "MdSwitch.IsToggled · MdCheckBox.IsThreeState · MdRadioButton.Label · MdRadioGroup.AddOption");
    }

    private static Control Sliders()
    {
        var slider = new MdSlider
        {
            Minimum = 0,
            Maximum = 100,
            Value = 64,
            ShowValueIndicator = true,
        };
        var range = new MdRangeSlider
        {
            Minimum = 0,
            Maximum = 100,
            StartValue = 20,
            EndValue = 80,
        };

        var single = new MdSegmentedButton();
        single.AddSegment("Day", selected: true);
        single.AddSegment("Week");
        single.AddSegment("Month");

        var multi = new MdSegmentedButton { MultiSelect = true };
        multi.AddSegment("Bold", MdIcons.FormatBold, selected: true);
        multi.AddSegment("Italic", MdIcons.FormatItalic);
        multi.AddSegment("Underline", MdIcons.FormatUnderlined);

        return GalleryUi.Showcase(
            "MdSlider, MdRangeSlider and MdSegmentedButton",
            "Sliders support steps, divisions, value indicators and keyboard arrows; segmented buttons can be single or multi-select.",
            GalleryUi.Column(
                slider,
                range,
                GalleryUi.Row(single, multi)),
            "MdSlider.Value · MdRangeSlider.StartValue/EndValue · MdSegmentedButton.MultiSelect");
    }

    private static Control ChipsAndSelects()
    {
        var chips = GalleryUi.Row(
            new MdChip("Assist", MdChipKind.Assist, MdIcons.TipsAndUpdates),
            new MdChip("Filter", MdChipKind.Filter, selected: true),
            new MdChip("Input", MdChipKind.Input, MdIcons.Close),
            new MdChip("Suggestion", MdChipKind.Suggestion));

        var chipGroup = new MdChipGroup { SingleSelection = true };
        chipGroup.AddChip("Design", MdChipKind.Filter, MdIcons.Palette, selected: true);
        chipGroup.AddChip("Engineering", MdChipKind.Filter, MdIcons.Code);
        chipGroup.AddChip("Research", MdChipKind.Filter, MdIcons.Science);

        var select = new MdSelect("Role", new[] { "Admin", "Editor", "Viewer" }) { SelectedIndex = 1 };
        var combo = new MdComboBox("City", new[] { "Berlin", "London", "Tokyo" }) { Text = "Berlin" };
        var rating = new MdRatingBar { MaxRating = 5, AllowHalf = true, Value = 4.5 };

        return GalleryUi.Showcase(
            "MdChip, MdChipGroup, MdSelect, MdComboBox and MdRatingBar",
            "Chips cover assist/filter/input/suggestion; select and combo box provide options; rating supports half stars.",
            GalleryUi.Column(
                chips,
                chipGroup,
                GalleryUi.Row(select, combo, rating)),
            "MdChip.Kind · MdChipGroup.SingleSelection · MdSelect.Options · MdComboBox.Items · MdRatingBar.AllowHalf");
    }
}
