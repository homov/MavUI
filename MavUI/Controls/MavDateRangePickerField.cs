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
/// Inline text field that opens an <see cref="MavDatePicker"/> in date-range mode when tapped.
/// </summary>
public class MavDateRangePickerField : MavPickerFieldBase
{
    public static readonly StyledProperty<DateTime?> StartDateProperty =
        AvaloniaProperty.Register<MavDateRangePickerField, DateTime?>(
            nameof(StartDate),
            null,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> EndDateProperty =
        AvaloniaProperty.Register<MavDateRangePickerField, DateTime?>(
            nameof(EndDate),
            null,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> FormatProperty =
        AvaloniaProperty.Register<MavDateRangePickerField, string>(nameof(Format), "MMM d, yyyy");

    public static readonly StyledProperty<DateTime?> MinDateProperty =
        AvaloniaProperty.Register<MavDateRangePickerField, DateTime?>(nameof(MinDate));

    public static readonly StyledProperty<DateTime?> MaxDateProperty =
        AvaloniaProperty.Register<MavDateRangePickerField, DateTime?>(nameof(MaxDate));

    public event EventHandler<(DateTime? Start, DateTime? End)>? RangeChanged;
    public event EventHandler<(DateTime Start, DateTime End)>? RangeSelected;

    static MavDateRangePickerField()
    {
        StartDateProperty.Changed.AddClassHandler<MavDateRangePickerField>((f, _) => f.OnRangeChanged());
        EndDateProperty.Changed.AddClassHandler<MavDateRangePickerField>((f, _) => f.OnRangeChanged());
        FormatProperty.Changed.AddClassHandler<MavDateRangePickerField>((f, _) => f.SyncFieldText());
    }

    /// <summary>Creates a date range field labeled "Date Range".</summary>
    public MavDateRangePickerField() : this("Date Range")
    {
    }

    /// <summary>Creates a date range field with the given label, variant and icon.</summary>
    public MavDateRangePickerField(
        string label = "Date Range",
        DateTime? start = null,
        DateTime? end = null,
        MavTextFieldVariant variant = MavTextFieldVariant.Outlined)
        : base(label, variant, MavIcons.DateRange)
    {
        if (start is not null) SetValue(StartDateProperty, start);
        if (end is not null) SetValue(EndDateProperty, end);
        SyncFieldText();
    }

    public DateTime? StartDate
    {
        get => GetValue(StartDateProperty);
        set => SetValue(StartDateProperty, value);
    }

    public DateTime? EndDate
    {
        get => GetValue(EndDateProperty);
        set => SetValue(EndDateProperty, value);
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

    public string Format
    {
        get => GetValue(FormatProperty);
        set => SetValue(FormatProperty, string.IsNullOrWhiteSpace(value) ? "MMM d, yyyy" : value);
    }

    protected override bool HasSelectedValue => StartDate is not null || EndDate is not null;

    /// <summary>Clears both ends of the range.</summary>
    public override void Clear()
    {
        StartDate = null;
        EndDate = null;
    }

    /// <summary>Opens the range picker for this field and applies the picked range.</summary>
    protected override async Task OpenPickerAsync()
    {
        var picked = await MavDatePicker.PickRangeAsync(StartDate, EndDate, anchor: this, minDate: MinDate, maxDate: MaxDate);
        if (picked is { } r)
        {
            StartDate = r.Start;
            EndDate = r.End;
        }
    }

    /// <summary>Formats the selected range for display in the field.</summary>
    protected override string FormatDisplayValue()
    {
        if (StartDate is { } s && EndDate is { } e)
            return $"{s.ToString(Format, CultureInfo.CurrentUICulture)} {MavLocalization.DateRangeSeparator} {e.ToString(Format, CultureInfo.CurrentUICulture)}";
        if (StartDate is { } sOnly)
            return sOnly.ToString(Format, CultureInfo.CurrentUICulture);
        return string.Empty;
    }

    private void OnRangeChanged()
    {
        SyncFieldText();
        RangeChanged?.Invoke(this, (StartDate, EndDate));
        if (StartDate is { } s && EndDate is { } e)
            RangeSelected?.Invoke(this, (s, e));
    }
}
