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

    public MavCalendarGrid() : this(null, false)
    {
    }

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
            Content = monthBtnRow,
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

        Content = stack;
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
                Content = lbl,
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
                Content = lbl,
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

/// <summary>
/// Material Design 3 Modal Date Picker Dialog (Single date or Date range, with calendar and text input modes).
/// </summary>
public class MavDatePicker : Control
{
    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        AvaloniaProperty.Register<MavDatePicker, DateTime?>(nameof(SelectedDate), DateTime.Today, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> RangeStartProperty =
        AvaloniaProperty.Register<MavDatePicker, DateTime?>(nameof(RangeStart), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> RangeEndProperty =
        AvaloniaProperty.Register<MavDatePicker, DateTime?>(nameof(RangeEnd), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> MinDateProperty =
        AvaloniaProperty.Register<MavDatePicker, DateTime?>(nameof(MinDate));

    public static readonly StyledProperty<DateTime?> MaxDateProperty =
        AvaloniaProperty.Register<MavDatePicker, DateTime?>(nameof(MaxDate));

    public static readonly StyledProperty<bool> IsRangeProperty =
        AvaloniaProperty.Register<MavDatePicker, bool>(nameof(IsRange));

    public static readonly StyledProperty<bool> StartInInputModeProperty =
        AvaloniaProperty.Register<MavDatePicker, bool>(nameof(StartInInputMode));

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MavDatePicker, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    private IDisposable? _handle;
    private bool _syncingIsOpen;

    static MavDatePicker()
    {
        IsOpenProperty.Changed.AddClassHandler<MavDatePicker>((p, e) =>
        {
            if (p._syncingIsOpen) return;
            if (e.GetNewValue<bool>())
                p.Show(p.Parent as Visual ?? p);
            else
                p.Close();
        });
    }

    protected override Size MeasureOverride(Size availableSize) => default;

    public DateTime? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public DateTime? RangeStart
    {
        get => GetValue(RangeStartProperty);
        set => SetValue(RangeStartProperty, value);
    }

    public DateTime? RangeEnd
    {
        get => GetValue(RangeEndProperty);
        set => SetValue(RangeEndProperty, value);
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

    public bool IsRange
    {
        get => GetValue(IsRangeProperty);
        set => SetValue(IsRangeProperty, value);
    }

    public bool StartInInputMode
    {
        get => GetValue(StartInInputModeProperty);
        set => SetValue(StartInInputModeProperty, value);
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public event EventHandler<DateTime>? DateSelected;
    public event EventHandler<(DateTime Start, DateTime End)>? RangeSelected;
    public event EventHandler? Dismissed;

    public void Show(Visual? anchor = null)
    {
        Close();

        var t = MavTheme.Current;
        var c = t.Color;
        var inputMode = StartInInputMode;

        var overline = new TextBlock
        {
            Text = (IsRange ? MavLocalization.SelectDateRange : MavLocalization.SelectDate).ToUpperInvariant(),
        };
        overline.Apply(MavTypeRole.LabelMedium, t.Typography);
        overline.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);

        var headline = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };
        headline.Apply(MavTypeRole.HeadlineMedium, t.Typography);
        headline.Foreground = MavTheme.BrushOf(c.OnSurface);

        var cal = new MavCalendarGrid(IsRange ? (RangeStart ?? DateTime.Today) : (SelectedDate ?? DateTime.Today), IsRange)
        {
            MinDate = MinDate,
            MaxDate = MaxDate,
            SelectableDayPredicate = SelectableDayPredicate,
            RangeStart = RangeStart,
            RangeEnd = RangeEnd,
            SelectedDate = SelectedDate,
            IsVisible = !inputMode,
        };

        var singleInput = new MavTextField(MavLocalization.EnterDate, MavTextFieldVariant.Outlined, MavLocalization.DateFieldHint)
        {
            Text = (SelectedDate ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        };
        var startInput = new MavTextField(MavLocalization.StartDate, MavTextFieldVariant.Outlined, MavLocalization.DateFieldHint)
        {
            Text = RangeStart?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
        };
        var endInput = new MavTextField(MavLocalization.EndDate, MavTextFieldVariant.Outlined, MavLocalization.DateFieldHint)
        {
            Text = RangeEnd?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
        };

        var inputBox = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 12,
            Margin = new Thickness(0, 8),
            IsVisible = inputMode,
        };
        if (IsRange)
        {
            inputBox.Children.Add(startInput);
            inputBox.Children.Add(endInput);
        }
        else
        {
            inputBox.Children.Add(singleInput);
        }

        void UpdateHeadline()
        {
            if (!IsRange)
            {
                headline.Text = (cal.SelectedDate ?? DateTime.Today).ToString("ddd, MMM d", CultureInfo.CurrentUICulture);
            }
            else
            {
                var s = cal.RangeStart?.ToString("MMM d", CultureInfo.CurrentUICulture) ?? MavLocalization.StartDate;
                var e = cal.RangeEnd?.ToString("MMM d", CultureInfo.CurrentUICulture) ?? MavLocalization.EndDate;
                headline.Text = $"{s} {MavLocalization.DateRangeSeparator} {e}";
            }
        }

        cal.DateSelected += (_, dt) =>
        {
            SelectedDate = dt;
            singleInput.Text = dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            UpdateHeadline();
        };
        cal.RangeChanged += (_, r) =>
        {
            RangeStart = r.Start;
            RangeEnd = r.End;
            if (r.Start is { } rs) startInput.Text = rs.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (r.End is { } re) endInput.Text = re.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            UpdateHeadline();
        };

        var modeBtn = new MavIconButton(inputMode ? MavIcons.CalendarToday : MavIcons.Edit);
        modeBtn.Clicked += (_, _) =>
        {
            inputMode = !inputMode;
            modeBtn.Icon = inputMode ? MavIcons.CalendarToday : MavIcons.Edit;
            cal.IsVisible = !inputMode;
            inputBox.IsVisible = inputMode;
        };

        var headerRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(0, 4, 0, 8),
        };
        headerRow.Add(headline, 0, 0);
        headerRow.Add(modeBtn, 1, 0);

        var cancelBtn = new MavButton(MavLocalization.Cancel, MavButtonVariant.Text);
        cancelBtn.Clicked += (_, _) =>
        {
            Dismissed?.Invoke(this, EventArgs.Empty);
            Close();
        };

        var okBtn = new MavButton(MavLocalization.Ok, MavButtonVariant.Text);
        okBtn.Clicked += (_, _) =>
        {
            if (inputMode)
            {
                if (!IsRange)
                {
                    if (!DateTime.TryParse(singleInput.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    {
                        singleInput.ErrorText = MavLocalization.InvalidDate;
                        return;
                    }
                    SelectedDate = parsed.Date;
                }
                else
                {
                    if (!DateTime.TryParse(startInput.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var ps) ||
                        !DateTime.TryParse(endInput.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var pe) ||
                        pe.Date < ps.Date)
                    {
                        endInput.ErrorText = MavLocalization.InvalidRange;
                        return;
                    }
                    RangeStart = ps.Date;
                    RangeEnd = pe.Date;
                }
            }

            if (IsRange)
            {
                var rs = RangeStart ?? DateTime.Today;
                var re = RangeEnd ?? rs;
                RangeSelected?.Invoke(this, (rs, re));
            }
            else if (SelectedDate is { } dt)
            {
                DateSelected?.Invoke(this, dt);
            }
            Close();
        };

        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 0, 0),
        };
        footer.Children.Add(cancelBtn);
        footer.Children.Add(okBtn);

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
        };
        stack.Children.Add(overline);
        stack.Children.Add(headerRow);
        stack.Children.Add(new MavDivider());
        stack.Children.Add(cal);
        stack.Children.Add(inputBox);

        var anchorVisual = anchor ?? (this.Parent as Visual) ?? this;

        var scroll = new MavScrollViewer
        {
            Content = stack,
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        };

        // Cancel/OK stay pinned while the calendar scrolls on short screens.
        var content = new DockPanel();
        DockPanel.SetDock(footer, Dock.Bottom);
        content.Children.Add(footer);
        content.Children.Add(scroll);

        var card = new MavSurface
        {
            Radius = 28,
            Elevation = 3,
            Padding = new Thickness(24, 20, 24, 16),
            FillColor = c.SurfaceContainerHigh,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Content = content,
        };

        void Reflow()
        {
            var overlaySize = MavOverlay.GetOverlaySize(anchorVisual);
            var insets = MavOverlay.SafeAreaOf(anchorVisual);
            var availableWidth = Math.Max(0, overlaySize.Width - insets.Left - insets.Right - 32);
            var availableHeight = Math.Max(0, overlaySize.Height - insets.Top - insets.Bottom - 32);

            card.MinWidth = Math.Min(328, availableWidth);
            card.MaxWidth = availableWidth;
            card.MaxHeight = availableHeight;
            card.Margin = new Thickness(
                16 + insets.Left,
                16 + insets.Top,
                16 + insets.Right,
                16 + insets.Bottom);
        }

        Reflow();

        UpdateHeadline();
        _handle = MavOverlay.Show(anchorVisual, card, c.Scrim.WithAlpha(0.32f), () =>
        {
            Dismissed?.Invoke(this, EventArgs.Empty);
            Close();
        }, onViewportChanged: Reflow);
        _syncingIsOpen = true;
        SetValue(IsOpenProperty, true);
        _syncingIsOpen = false;
        MavMotion.ScaleIn(card, 0.92, MavMotion.Medium1);
    }

    public void Close()
    {
        if (_handle is not null)
        {
            _handle.Dispose();
            _handle = null;
            _syncingIsOpen = true;
            SetValue(IsOpenProperty, false);
            _syncingIsOpen = false;
        }
    }

    public static Task<DateTime?> PickAsync(
        DateTime? initial = null,
        bool startInInputMode = false,
        Visual? anchor = null,
        DateTime? minDate = null,
        DateTime? maxDate = null,
        Func<DateTime, bool>? selectableDayPredicate = null)
    {
        var tcs = new TaskCompletionSource<DateTime?>();
        var picker = new MavDatePicker
        {
            SelectedDate = initial ?? DateTime.Today,
            StartInInputMode = startInInputMode,
            MinDate = minDate,
            MaxDate = maxDate,
            SelectableDayPredicate = selectableDayPredicate,
        };
        picker.DateSelected += (_, dt) => tcs.TrySetResult(dt);
        picker.Dismissed += (_, _) => tcs.TrySetResult(null);
        picker.Show(anchor);
        return tcs.Task;
    }

    public static Task<DateTime?> PickDateAsync(
        DateTime? initial = null,
        DateTime? minDate = null,
        DateTime? maxDate = null,
        Visual? anchor = null) =>
        PickAsync(initial, startInInputMode: false, anchor: anchor, minDate: minDate, maxDate: maxDate);

    public static Task<(DateTime Start, DateTime End)?> PickRangeAsync(
        DateTime? start = null,
        DateTime? end = null,
        Visual? anchor = null,
        DateTime? minDate = null,
        DateTime? maxDate = null)
    {
        var tcs = new TaskCompletionSource<(DateTime Start, DateTime End)?>();
        var picker = new MavDatePicker
        {
            IsRange = true,
            RangeStart = start ?? DateTime.Today,
            RangeEnd = end ?? DateTime.Today.AddDays(4),
            MinDate = minDate,
            MaxDate = maxDate,
        };
        picker.RangeSelected += (_, r) => tcs.TrySetResult(r);
        picker.Dismissed += (_, _) => tcs.TrySetResult(null);
        picker.Show(anchor);
        return tcs.Task;
    }

    public static Task<(DateTime Start, DateTime End)?> PickDateRangeAsync(
        DateTime? start = null,
        DateTime? end = null,
        DateTime? minDate = null,
        DateTime? maxDate = null,
        Visual? anchor = null) =>
        PickRangeAsync(start, end, anchor, minDate, maxDate);
}

/// <summary>
/// Shared base class for read-only picker text fields (<see cref="MavDatePickerField"/>,
/// <see cref="MavDateRangePickerField"/>, <see cref="MavTimePickerField"/>).
/// </summary>
public abstract class MavPickerFieldBase : MavContentView
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MavPickerFieldBase, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string?> SupportingTextProperty =
        AvaloniaProperty.Register<MavPickerFieldBase, string?>(nameof(SupportingText));

    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<MavPickerFieldBase, string?>(nameof(ErrorText));

