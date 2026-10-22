using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Inline text field that opens an <see cref="MavTimePicker"/> dialog when tapped.
/// </summary>
public class MavTimePickerField : MavPickerFieldBase
{
    public static readonly StyledProperty<TimeSpan?> SelectedTimeProperty =
        AvaloniaProperty.Register<MavTimePickerField, TimeSpan?>(
            nameof(SelectedTime),
            null,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> Is24HourProperty =
        AvaloniaProperty.Register<MavTimePickerField, bool>(nameof(Is24Hour));

    public event EventHandler<TimeSpan>? TimeSelected;
    public event EventHandler<TimeSpan?>? SelectedTimeChanged;

    static MavTimePickerField()
    {
        SelectedTimeProperty.Changed.AddClassHandler<MavTimePickerField>((f, e) =>
            f.OnSelectedTimeChanged(e.GetNewValue<TimeSpan?>()));
        Is24HourProperty.Changed.AddClassHandler<MavTimePickerField>((f, _) => f.SyncFieldText());
    }

    /// <summary>Creates a time field labeled "Time".</summary>
    public MavTimePickerField() : this("Time")
    {
    }

    /// <summary>Creates a time field with the given label, variant and icon.</summary>
    public MavTimePickerField(
        string label = "Time",
        TimeSpan? initial = null,
        bool is24Hour = false,
        MavTextFieldVariant variant = MavTextFieldVariant.Outlined)
        : base(label, variant, MavIcons.Schedule)
    {
        Is24Hour = is24Hour;
        if (initial is not null)
            SetValue(SelectedTimeProperty, initial);
        SyncFieldText();
    }

    public TimeSpan? SelectedTime
    {
        get => GetValue(SelectedTimeProperty);
        set => SetValue(SelectedTimeProperty, value);
    }

    public bool Is24Hour
    {
        get => GetValue(Is24HourProperty);
        set => SetValue(Is24HourProperty, value);
    }

    protected override bool HasSelectedValue => SelectedTime is not null;

    /// <summary>
    /// Clears the selected time.
    /// </summary>
    public override void Clear() => SelectedTime = null;

    /// <summary>Opens the time picker for this field and applies the picked time.</summary>
    protected override async Task OpenPickerAsync()
    {
        var picked = await MavTimePicker.PickAsync(SelectedTime, Is24Hour, anchor: this);
        if (picked is { } tm)
            SelectedTime = tm;
    }

    /// <summary>Formats the selected time for display in the field, respecting the 12 or 24-hour setting.</summary>
    protected override string FormatDisplayValue()
    {
        if (SelectedTime is not { } tm)
            return string.Empty;

        if (Is24Hour)
            return $"{tm.Hours:00}:{tm.Minutes:00}";

        var h12 = tm.Hours % 12 == 0 ? 12 : tm.Hours % 12;
        var ampm = tm.Hours >= 12 ? MavLocalization.Pm : MavLocalization.Am;
        return $"{h12:00}:{tm.Minutes:00} {ampm}";
    }

    private void OnSelectedTimeChanged(TimeSpan? tm)
    {
        SyncFieldText();
        SelectedTimeChanged?.Invoke(this, tm);
        if (tm is { } val)
            TimeSelected?.Invoke(this, val);
    }
}
