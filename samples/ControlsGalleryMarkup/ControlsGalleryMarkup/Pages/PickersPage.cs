using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Theming;
using static Mav.ControlsGalleryMarkup.Pages.PageParts;

namespace Mav.ControlsGalleryMarkup.Pages;

/// <summary>Inline fields, embedded pickers and modal pickers. Mirrors the AXAML Pickers page.</summary>
public static class PickersPage
{
    public static Control Build()
    {
        var result = new MavLabel(L("Pickers.Modal.None", "No selection yet"), MavTypeRole.BodySmall).Secondary(true);

        var pickDate = new MavButton(L("Pickers.Modal.Date", "Pick date"), MavButtonVariant.Outlined, MavIcons.Event);
        pickDate.OnClicked(async () =>
        {
            var picked = await MavDatePicker.PickDateAsync(DateTime.Today);
            result.Text = picked is null
                ? MavLocalization.Get("Pickers.Result.DateDismissed", "Date dismissed")
                : MavLocalization.Format("Pickers.Result.Date", "Date: {0:dd MMM yyyy}", picked.Value);
        });

        var pickRange = new MavButton(L("Pickers.Modal.Range", "Pick range"), MavButtonVariant.Outlined, MavIcons.DateRange);
        pickRange.OnClicked(async () =>
        {
            var picked = await MavDatePicker.PickDateRangeAsync(start: DateTime.Today);
            result.Text = picked is null
                ? MavLocalization.Get("Pickers.Result.RangeDismissed", "Range dismissed")
                : MavLocalization.Format("Pickers.Result.Range", "Range: {0:dd MMM} – {1:dd MMM}",
                    picked.Value.Start, picked.Value.End);
        });

        var pickTime = new MavButton(L("Pickers.Modal.Time", "Pick time"), MavButtonVariant.Outlined, MavIcons.Schedule);
        pickTime.OnClicked(async () =>
        {
            var picked = await MavTimePicker.PickTimeAsync(new TimeSpan(9, 0, 0), is24Hour: true);
            result.Text = picked is null
                ? MavLocalization.Get("Pickers.Result.TimeDismissed", "Time dismissed")
                : MavLocalization.Format("Pickers.Result.Time", "Time: {0:hh\\:mm}", picked.Value);
        });

        var pickColor = new MavButton(L("Pickers.Modal.Color", "Pick color"), MavButtonVariant.Outlined, MavIcons.Palette);
        pickColor.OnClicked(async () =>
        {
            var picked = await MavColorPicker.PickAsync(pickColor, Colors.Teal,
                MavLocalization.Get("Pickers.Result.ColorTitle", "Accent color"));
            result.Text = picked is null
                ? MavLocalization.Get("Pickers.Result.ColorDismissed", "Color dismissed")
                : MavLocalization.Format("Pickers.Result.Color", "Color: {0}", picked.Value);
        });

        return Page(
            Section(
                "Pickers.Fields.Section", "Inline fields",
                "Pickers.Fields.Section.Body", "Drop-in fields that format the selected value and open the matching picker.",
                Card(
                    "MavDatePickerField, MavDateRangePickerField and MavTimePickerField",
                    "Pickers.Fields.Card.Body", "Inline fields keep the form compact and remain clearable and validatable with MavFormValidator.",
                    "SelectedDate · StartDate/EndDate · SelectedTime · IsClearable",
                    new MavDatePickerField()
                        .Label(L("Pickers.Field.Due", "Due date"))
                        .IsClearable(true)
                        .SelectedDate(DateTime.Today.AddDays(3)),
                    new MavDateRangePickerField()
                        .Label(L("Pickers.Field.Sprint", "Sprint"))
                        .StartDate(DateTime.Today)
                        .EndDate(DateTime.Today.AddDays(14)),
                    new MavTimePickerField()
                        .Label(L("Pickers.Field.Reminder", "Reminder"))
                        .Is24Hour(true)
                        .SelectedTime(new TimeSpan(9, 30, 0)))),
            Section(
                "Pickers.Embedded.Section", "Embedded pickers",
                "Pickers.Embedded.Section.Body", "Calendar grid, clock dial and color picker can be embedded in any layout.",
                Card(
                    "MavCalendarGrid, MavTimeDial and MavColorPicker",
                    "Pickers.Embedded.Card.Body", "Embed the picker surface directly when a dialog or sheet is not the right fit.",
                    "MavCalendarGrid.SelectedDate · MavTimeDial.Hour/Minute · MavColorPicker.SelectedColor",
                    Adaptive(300,
                        new MavCard(MavCardVariant.Outlined,
                                new MavCalendarGrid().SelectedDate(DateTime.Today))
                            .Margin(new Thickness(0, 0, 8, 8)),
                        new MavCard(MavCardVariant.Outlined,
                                new MavTimeDial().Is24Hour(false).Hour(10).Minute(15))
                            .Margin(new Thickness(0, 0, 8, 8)),
                        new MavCard(MavCardVariant.Outlined,
                                new MavColorPicker()
                                    .SelectedColor(Colors.Teal)
                                    .MaxWidth(320)
                                    .HorizontalAlignment(HorizontalAlignment.Left))
                            .Margin(new Thickness(0, 0, 8, 8))))),
            Section(
                "Pickers.Modal.Section", "Modal pickers",
                "Pickers.Modal.Section.Body", "Awaitable helpers return the selected value or null when dismissed.",
                Card(
                    "Modal date, time and color pickers",
                    "Pickers.Modal.Card.Body", "Static helpers use the current top level and return null when the user dismisses the overlay.",
                    "await MavDatePicker.PickDateAsync(...) · MavDatePicker.PickDateRangeAsync(...) · MavTimePicker.PickTimeAsync(...) · MavColorPicker.PickAsync(...)",
                    Row(pickDate, pickRange, pickTime, pickColor),
                    result)));
    }
}