    public static readonly StyledProperty<MavTextFieldVariant> VariantProperty =
        AvaloniaProperty.Register<MavPickerFieldBase, MavTextFieldVariant>(nameof(Variant), MavTextFieldVariant.Outlined);

    public static readonly StyledProperty<bool> IsClearableProperty =
        AvaloniaProperty.Register<MavPickerFieldBase, bool>(nameof(IsClearable));

    protected readonly MavTextField Field;

    static MavPickerFieldBase()
    {
        LabelProperty.Changed.AddClassHandler<MavPickerFieldBase>((f, e) => f.Field.Label = e.GetNewValue<string>());
        SupportingTextProperty.Changed.AddClassHandler<MavPickerFieldBase>((f, e) => f.Field.SupportingText = e.GetNewValue<string?>());
        ErrorTextProperty.Changed.AddClassHandler<MavPickerFieldBase>((f, e) => f.Field.ErrorText = e.GetNewValue<string?>());
        VariantProperty.Changed.AddClassHandler<MavPickerFieldBase>((f, e) => f.Field.Variant = e.GetNewValue<MavTextFieldVariant>());
        IsClearableProperty.Changed.AddClassHandler<MavPickerFieldBase>((f, _) => f.SyncFieldText());
    }

    protected MavPickerFieldBase(string label, MavTextFieldVariant variant, string leadingIcon)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        Field = new MavTextField(label, variant, leadingIcon: leadingIcon)
        {
            IsReadOnly = true,
            TrailingIcon = MavIcons.ArrowDropDown,
        };
        Field.InnerEntry.IsHitTestVisible = false;
        Field.TrailingIconClicked += async (_, _) =>
        {
            if (!IsEnabled) return;
            if (IsClearable && HasSelectedValue)
            {
                Clear();
                return;
            }
            await OpenPickerAsync();
        };

