using Avalonia.Controls;
using Mav.Controls;
using static Mav.ControlsGalleryMarkup.Pages.PageParts;

namespace Mav.ControlsGalleryMarkup.Pages;

public static class SelectionPage
{
    public static Control Build() =>
        new StackPanel()
            .Spacing(18)
            .Children(
                Section(
                    "Toggles and checks",
                    "Switches and checkboxes, including a three-state checkbox.",
                    Row(
                        new MavSwitch(isToggled: true, showIcons: true),
                        new MavCheckBox("Accept terms", isChecked: true),
                        new MavCheckBox("Three-state", isIndeterminate: true))),
                Section(
                    "Sliders",
                    "Continuous value selection with a value indicator.",
                    Column(
                        new MavSlider()
                            .Minimum(0)
                            .Maximum(100)
                            .Value(64)
                            .ShowValueIndicator(true))),
                Section(
                    "Chips",
                    "Chips for assist, filter, input and suggestion actions.",
                    Row(
                        new MavChip("Assist", MavChipKind.Assist, MavIcons.TipsAndUpdates),
                        new MavChip("Filter", MavChipKind.Filter).IsSelected(true),
                        new MavChip("Input", MavChipKind.Input, MavIcons.Close),
                        new MavChip("Suggestion", MavChipKind.Suggestion))));
}
