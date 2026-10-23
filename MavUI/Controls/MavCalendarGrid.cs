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
/// Material Design 3 inline Calendar Grid with month navigation, 3-column year selector,
/// single-date selection, and continuous range selection highlight.
/// </summary>
public class MavCalendarGrid : MavContentView
{
    public static readonly StyledProperty<bool> IsRangeProperty =
        AvaloniaProperty.Register<MavCalendarGrid, bool>(nameof(IsRange));

    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        AvaloniaProperty.Register<MavCalendarGrid, DateTime?>(nameof(SelectedDate), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> RangeStartProperty =
        AvaloniaProperty.Register<MavCalendarGrid, DateTime?>(nameof(RangeStart), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> RangeEndProperty =
        AvaloniaProperty.Register<MavCalendarGrid, DateTime?>(nameof(RangeEnd), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> MinDateProperty =
        AvaloniaProperty.Register<MavCalendarGrid, DateTime?>(nameof(MinDate));

    public static readonly StyledProperty<DateTime?> MaxDateProperty =
        AvaloniaProperty.Register<MavCalendarGrid, DateTime?>(nameof(MaxDate));

    private DateTime _displayMonth;
    private DateTime? _selectedDate;
    private DateTime? _rangeStart;
    private DateTime? _rangeEnd;
    private DateTime? _minDate;
    private DateTime? _maxDate;
    private Func<DateTime, bool>? _selectableDayPredicate;
    private bool _isRange;
    private bool _yearPickerOpen;

    private readonly TextBlock _monthBtnLabel;
    private readonly MavIcon _monthChevron;
    private readonly MavIconButton _prevBtn;
    private readonly MavIconButton _nextBtn;
    private readonly Grid _weekdayRow;
    private readonly Grid _daysGrid;
    private readonly ScrollViewer _yearScroll;
    private readonly Grid _yearGrid;

    public event EventHandler<DateTime>? DateSelected;
    public event EventHandler<(DateTime? Start, DateTime? End)>? RangeChanged;

    static MavCalendarGrid()
    {
        IsRangeProperty.Changed.AddClassHandler<MavCalendarGrid>((g, e) =>
        {
            g._isRange = e.GetNewValue<bool>();
            g.RebuildDays();
        });
        SelectedDateProperty.Changed.AddClassHandler<MavCalendarGrid>((g, e) =>
        {
            var val = e.GetNewValue<DateTime?>();
            g._selectedDate = val?.Date;
            if (val is { } dt)
                g._displayMonth = new DateTime(dt.Year, dt.Month, 1);
            g.RebuildDays();
        });
        RangeStartProperty.Changed.AddClassHandler<MavCalendarGrid>((g, e) =>
        {
            g._rangeStart = e.GetNewValue<DateTime?>()?.Date;
            g.RebuildDays();
        });
        RangeEndProperty.Changed.AddClassHandler<MavCalendarGrid>((g, e) =>
        {
            g._rangeEnd = e.GetNewValue<DateTime?>()?.Date;
            g.RebuildDays();
        });
        MinDateProperty.Changed.AddClassHandler<MavCalendarGrid>((g, e) =>
        {
            g._minDate = e.GetNewValue<DateTime?>()?.Date;
            g.RebuildDays();
        });
        MaxDateProperty.Changed.AddClassHandler<MavCalendarGrid>((g, e) =>
        {
            g._maxDate = e.GetNewValue<DateTime?>()?.Date;
            g.RebuildDays();
        });
    }

    /// <summary>Creates a calendar grid that shows the current month.</summary>
    public MavCalendarGrid() : this(null, false)
    {
    }

    /// <summary>Creates a calendar grid that shows the month of <paramref name="initialDate"/>, optionally in range mode.</summary>
    public MavCalendarGrid(DateTime? initialDate = null, bool isRange = false)
    {
        _isRange = isRange;
        var seed = initialDate ?? DateTime.Today;
        _displayMonth = new DateTime(seed.Year, seed.Month, 1);
        if (!isRange && initialDate is not null)
            _selectedDate = initialDate.Value.Date;
        else if (isRange && initialDate is not null)
            _rangeStart = initialDate.Value.Date;

        _monthBtnLabel = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        _monthChevron = new MavIcon(MavIcons.ArrowDropDown, 20) { UseThemeDefaultColor = false };

        var monthBtnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
        };
        monthBtnRow.Children.Add(_monthBtnLabel);
        monthBtnRow.Children.Add(_monthChevron);

        var monthBtn = new MavInkSurface
        {
            Height = 36,
            CornerRadius = 18,
            Padding = new Thickness(12, 0, 8, 0),
            Child = monthBtnRow,
        };

        _prevBtn = new MavIconButton(MavIcons.ChevronLeft);
        _prevBtn.Clicked += (_, _) =>
        {
            _displayMonth = _displayMonth.AddMonths(-1);
            RebuildDays();
        };

        _nextBtn = new MavIconButton(MavIcons.ChevronRight);
        _nextBtn.Clicked += (_, _) =>
        {
            _displayMonth = _displayMonth.AddMonths(1);
            RebuildDays();
        };

        var navRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
            Margin = new Thickness(4, 0, 4, 8),
        };
        navRow.Add(monthBtn, 0, 0);
        navRow.Add(_prevBtn, 2, 0);
        navRow.Add(_nextBtn, 3, 0);

        _weekdayRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*"),
            Margin = new Thickness(0, 0, 0, 4),
        };

        _daysGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto"),
            RowSpacing = 2,
        };

