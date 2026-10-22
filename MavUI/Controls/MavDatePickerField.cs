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
/// Inline text field that opens an <see cref="MavDatePicker"/> dialog when tapped.
/// </summary>
public class MavDatePickerField : MavPickerFieldBase
{
    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        AvaloniaProperty.Register<MavDatePickerField, DateTime?>(
            nameof(SelectedDate),
            null,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> FormatProperty =
        AvaloniaProperty.Register<MavDatePickerField, string>(nameof(Format), "yyyy-MM-dd");

    public static readonly StyledProperty<DateTime?> MinDateProperty =
        AvaloniaProperty.Register<MavDatePickerField, DateTime?>(nameof(MinDate));

    public static readonly StyledProperty<DateTime?> MaxDateProperty =
        AvaloniaProperty.Register<MavDatePickerField, DateTime?>(nameof(MaxDate));

    public event EventHandler<DateTime>? DateSelected;
    public event EventHandler<DateTime?>? SelectedDateChanged;

    static MavDatePickerField()
    {
        SelectedDateProperty.Changed.AddClassHandler<MavDatePickerField>((f, e) =>
            f.OnSelectedDateChanged(e.GetNewValue<DateTime?>()));
        FormatProperty.Changed.AddClassHandler<MavDatePickerField>((f, _) => f.SyncFieldText());
    }

    /// <summary>Creates a date field labeled "Date".</summary>
    public MavDatePickerField() : this("Date")
    {
    }

    /// <summary>Creates a date field with the given label, variant and icon.</summary>
    public MavDatePickerField(
        string label = "Date",
        DateTime? initial = null,
        MavTextFieldVariant variant = MavTextFieldVariant.Outlined)
        : base(label, variant, MavIcons.CalendarToday)
    {
        if (initial is not null)
            SetValue(SelectedDateProperty, initial);
        SyncFieldText();
    }

    public DateTime? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public DateTime? MinDate
    {
        get => GetValue(MinDateProperty);
        set => SetValue(MinDateProperty, value);
    }

    public DateTime? MaxDate
    {
        get => GetValue(MaxDateProperty);
        set => SetValue(MaxDateProperty, value);
    }

    public Func<DateTime, bool>? SelectableDayPredicate { get; set; }

    public string Format
    {
        get => GetValue(FormatProperty);
        set => SetValue(FormatProperty, string.IsNullOrWhiteSpace(value) ? "yyyy-MM-dd" : value);
    }

    protected override bool HasSelectedValue => SelectedDate is not null;

    public override void Clear() => SelectedDate = null;

    /// <summary>Opens the date picker for this field and applies the picked date.</summary>
    protected override async Task OpenPickerAsync()
    {
        var picked = await MavDatePicker.PickAsync(SelectedDate, anchor: this, minDate: MinDate, maxDate: MaxDate, selectableDayPredicate: SelectableDayPredicate);
        if (picked is { } dt)
            SelectedDate = dt;
    }

    /// <summary>Formats the selected date for display in the field.</summary>
    protected override string FormatDisplayValue() =>
        SelectedDate?.ToString(Format, CultureInfo.CurrentUICulture) ?? string.Empty;

    private void OnSelectedDateChanged(DateTime? dt)
    {
        SyncFieldText();
        SelectedDateChanged?.Invoke(this, dt);
        if (dt is { } val)
            DateSelected?.Invoke(this, val);
    }
}
