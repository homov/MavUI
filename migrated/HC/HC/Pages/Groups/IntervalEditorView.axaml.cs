using Avalonia.Controls;
using Mav;
using Mav.Controls;

namespace Hc.Pages.Groups;

/// <summary>Edits one schedule interval: hours, value, and the device state. Raises <see cref="Applied"/> on Apply.</summary>
public partial class IntervalEditorView : UserControl
{
    public IntervalEditorView()
    {
        InitializeComponent();
        ApplyButton.Clicked += (_, _) => Applied?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Applied;

    /// <summary>Fills the fields for the type. A null interval means a new one for the whole day.</summary>
    public void Configure(ScheduleDraft draft, ScheduleInterval? interval)
    {
        ValueBox.Minimum = draft.MinimumValue;
        ValueBox.Maximum = draft.MaximumValue;
        ValueBox.IsVisible = draft.HasValue;
        OnLabel.IsVisible = draft.HasIntervals;
        OnSwitch.IsVisible = draft.HasIntervals;

        StartBox.Value = interval?.StartHour ?? 0;
        EndBox.Value = interval?.EndHour ?? ScheduleDraft.HoursPerDay;
        ValueBox.Value = interval?.Value ?? draft.DefaultValue;
        OnSwitch.IsToggled = interval?.IsDeviceOn ?? true;
    }

    /// <summary>The values in the fields. The value is null when the type has none.</summary>
    public (int Start, int End, int? Value, bool IsDeviceOn) Read()
    {
        int? value = ValueBox.IsVisible ? (int)ValueBox.Value : null;
        return ((int)StartBox.Value, (int)EndBox.Value, value, OnSwitch.IsToggled);
    }

    public void ShowError(string message)
    {
        ErrorBanner.Message = message;
        ErrorBanner.Show();
    }
}
