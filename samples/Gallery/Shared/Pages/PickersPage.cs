using Avalonia.Controls;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Theming;

namespace Mav.Gallery;

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
        var date = new MavDatePickerField
        {
            Label = "Due date",
            IsClearable = true,
            SelectedDate = DateTime.Today.AddDays(3),
        };
        var range = new MavDateRangePickerField
        {
            Label = "Sprint",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(14),
        };
        var time = new MavTimePickerField
        {
            Label = "Reminder",
            Is24Hour = true,
            SelectedTime = new TimeSpan(9, 30, 0),
        };

        return GalleryUi.Showcase(
            "MavDatePickerField, MavDateRangePickerField and MavTimePickerField",
            "Inline fields keep the form compact and remain clearable and validatable with MavFormValidator.",
            GalleryUi.Column(date, range, time),
            "SelectedDate · StartDate/EndDate · SelectedTime · IsClearable");
    }

    private static Control Embedded()
    {
        var calendar = new MavCalendarGrid
        {
            SelectedDate = DateTime.Today,
        };

        var dial = new MavTimeDial
        {
            Is24Hour = false,
            Hour = 10,
            Minute = 15,
        };

        var color = new MavColorPicker
        {
            SelectedColor = Colors.Teal,
            Width = 320,
        };

        return GalleryUi.Showcase(
            "MavCalendarGrid, MavTimeDial and MavColorPicker",
            "Embed the picker surface directly when a dialog or sheet is not the right fit.",
            GalleryUi.Row(
                new MavCard(MavCardVariant.Outlined, calendar) { Width = 340 },
                new MavCard(MavCardVariant.Outlined, dial) { Width = 300 },
                new MavCard(MavCardVariant.Outlined, color) { Width = 360 }),
            "MavCalendarGrid.SelectedDate · MavTimeDial.Hour/Minute · MavColorPicker.SelectedColor");
    }

    private static Control Modal()
    {
        var result = new MavLabel
        {
            Text = "No selection yet",
            Role = MavTypeRole.BodySmall,
            Secondary = true,
        };

        var date = new MavButton("Pick date", MavButtonVariant.Outlined, MavIcons.Event);
        date.Clicked += async (_, _) =>
        {
            var picked = await MavDatePicker.PickDateAsync(DateTime.Today);
            result.Text = picked is null ? "Date dismissed" : $"Date: {picked:dd MMM yyyy}";
        };

        var range = new MavButton("Pick range", MavButtonVariant.Outlined, MavIcons.DateRange);
        range.Clicked += async (_, _) =>
        {
            var picked = await MavDatePicker.PickDateRangeAsync(start: DateTime.Today);
            result.Text = picked is null
                ? "Range dismissed"
                : $"Range: {picked.Value.Start:dd MMM} – {picked.Value.End:dd MMM}";
        };

        var time = new MavButton("Pick time", MavButtonVariant.Outlined, MavIcons.Schedule);
        time.Clicked += async (_, _) =>
        {
            var picked = await MavTimePicker.PickTimeAsync(new TimeSpan(9, 0, 0), is24Hour: true);
            result.Text = picked is null ? "Time dismissed" : $"Time: {picked:hh\\:mm}";
        };

        var color = new MavButton("Pick color", MavButtonVariant.Outlined, MavIcons.Palette);
        color.Clicked += async (_, _) =>
        {
            var picked = await MavColorPicker.PickAsync(color, Colors.Teal, "Accent color");
            result.Text = picked is null ? "Color dismissed" : $"Color: {picked}";
        };

        return GalleryUi.Showcase(
            "Modal date, time and color pickers",
            "Static helpers use the current top level and return null when the user dismisses the overlay.",
            GalleryUi.Column(GalleryUi.Row(date, range, time, color), result),
            "await MavDatePicker.PickDateAsync(...) · MavDatePicker.PickDateRangeAsync(...) · MavTimePicker.PickTimeAsync(...) · MavColorPicker.PickAsync(...)");
    }
}
