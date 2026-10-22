using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Material Design 3 inline Calendar Grid with month navigation, 3-column year selector,
/// single-date selection, and continuous range selection highlight.
/// </summary>
public class MdCalendarGrid : MdContentView
{
    public static readonly StyledProperty<bool> IsRangeProperty =
        AvaloniaProperty.Register<MdCalendarGrid, bool>(nameof(IsRange));

    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        AvaloniaProperty.Register<MdCalendarGrid, DateTime?>(nameof(SelectedDate), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> RangeStartProperty =
        AvaloniaProperty.Register<MdCalendarGrid, DateTime?>(nameof(RangeStart), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> RangeEndProperty =
        AvaloniaProperty.Register<MdCalendarGrid, DateTime?>(nameof(RangeEnd), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> MinDateProperty =
        AvaloniaProperty.Register<MdCalendarGrid, DateTime?>(nameof(MinDate));

    public static readonly StyledProperty<DateTime?> MaxDateProperty =
        AvaloniaProperty.Register<MdCalendarGrid, DateTime?>(nameof(MaxDate));

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
    private readonly MdIcon _monthChevron;
    private readonly MdIconButton _prevBtn;
    private readonly MdIconButton _nextBtn;
    private readonly Grid _weekdayRow;
    private readonly Grid _daysGrid;
    private readonly ScrollViewer _yearScroll;
    private readonly Grid _yearGrid;

    public event EventHandler<DateTime>? DateSelected;
    public event EventHandler<(DateTime? Start, DateTime? End)>? RangeChanged;

    static MdCalendarGrid()
    {
        IsRangeProperty.Changed.AddClassHandler<MdCalendarGrid>((g, e) =>
        {
            g._isRange = e.GetNewValue<bool>();
            g.RebuildDays();
        });
        SelectedDateProperty.Changed.AddClassHandler<MdCalendarGrid>((g, e) =>
        {
            var val = e.GetNewValue<DateTime?>();
            g._selectedDate = val?.Date;
            if (val is { } dt)
                g._displayMonth = new DateTime(dt.Year, dt.Month, 1);
            g.RebuildDays();
        });
        RangeStartProperty.Changed.AddClassHandler<MdCalendarGrid>((g, e) =>
        {
            g._rangeStart = e.GetNewValue<DateTime?>()?.Date;
            g.RebuildDays();
        });
        RangeEndProperty.Changed.AddClassHandler<MdCalendarGrid>((g, e) =>
        {
            g._rangeEnd = e.GetNewValue<DateTime?>()?.Date;
            g.RebuildDays();
        });
        MinDateProperty.Changed.AddClassHandler<MdCalendarGrid>((g, e) =>
        {
            g._minDate = e.GetNewValue<DateTime?>()?.Date;
            g.RebuildDays();
        });
        MaxDateProperty.Changed.AddClassHandler<MdCalendarGrid>((g, e) =>
        {
            g._maxDate = e.GetNewValue<DateTime?>()?.Date;
            g.RebuildDays();
        });
    }

    public MdCalendarGrid() : this(null, false)
    {
    }

    public MdCalendarGrid(DateTime? initialDate = null, bool isRange = false)
    {
        _isRange = isRange;
        var seed = initialDate ?? DateTime.Today;
        _displayMonth = new DateTime(seed.Year, seed.Month, 1);
        if (!isRange && initialDate is not null)
            _selectedDate = initialDate.Value.Date;
        else if (isRange && initialDate is not null)
            _rangeStart = initialDate.Value.Date;

        _monthBtnLabel = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        _monthChevron = new MdIcon(MdIcons.ArrowDropDown, 20) { UseThemeDefaultColor = false };

        var monthBtnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
        };
        monthBtnRow.Children.Add(_monthBtnLabel);
        monthBtnRow.Children.Add(_monthChevron);

        var monthBtn = new MdInkSurface
        {
            Height = 36,
            CornerRadius = 18,
            Padding = new Thickness(12, 0, 8, 0),
            Content = monthBtnRow,
        };

        _prevBtn = new MdIconButton(MdIcons.ChevronLeft);
        _prevBtn.Clicked += (_, _) =>
        {
            _displayMonth = _displayMonth.AddMonths(-1);
            RebuildDays();
        };

        _nextBtn = new MdIconButton(MdIcons.ChevronRight);
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
        _yearScroll = new MdScrollViewer
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
        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
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

    private void RebuildWeekdays(MdTheme t)
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
            lbl.Apply(MdTypeRole.BodySmall, t.Typography);
            lbl.Foreground = MdTheme.BrushOf(t.Color.OnSurfaceVariant);
            _weekdayRow.Add(lbl, i, 0);
        }
    }

    private void RebuildDays()
    {
        var t = MdTheme.Current;
        var c = t.Color;
        _monthBtnLabel.Text = _displayMonth.ToString("MMMM yyyy", CultureInfo.CurrentUICulture);
        _monthBtnLabel.Apply(MdTypeRole.LabelLarge, t.Typography);
        _monthBtnLabel.Foreground = MdTheme.BrushOf(c.OnSurfaceVariant);
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
                    Background = MdTheme.BrushOf(c.SecondaryContainer),
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
            lbl.Apply(MdTypeRole.BodyMedium, t.Typography);
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
            lbl.Foreground = MdTheme.BrushOf(fg);

            var circle = new MdInkSurface
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
        var t = MdTheme.Current;
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
            lbl.Apply(MdTypeRole.BodyLarge, t.Typography);
            lbl.Foreground = MdTheme.BrushOf(sel ? c.OnPrimary : c.OnSurfaceVariant);

            var pill = new MdInkSurface
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

    private void ApplyTheme(MdTheme t)
    {
        RebuildWeekdays(t);
        RebuildDays();
        if (_yearPickerOpen) RebuildYears();
    }
}

/// <summary>
/// Material Design 3 Modal Date Picker Dialog (Single date or Date range, with calendar and text input modes).
/// </summary>
public class MdDatePicker : Control
{
    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        AvaloniaProperty.Register<MdDatePicker, DateTime?>(nameof(SelectedDate), DateTime.Today, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> RangeStartProperty =
        AvaloniaProperty.Register<MdDatePicker, DateTime?>(nameof(RangeStart), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> RangeEndProperty =
        AvaloniaProperty.Register<MdDatePicker, DateTime?>(nameof(RangeEnd), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> MinDateProperty =
        AvaloniaProperty.Register<MdDatePicker, DateTime?>(nameof(MinDate));

    public static readonly StyledProperty<DateTime?> MaxDateProperty =
        AvaloniaProperty.Register<MdDatePicker, DateTime?>(nameof(MaxDate));

    public static readonly StyledProperty<bool> IsRangeProperty =
        AvaloniaProperty.Register<MdDatePicker, bool>(nameof(IsRange));

    public static readonly StyledProperty<bool> StartInInputModeProperty =
        AvaloniaProperty.Register<MdDatePicker, bool>(nameof(StartInInputMode));

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MdDatePicker, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    private IDisposable? _handle;
    private bool _syncingIsOpen;

    static MdDatePicker()
    {
        IsOpenProperty.Changed.AddClassHandler<MdDatePicker>((p, e) =>
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

        var t = MdTheme.Current;
        var c = t.Color;
        var inputMode = StartInInputMode;

        var overline = new TextBlock
        {
            Text = (IsRange ? MdLocalization.SelectDateRange : MdLocalization.SelectDate).ToUpperInvariant(),
        };
        overline.Apply(MdTypeRole.LabelMedium, t.Typography);
        overline.Foreground = MdTheme.BrushOf(c.OnSurfaceVariant);

        var headline = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };
        headline.Apply(MdTypeRole.HeadlineMedium, t.Typography);
        headline.Foreground = MdTheme.BrushOf(c.OnSurface);

        var cal = new MdCalendarGrid(IsRange ? (RangeStart ?? DateTime.Today) : (SelectedDate ?? DateTime.Today), IsRange)
        {
            MinDate = MinDate,
            MaxDate = MaxDate,
            SelectableDayPredicate = SelectableDayPredicate,
            RangeStart = RangeStart,
            RangeEnd = RangeEnd,
            SelectedDate = SelectedDate,
            IsVisible = !inputMode,
        };

        var singleInput = new MdTextField(MdLocalization.EnterDate, MdTextFieldVariant.Outlined, MdLocalization.DateFieldHint)
        {
            Text = (SelectedDate ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        };
        var startInput = new MdTextField(MdLocalization.StartDate, MdTextFieldVariant.Outlined, MdLocalization.DateFieldHint)
        {
            Text = RangeStart?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
        };
        var endInput = new MdTextField(MdLocalization.EndDate, MdTextFieldVariant.Outlined, MdLocalization.DateFieldHint)
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
                var s = cal.RangeStart?.ToString("MMM d", CultureInfo.CurrentUICulture) ?? MdLocalization.StartDate;
                var e = cal.RangeEnd?.ToString("MMM d", CultureInfo.CurrentUICulture) ?? MdLocalization.EndDate;
                headline.Text = $"{s} {MdLocalization.DateRangeSeparator} {e}";
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

        var modeBtn = new MdIconButton(inputMode ? MdIcons.CalendarToday : MdIcons.Edit);
        modeBtn.Clicked += (_, _) =>
        {
            inputMode = !inputMode;
            modeBtn.Icon = inputMode ? MdIcons.CalendarToday : MdIcons.Edit;
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

        var cancelBtn = new MdButton(MdLocalization.Cancel, MdButtonVariant.Text);
        cancelBtn.Clicked += (_, _) =>
        {
            Dismissed?.Invoke(this, EventArgs.Empty);
            Close();
        };

        var okBtn = new MdButton(MdLocalization.Ok, MdButtonVariant.Text);
        okBtn.Clicked += (_, _) =>
        {
            if (inputMode)
            {
                if (!IsRange)
                {
                    if (!DateTime.TryParse(singleInput.Text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    {
                        singleInput.ErrorText = MdLocalization.InvalidDate;
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
                        endInput.ErrorText = MdLocalization.InvalidRange;
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
        stack.Children.Add(new MdDivider());
        stack.Children.Add(cal);
        stack.Children.Add(inputBox);

        var anchorVisual = anchor ?? (this.Parent as Visual) ?? this;

        var scroll = new MdScrollViewer
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

        var card = new MdSurface
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
            var overlaySize = MdOverlay.GetOverlaySize(anchorVisual);
            var insets = MdOverlay.SafeAreaOf(anchorVisual);
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
        _handle = MdOverlay.Show(anchorVisual, card, c.Scrim.WithAlpha(0.32f), () =>
        {
            Dismissed?.Invoke(this, EventArgs.Empty);
            Close();
        }, onViewportChanged: Reflow);
        _syncingIsOpen = true;
        SetValue(IsOpenProperty, true);
        _syncingIsOpen = false;
        MdMotion.ScaleIn(card, 0.92, MdMotion.Medium1);
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
        var picker = new MdDatePicker
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
        var picker = new MdDatePicker
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
/// Shared base class for read-only picker text fields (<see cref="MdDatePickerField"/>,
/// <see cref="MdDateRangePickerField"/>, <see cref="MdTimePickerField"/>).
/// </summary>
public abstract class MdPickerFieldBase : MdContentView
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MdPickerFieldBase, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string?> SupportingTextProperty =
        AvaloniaProperty.Register<MdPickerFieldBase, string?>(nameof(SupportingText));

    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<MdPickerFieldBase, string?>(nameof(ErrorText));

    public static readonly StyledProperty<MdTextFieldVariant> VariantProperty =
        AvaloniaProperty.Register<MdPickerFieldBase, MdTextFieldVariant>(nameof(Variant), MdTextFieldVariant.Outlined);

    public static readonly StyledProperty<bool> IsClearableProperty =
        AvaloniaProperty.Register<MdPickerFieldBase, bool>(nameof(IsClearable));

    protected readonly MdTextField Field;

    static MdPickerFieldBase()
    {
        LabelProperty.Changed.AddClassHandler<MdPickerFieldBase>((f, e) => f.Field.Label = e.GetNewValue<string>());
        SupportingTextProperty.Changed.AddClassHandler<MdPickerFieldBase>((f, e) => f.Field.SupportingText = e.GetNewValue<string?>());
        ErrorTextProperty.Changed.AddClassHandler<MdPickerFieldBase>((f, e) => f.Field.ErrorText = e.GetNewValue<string?>());
        VariantProperty.Changed.AddClassHandler<MdPickerFieldBase>((f, e) => f.Field.Variant = e.GetNewValue<MdTextFieldVariant>());
        IsClearableProperty.Changed.AddClassHandler<MdPickerFieldBase>((f, _) => f.SyncFieldText());
    }

    protected MdPickerFieldBase(string label, MdTextFieldVariant variant, string leadingIcon)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        Field = new MdTextField(label, variant, leadingIcon: leadingIcon)
        {
            IsReadOnly = true,
            TrailingIcon = MdIcons.ArrowDropDown,
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

    public MdTextFieldVariant Variant
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
        Field.TrailingIcon = (IsClearable && HasSelectedValue) ? MdIcons.Cancel : MdIcons.ArrowDropDown;
    }
}

/// <summary>
/// Inline text field that opens an <see cref="MdDatePicker"/> dialog when tapped.
/// </summary>
public class MdDatePickerField : MdPickerFieldBase
{
    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        AvaloniaProperty.Register<MdDatePickerField, DateTime?>(
            nameof(SelectedDate),
            null,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> FormatProperty =
        AvaloniaProperty.Register<MdDatePickerField, string>(nameof(Format), "yyyy-MM-dd");

    public static readonly StyledProperty<DateTime?> MinDateProperty =
        AvaloniaProperty.Register<MdDatePickerField, DateTime?>(nameof(MinDate));

    public static readonly StyledProperty<DateTime?> MaxDateProperty =
        AvaloniaProperty.Register<MdDatePickerField, DateTime?>(nameof(MaxDate));

    public event EventHandler<DateTime>? DateSelected;
    public event EventHandler<DateTime?>? SelectedDateChanged;

    static MdDatePickerField()
    {
        SelectedDateProperty.Changed.AddClassHandler<MdDatePickerField>((f, e) =>
            f.OnSelectedDateChanged(e.GetNewValue<DateTime?>()));
        FormatProperty.Changed.AddClassHandler<MdDatePickerField>((f, _) => f.SyncFieldText());
    }

    public MdDatePickerField() : this("Date")
    {
    }

    public MdDatePickerField(
        string label = "Date",
        DateTime? initial = null,
        MdTextFieldVariant variant = MdTextFieldVariant.Outlined)
        : base(label, variant, MdIcons.CalendarToday)
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
        var picked = await MdDatePicker.PickAsync(SelectedDate, anchor: this, minDate: MinDate, maxDate: MaxDate, selectableDayPredicate: SelectableDayPredicate);
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
/// Inline text field that opens an <see cref="MdDatePicker"/> in date-range mode when tapped.
/// </summary>
public class MdDateRangePickerField : MdPickerFieldBase
{
    public static readonly StyledProperty<DateTime?> StartDateProperty =
        AvaloniaProperty.Register<MdDateRangePickerField, DateTime?>(
            nameof(StartDate),
            null,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> EndDateProperty =
        AvaloniaProperty.Register<MdDateRangePickerField, DateTime?>(
            nameof(EndDate),
            null,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> FormatProperty =
        AvaloniaProperty.Register<MdDateRangePickerField, string>(nameof(Format), "MMM d, yyyy");

    public static readonly StyledProperty<DateTime?> MinDateProperty =
        AvaloniaProperty.Register<MdDateRangePickerField, DateTime?>(nameof(MinDate));

    public static readonly StyledProperty<DateTime?> MaxDateProperty =
        AvaloniaProperty.Register<MdDateRangePickerField, DateTime?>(nameof(MaxDate));

    public event EventHandler<(DateTime? Start, DateTime? End)>? RangeChanged;
    public event EventHandler<(DateTime Start, DateTime End)>? RangeSelected;

    static MdDateRangePickerField()
    {
        StartDateProperty.Changed.AddClassHandler<MdDateRangePickerField>((f, _) => f.OnRangeChanged());
        EndDateProperty.Changed.AddClassHandler<MdDateRangePickerField>((f, _) => f.OnRangeChanged());
        FormatProperty.Changed.AddClassHandler<MdDateRangePickerField>((f, _) => f.SyncFieldText());
    }

    public MdDateRangePickerField() : this("Date Range")
    {
    }

    public MdDateRangePickerField(
        string label = "Date Range",
        DateTime? start = null,
        DateTime? end = null,
        MdTextFieldVariant variant = MdTextFieldVariant.Outlined)
        : base(label, variant, MdIcons.DateRange)
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
        var picked = await MdDatePicker.PickRangeAsync(StartDate, EndDate, anchor: this, minDate: MinDate, maxDate: MaxDate);
        if (picked is { } r)
        {
            StartDate = r.Start;
            EndDate = r.End;
        }
    }

    protected override string FormatDisplayValue()
    {
        if (StartDate is { } s && EndDate is { } e)
            return $"{s.ToString(Format, CultureInfo.CurrentUICulture)} {MdLocalization.DateRangeSeparator} {e.ToString(Format, CultureInfo.CurrentUICulture)}";
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
