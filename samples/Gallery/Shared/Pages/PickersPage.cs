using Avalonia.Controls;
using Avalonia.Media;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Theming;

namespace Md3.Gallery;

internal static class PickersPage
{
    public static Control Build()
    {
        return GalleryUi.Page(
            GalleryUi.Section("Inline fields", "Drop-in fields that format the selected value and open the matching picker.", Fields()),
            GalleryUi.Section("Embedded pickers", "Calendar grid, clock dial and color picker can be embedded in any layout.", Embedded()),
            GalleryUi.Section("Modal pickers", "Awaitable helpers return the selected value or null when dismissed.", Modal()));
    }

    private static Control Fields()
    {
        var date = new MdDatePickerField
        {
            Label = "Due date",
            IsClearable = true,
            SelectedDate = DateTime.Today.AddDays(3),
        };
        var range = new MdDateRangePickerField
        {
            Label = "Sprint",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(14),
        };
        var time = new MdTimePickerField
        {
            Label = "Reminder",
            Is24Hour = true,
            SelectedTime = new TimeSpan(9, 30, 0),
        };

        return GalleryUi.Showcase(
            "MdDatePickerField, MdDateRangePickerField and MdTimePickerField",
            "Inline fields keep the form compact and remain clearable and validatable with MdFormValidator.",
            GalleryUi.Column(date, range, time),
            "SelectedDate · StartDate/EndDate · SelectedTime · IsClearable");
    }

    private static Control Embedded()
    {
        var calendar = new MdCalendarGrid
        {
            SelectedDate = DateTime.Today,
        };

        var dial = new MdTimeDial
        {
            Is24Hour = false,
            Hour = 10,
            Minute = 15,
        };

        var color = new MdColorPicker
        {
            SelectedColor = Colors.Teal,
            Width = 320,
        };

        return GalleryUi.Showcase(
            "MdCalendarGrid, MdTimeDial and MdColorPicker",
            "Embed the picker surface directly when a dialog or sheet is not the right fit.",
            GalleryUi.Row(
                new MdCard(MdCardVariant.Outlined, calendar) { Width = 340 },
                new MdCard(MdCardVariant.Outlined, dial) { Width = 300 },
                new MdCard(MdCardVariant.Outlined, color) { Width = 360 }),
            "MdCalendarGrid.SelectedDate · MdTimeDial.Hour/Minute · MdColorPicker.SelectedColor");
    }

    private static Control Modal()
    {
        var result = new MdLabel
        {
            Text = "No selection yet",
            Role = MdTypeRole.BodySmall,
            Secondary = true,
        };

        var date = new MdButton("Pick date", MdButtonVariant.Outlined, MdIcons.Event);
        date.Clicked += async (_, _) =>
        {
            var picked = await MdDatePicker.PickDateAsync(DateTime.Today);
            result.Text = picked is null ? "Date dismissed" : $"Date: {picked:dd MMM yyyy}";
        };

        var range = new MdButton("Pick range", MdButtonVariant.Outlined, MdIcons.DateRange);
        range.Clicked += async (_, _) =>
        {
            var picked = await MdDatePicker.PickDateRangeAsync(start: DateTime.Today);
            result.Text = picked is null
                ? "Range dismissed"
                : $"Range: {picked.Value.Start:dd MMM} – {picked.Value.End:dd MMM}";
        };

        var time = new MdButton("Pick time", MdButtonVariant.Outlined, MdIcons.Schedule);
        time.Clicked += async (_, _) =>
        {
            var picked = await MdTimePicker.PickTimeAsync(new TimeSpan(9, 0, 0), is24Hour: true);
            result.Text = picked is null ? "Time dismissed" : $"Time: {picked:hh\\:mm}";
        };

        var color = new MdButton("Pick color", MdButtonVariant.Outlined, MdIcons.Palette);
        color.Clicked += async (_, _) =>
        {
            var picked = await MdColorPicker.PickAsync(color, Colors.Teal, "Accent color");
            result.Text = picked is null ? "Color dismissed" : $"Color: {picked}";
        };

        return GalleryUi.Showcase(
            "Modal date, time and color pickers",
            "Static helpers use the current top level and return null when the user dismisses the overlay.",
            GalleryUi.Column(GalleryUi.Row(date, range, time, color), result),
            "await MdDatePicker.PickDateAsync(...) · MdDatePicker.PickDateRangeAsync(...) · MdTimePicker.PickTimeAsync(...) · MdColorPicker.PickAsync(...)");
    }
}
