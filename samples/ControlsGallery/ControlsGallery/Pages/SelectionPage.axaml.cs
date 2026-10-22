using Avalonia.Controls;
using Mav;
using Mav.Controls;

namespace Mav.ControlsGallery.Pages;

public partial class SelectionPage : UserControl
{
    public SelectionPage()
    {
        InitializeComponent();

        RoleRadios.AddOption(MavLocalization.Get("Selection.Role.Admin", "Admin"), "admin", selected: true);
        RoleRadios.AddOption(MavLocalization.Get("Selection.Role.Editor", "Editor"), "editor");
        RoleRadios.AddOption(MavLocalization.Get("Selection.Role.Viewer", "Viewer"), "viewer");

        SingleSegments.AddSegment(MavLocalization.Get("Selection.Segment.Day", "Day"), selected: true);
        SingleSegments.AddSegment(MavLocalization.Get("Selection.Segment.Week", "Week"));
        SingleSegments.AddSegment(MavLocalization.Get("Selection.Segment.Month", "Month"));

        MultiSegments.AddSegment("Bold", MavIcons.FormatBold, selected: true);
        MultiSegments.AddSegment("Italic", MavIcons.FormatItalic);
        MultiSegments.AddSegment("Underline", MavIcons.FormatUnderlined);

        ChipGroup.AddChip(MavLocalization.Get("Selection.Chip.Design", "Design"), MavChipKind.Filter, MavIcons.Palette, selected: true);
        ChipGroup.AddChip(MavLocalization.Get("Selection.Chip.Engineering", "Engineering"), MavChipKind.Filter, MavIcons.Code);
        ChipGroup.AddChip(MavLocalization.Get("Selection.Chip.Research", "Research"), MavChipKind.Filter, MavIcons.Science);

        RoleSelect.ItemsSource = new[]
        {
            MavLocalization.Get("Selection.Role.Admin", "Admin"),
            MavLocalization.Get("Selection.Role.Editor", "Editor"),
            MavLocalization.Get("Selection.Role.Viewer", "Viewer"),
        };
        RoleSelect.SelectedIndex = 1;

        CityCombo.ItemsSource = new[] { "Berlin", "London", "Tokyo" };
        CityCombo.Text = "Berlin";
    }
}
