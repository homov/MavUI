using Avalonia.Controls;
using Avalonia.Media;
using Mav;
using Mav.Controls;

namespace Mav.ControlsGallery.Pages;

public partial class PickersPage : UserControl
{
    public PickersPage()
    {
        InitializeComponent();

        DueDate.SelectedDate = DateTime.Today.AddDays(3);
        Sprint.StartDate = DateTime.Today;
        Sprint.EndDate = DateTime.Today.AddDays(14);
        Reminder.Is24Hour = true;
        Reminder.SelectedTime = new TimeSpan(9, 30, 0);

        Calendar.SelectedDate = DateTime.Today;
        Dial.Is24Hour = false;
        Dial.Hour = 10;
        Dial.Minute = 15;
        ColorPicker.SelectedColor = Colors.Teal;

        PickDateButton.Clicked += async (_, _) =>
        {
            var picked = await MavDatePicker.PickDateAsync(DateTime.Today);
            PickResult.Text = picked is null
                ? MavLocalization.Get("Pickers.Result.DateDismissed", "Date dismissed")
                : MavLocalization.Format("Pickers.Result.Date", "Date: {0:dd MMM yyyy}", picked.Value);
        };

        PickRangeButton.Clicked += async (_, _) =>
        {
            var picked = await MavDatePicker.PickDateRangeAsync(start: DateTime.Today);
            PickResult.Text = picked is null
                ? MavLocalization.Get("Pickers.Result.RangeDismissed", "Range dismissed")
                : MavLocalization.Format("Pickers.Result.Range", "Range: {0:dd MMM} – {1:dd MMM}",
                    picked.Value.Start, picked.Value.End);
        };

        PickTimeButton.Clicked += async (_, _) =>
        {
            var picked = await MavTimePicker.PickTimeAsync(new TimeSpan(9, 0, 0), is24Hour: true);
            PickResult.Text = picked is null
                ? MavLocalization.Get("Pickers.Result.TimeDismissed", "Time dismissed")
                : MavLocalization.Format("Pickers.Result.Time", "Time: {0:hh\\:mm}", picked.Value);
        };

        PickColorButton.Clicked += async (_, _) =>
        {
            var picked = await MavColorPicker.PickAsync(PickColorButton, Colors.Teal,
                MavLocalization.Get("Pickers.Result.ColorTitle", "Accent color"));
            PickResult.Text = picked is null
                ? MavLocalization.Get("Pickers.Result.ColorDismissed", "Color dismissed")
                : MavLocalization.Format("Pickers.Result.Color", "Color: {0}", picked.Value);
        };
    }
}