        var tapOverlay = new Border
        {
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            Margin = new Thickness(0, 0, 44, 0),
        };
        tapOverlay.PointerReleased += async (_, _) =>
        {
            if (!IsEnabled) return;
            await OpenPickerAsync();
        };

        var grid = new Grid();
        grid.Children.Add(Field);
        grid.Children.Add(tapOverlay);
        Content = grid;

        SetValue(LabelProperty, label);
        SetValue(VariantProperty, variant);
    }

    public bool IsClearable
    {
        get => GetValue(IsClearableProperty);
        set => SetValue(IsClearableProperty, value);
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? SupportingText
    {
        get => GetValue(SupportingTextProperty);
        set => SetValue(SupportingTextProperty, value);
    }

    public string? ErrorText
    {
        get => GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    public MavTextFieldVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    protected abstract bool HasSelectedValue { get; }
    public abstract void Clear();
    protected abstract Task OpenPickerAsync();
    protected abstract string FormatDisplayValue();

    protected void SyncFieldText()
    {
        Field.Text = FormatDisplayValue();
        Field.TrailingIcon = (IsClearable && HasSelectedValue) ? MavIcons.Cancel : MavIcons.ArrowDropDown;
    }
}

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

    public MavDatePickerField() : this("Date")
    {
    }

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

    protected override async Task OpenPickerAsync()
    {
        var picked = await MavDatePicker.PickAsync(SelectedDate, anchor: this, minDate: MinDate, maxDate: MaxDate, selectableDayPredicate: SelectableDayPredicate);
        if (picked is { } dt)
            SelectedDate = dt;
    }

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

    public MavDateRangePickerField() : this("Date Range")
    {
    }

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

    public override void Clear()
    {
        StartDate = null;
        EndDate = null;
    }

    protected override async Task OpenPickerAsync()
    {
        var picked = await MavDatePicker.PickRangeAsync(StartDate, EndDate, anchor: this, minDate: MinDate, maxDate: MaxDate);
        if (picked is { } r)
        {
            StartDate = r.Start;
            EndDate = r.End;
        }
    }

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
