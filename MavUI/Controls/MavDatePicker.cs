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

    /// <summary>
    /// Measures the calendar during the layout pass.
    /// </summary>
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

    /// <summary>Shows the picker over <paramref name="anchor"/>.</summary>
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
            Child = stack,
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
            Child = content,
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

    /// <summary>Hides the picker.</summary>
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

    /// <summary>Shows the picker next to <paramref name="anchor"/> and returns the picked date, or <c>null</c> if it was cancelled.</summary>
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

    /// <summary>Shows the picker and returns the picked date, or <c>null</c> if it was cancelled.</summary>
    public static Task<DateTime?> PickDateAsync(
        DateTime? initial = null,
        DateTime? minDate = null,
        DateTime? maxDate = null,
        Visual? anchor = null) =>
        PickAsync(initial, startInInputMode: false, anchor: anchor, minDate: minDate, maxDate: maxDate);

    /// <summary>
    /// Shows a date-range picker and returns the selected range, or null when cancelled.
    /// </summary>
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

    /// <summary>
    /// Shows a date-range picker and returns the selected range, or null when cancelled.
    /// </summary>
    public static Task<(DateTime Start, DateTime End)?> PickDateRangeAsync(
        DateTime? start = null,
        DateTime? end = null,
        DateTime? minDate = null,
        DateTime? maxDate = null,
        Visual? anchor = null) =>
        PickRangeAsync(start, end, anchor, minDate, maxDate);
}