        _yearGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*"),
            RowSpacing = 6,
            ColumnSpacing = 6,
            Margin = new Thickness(4),
        };
        _yearScroll = new MavScrollViewer
        {
            Height = 240,
            IsVisible = false,
            Content = _yearGrid,
        };

        monthBtn.Clicked += (_, _) =>
        {
            _yearPickerOpen = !_yearPickerOpen;
            _monthChevron.SetRotation(_yearPickerOpen ? 180 : 0);
            _prevBtn.IsVisible = !_yearPickerOpen;
            _nextBtn.IsVisible = !_yearPickerOpen;
            _weekdayRow.IsVisible = !_yearPickerOpen;
            _daysGrid.IsVisible = !_yearPickerOpen;
            _yearScroll.IsVisible = _yearPickerOpen;
            if (_yearPickerOpen) RebuildYears();
        };

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
        };
        stack.Children.Add(navRow);
        stack.Children.Add(_weekdayRow);
        stack.Children.Add(_daysGrid);
        stack.Children.Add(_yearScroll);

        Child = stack;
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public bool IsRange
    {
        get => GetValue(IsRangeProperty);
        set => SetValue(IsRangeProperty, value);
    }

    public DateTime? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value?.Date);
    }

    public DateTime? RangeStart
    {
        get => GetValue(RangeStartProperty);
        set => SetValue(RangeStartProperty, value?.Date);
    }

    public DateTime? RangeEnd
    {
        get => GetValue(RangeEndProperty);
        set => SetValue(RangeEndProperty, value?.Date);
    }

    public DateTime? MinDate
    {
        get => GetValue(MinDateProperty);
        set => SetValue(MinDateProperty, value?.Date);
    }

    public DateTime? MaxDate
    {
        get => GetValue(MaxDateProperty);
        set => SetValue(MaxDateProperty, value?.Date);
    }

    public Func<DateTime, bool>? SelectableDayPredicate
    {
        get => _selectableDayPredicate;
        set
        {
            _selectableDayPredicate = value;
            RebuildDays();
        }
    }

    private bool IsDaySelectable(DateTime date)
    {
        var d = date.Date;
        if (_minDate is { } min && d < min)
            return false;
        if (_maxDate is { } max && d > max)
            return false;
        if (_selectableDayPredicate is not null && !_selectableDayPredicate(d))
            return false;
        return true;
    }

    private void RebuildWeekdays(MavTheme t)
    {
        _weekdayRow.Children.Clear();
        var names = CultureInfo.CurrentUICulture.DateTimeFormat.AbbreviatedDayNames;
        var firstDay = (int)CultureInfo.CurrentUICulture.DateTimeFormat.FirstDayOfWeek;
        for (int i = 0; i < 7; i++)
        {
            var dayName = names[(firstDay + i) % 7];
            var shortStr = string.IsNullOrEmpty(dayName) ? "?" : dayName[..1].ToUpperInvariant();
            var lbl = new TextBlock
            {
                Text = shortStr,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 4),
            };
            lbl.Apply(MavTypeRole.BodySmall, t.Typography);
            lbl.Foreground = MavTheme.BrushOf(t.Color.OnSurfaceVariant);
            _weekdayRow.Add(lbl, i, 0);
        }
    }

    private void RebuildDays()
    {
        var t = MavTheme.Current;
        var c = t.Color;
        _monthBtnLabel.Text = _displayMonth.ToString("MMMM yyyy", CultureInfo.CurrentUICulture);
        _monthBtnLabel.Apply(MavTypeRole.LabelLarge, t.Typography);
        _monthBtnLabel.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);
        _monthChevron.Color = c.OnSurfaceVariant;

        _daysGrid.Children.Clear();

        var firstDayOfWeek = (int)CultureInfo.CurrentUICulture.DateTimeFormat.FirstDayOfWeek;
        var monthStartDow = (int)_displayMonth.DayOfWeek;
        var offset = (monthStartDow - firstDayOfWeek + 7) % 7;
        var daysInMonth = DateTime.DaysInMonth(_displayMonth.Year, _displayMonth.Month);
        var today = DateTime.Today;

        for (int day = 1; day <= daysInMonth; day++)
        {
            var cellIdx = offset + day - 1;
            var col = cellIdx % 7;
            var row = cellIdx / 7;
            var dt = new DateTime(_displayMonth.Year, _displayMonth.Month, day);

            var isToday = dt == today;
            var isSelectable = IsDaySelectable(dt);
            var isSingleSel = !_isRange && _selectedDate == dt;
            var isStart = _isRange && _rangeStart == dt;
            var isEnd = _isRange && _rangeEnd == dt;
            var inBetween = _isRange && _rangeStart is { } rs && _rangeEnd is { } re && dt > rs && dt < re;
            var isEndpoint = isSingleSel || isStart || isEnd;

            var cellHost = new Grid
            {
                Height = 40,
            };

            if (inBetween || (isStart && _rangeEnd is not null) || (isEnd && _rangeStart is not null))
            {
                var band = new Border
                {
                    Height = 36,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = MavTheme.BrushOf(c.SecondaryContainer),
                    Margin = isStart && _rangeEnd is not null && _rangeStart != _rangeEnd
                        ? new Thickness(18, 0, 0, 0)
                        : isEnd && _rangeStart is not null && _rangeStart != _rangeEnd
                            ? new Thickness(0, 0, 18, 0)
                            : new Thickness(0),
                };
                cellHost.Children.Add(band);
            }

            var lbl = new TextBlock
            {
                Text = day.ToString(CultureInfo.InvariantCulture),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            lbl.Apply(MavTypeRole.BodyMedium, t.Typography);
            if (isEndpoint || isToday)
                lbl.FontWeight = FontWeight.SemiBold;

            var fg = !isSelectable
                ? c.OnSurface.WithAlpha(t.StateLayer.DisabledContent)
                : isEndpoint
                    ? c.OnPrimary
                    : inBetween
                        ? c.OnSecondaryContainer
                        : isToday
                            ? c.Primary
                            : c.OnSurface;
            lbl.Foreground = MavTheme.BrushOf(fg);

            var circle = new MavInkSurface
            {
                Width = 36,
                Height = 36,
                CornerRadius = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsEnabled = isSelectable,
                IsInteractive = isSelectable,
                FillColor = isEndpoint ? c.Primary : Colors.Transparent,
                StrokeColor = isToday && !isEndpoint ? c.Primary : Colors.Transparent,
                StrokeThickness = isToday && !isEndpoint ? 1 : 0,
                RippleColor = isEndpoint ? c.OnPrimary : c.Primary,
                Child = lbl,
            };
            var captured = dt;
            circle.Clicked += (_, _) => OnDayClicked(captured);

            cellHost.Children.Add(circle);
            _daysGrid.Add(cellHost, col, row);
        }
    }

    private void RebuildYears()
    {
        var t = MavTheme.Current;
        var c = t.Color;
        _yearGrid.Children.Clear();
        _yearGrid.RowDefinitions.Clear();

        var curYear = DateTime.Today.Year;
        var startYear = _minDate?.Year ?? (curYear - 12);
        var endYear = _maxDate?.Year ?? (curYear + 11);
        var totalYears = Math.Max(1, endYear - startYear + 1);
        var rows = (totalYears + 2) / 3;
        for (int r = 0; r < rows; r++)
            _yearGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        for (int i = 0; i < totalYears; i++)
        {
            var yr = startYear + i;
            var sel = yr == _displayMonth.Year;
            var lbl = new TextBlock
            {
                Text = yr.ToString(CultureInfo.InvariantCulture),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            lbl.Apply(MavTypeRole.BodyLarge, t.Typography);
            lbl.Foreground = MavTheme.BrushOf(sel ? c.OnPrimary : c.OnSurfaceVariant);

            var pill = new MavInkSurface
            {
                Height = 36,
                CornerRadius = 18,
                FillColor = sel ? c.Primary : Colors.Transparent,
                RippleColor = c.Primary,
                Child = lbl,
            };
            var capturedYr = yr;
            pill.Clicked += (_, _) =>
            {
                _displayMonth = new DateTime(capturedYr, _displayMonth.Month, 1);
                _yearPickerOpen = false;
                _monthChevron.SetRotation(0);
                _prevBtn.IsVisible = true;
                _nextBtn.IsVisible = true;
                _weekdayRow.IsVisible = true;
                _daysGrid.IsVisible = true;
                _yearScroll.IsVisible = false;
                RebuildDays();
            };
            _yearGrid.Add(pill, i % 3, i / 3);
        }
    }

    private void OnDayClicked(DateTime date)
    {
        if (!IsDaySelectable(date))
            return;

        if (!_isRange)
        {
            _selectedDate = date;
            SetValue(SelectedDateProperty, date);
            RebuildDays();
            DateSelected?.Invoke(this, date);
            return;
        }

        if (_rangeStart is not { } start || _rangeEnd is not null)
        {
            _rangeStart = date;
            _rangeEnd = null;
        }
        else if (date < start)
        {
            _rangeEnd = start;
            _rangeStart = date;
        }
        else
        {
            _rangeEnd = date;
        }

        SetValue(RangeStartProperty, _rangeStart);
        SetValue(RangeEndProperty, _rangeEnd);
        RebuildDays();
        RangeChanged?.Invoke(this, (_rangeStart, _rangeEnd));
    }

    private void ApplyTheme(MavTheme t)
    {
        RebuildWeekdays(t);
        RebuildDays();
        if (_yearPickerOpen) RebuildYears();
    }
}
