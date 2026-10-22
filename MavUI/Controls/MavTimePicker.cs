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
/// Material Design 3 Time Picker Dialog with Hour/Minute digital chips, AM/PM selector,
/// 256dp analog clock dial, and keyboard input mode toggle.
/// </summary>
public class MavTimePicker : Control
{
    public static readonly StyledProperty<TimeSpan> SelectedTimeProperty =
        AvaloniaProperty.Register<MavTimePicker, TimeSpan>(nameof(SelectedTime), new TimeSpan(9, 30, 0), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> Is24HourProperty =
        AvaloniaProperty.Register<MavTimePicker, bool>(nameof(Is24Hour));

    public static readonly StyledProperty<bool> StartInInputModeProperty =
        AvaloniaProperty.Register<MavTimePicker, bool>(nameof(StartInInputMode));

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MavTimePicker, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    private IDisposable? _handle;
    private bool _syncingIsOpen;

    static MavTimePicker()
    {
        IsOpenProperty.Changed.AddClassHandler<MavTimePicker>((p, e) =>
        {
            if (p._syncingIsOpen) return;
            if (e.GetNewValue<bool>())
                p.Show(p.Parent as Visual ?? p);
            else
                p.Close();
        });
    }

    protected override Size MeasureOverride(Size availableSize) => default;

    public TimeSpan SelectedTime
    {
        get => GetValue(SelectedTimeProperty);
        set => SetValue(SelectedTimeProperty, value);
    }

    public bool Is24Hour
    {
        get => GetValue(Is24HourProperty);
        set => SetValue(Is24HourProperty, value);
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

    public event EventHandler<TimeSpan>? TimeSelected;
    public event EventHandler? Dismissed;

    /// <summary>Shows the picker over <paramref name="anchor"/>.</summary>
    public void Show(Visual? anchor = null)
    {
        Close();

        var t = MavTheme.Current;
        var c = t.Color;
        var hour = SelectedTime.Hours;
        var minute = SelectedTime.Minutes;
        var inputMode = StartInInputMode;

        var overline = new TextBlock
        {
            Text = inputMode ? MavLocalization.EnterTime.ToUpperInvariant() : MavLocalization.SelectTime.ToUpperInvariant(),
        };
        overline.Apply(MavTypeRole.LabelMedium, t.Typography);
        overline.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);

        var dial = new MavTimeDial(hour, minute, Is24Hour);

        // Digital hour & minute chips / input boxes
        var hourEntry = new MavNativeEntry
        {
            MaxLength = 2,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = t.Typography.BrandFont,
            FontSize = 36,
            IsReadOnly = !inputMode,
            IsHitTestVisible = inputMode,
        };
        var minEntry = new MavNativeEntry
        {
            MaxLength = 2,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = t.Typography.BrandFont,
            FontSize = 36,
            IsReadOnly = !inputMode,
            IsHitTestVisible = inputMode,
        };

        var hourBox = new MavInkSurface
        {
            MinWidth = 56,
            MaxWidth = 88,
            Height = 72,
            CornerRadius = 12,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Content = hourEntry,
        };
        var minBox = new MavInkSurface
        {
            MinWidth = 56,
            MaxWidth = 88,
            Height = 72,
            CornerRadius = 12,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Content = minEntry,
        };

        var colon = new TextBlock
        {
            Text = ":",
            FontFamily = t.Typography.BrandFont,
            FontSize = 40,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, -4, 6, 0),
            Foreground = MavTheme.BrushOf(c.OnSurface),
        };

        // AM / PM vertical toggle
        var amLbl = new TextBlock
        {
            Text = MavLocalization.Am,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        amLbl.Apply(MavTypeRole.TitleSmall, t.Typography);

        var pmLbl = new TextBlock
        {
            Text = MavLocalization.Pm,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        pmLbl.Apply(MavTypeRole.TitleSmall, t.Typography);

        var amBtn = new MavInkSurface
        {
            Width = 52,
            Height = 36,
            StrokeThickness = 1,
            StrokeColor = c.Outline,
            Content = amLbl,
        };
        amBtn.SetCornerRadii(8, 8, 0, 0);

        var pmBtn = new MavInkSurface
        {
            Width = 52,
            Height = 36,
            Margin = new Thickness(0, -1, 0, 0),
            StrokeThickness = 1,
            StrokeColor = c.Outline,
            Content = pmLbl,
        };
        pmBtn.SetCornerRadii(0, 0, 8, 8);

        var amPmCol = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = !Is24Hour,
        };
        amPmCol.Children.Add(amBtn);
        amPmCol.Children.Add(pmBtn);

        bool suppressEntrySync = false;

        void UpdateDigitalDisplay()
        {
            var isHourActive = dial.Mode == MavTimeDialMode.Hour;
            hourBox.FillColor = isHourActive ? c.PrimaryContainer : c.SurfaceContainerHighest;
            hourBox.StrokeColor = isHourActive && inputMode ? c.Primary : Colors.Transparent;
            hourBox.StrokeThickness = isHourActive && inputMode ? 2 : 0;
            hourEntry.TextColor = isHourActive ? c.OnPrimaryContainer : c.OnSurface;

            minBox.FillColor = !isHourActive ? c.PrimaryContainer : c.SurfaceContainerHighest;
            minBox.StrokeColor = !isHourActive && inputMode ? c.Primary : Colors.Transparent;
            minBox.StrokeThickness = !isHourActive && inputMode ? 2 : 0;
            minEntry.TextColor = !isHourActive ? c.OnPrimaryContainer : c.OnSurface;

            var dispH = Is24Hour ? hour : (hour % 12 == 0 ? 12 : hour % 12);
            suppressEntrySync = true;
            if (!hourEntry.IsFocused || !inputMode)
                hourEntry.Text = dispH.ToString("00", CultureInfo.InvariantCulture);
            if (!minEntry.IsFocused || !inputMode)
                minEntry.Text = minute.ToString("00", CultureInfo.InvariantCulture);
            suppressEntrySync = false;

            var isPm = hour >= 12;
            amBtn.FillColor = !isPm ? c.TertiaryContainer : Colors.Transparent;
            amLbl.Foreground = MavTheme.BrushOf(!isPm ? c.OnTertiaryContainer : c.OnSurfaceVariant);
            pmBtn.FillColor = isPm ? c.TertiaryContainer : Colors.Transparent;
            pmLbl.Foreground = MavTheme.BrushOf(isPm ? c.OnTertiaryContainer : c.OnSurfaceVariant);
        }

        hourBox.Clicked += (_, _) =>
        {
            dial.Mode = MavTimeDialMode.Hour;
            UpdateDigitalDisplay();
            if (inputMode) hourEntry.Focus();
        };
        minBox.Clicked += (_, _) =>
        {
            dial.Mode = MavTimeDialMode.Minute;
            UpdateDigitalDisplay();
            if (inputMode) minEntry.Focus();
        };

        amBtn.Clicked += (_, _) =>
        {
            if (hour >= 12) hour -= 12;
            dial.Hour = hour;
            UpdateDigitalDisplay();
        };
        pmBtn.Clicked += (_, _) =>
        {
            if (hour < 12) hour += 12;
            dial.Hour = hour;
            UpdateDigitalDisplay();
        };

        hourEntry.PropertyChanged += (_, e) =>
        {
            if (suppressEntrySync || e.Property != TextBox.TextProperty) return;
            if (int.TryParse(hourEntry.Text, out var parsed))
            {
                if (Is24Hour)
                {
                    hour = Math.Clamp(parsed, 0, 23);
                }
                else
                {
                    var h12 = Math.Clamp(parsed, 1, 12) % 12;
                    hour = hour >= 12 ? h12 + 12 : h12;
                }
                dial.Hour = hour;
            }
        };

        minEntry.PropertyChanged += (_, e) =>
        {
            if (suppressEntrySync || e.Property != TextBox.TextProperty) return;
            if (int.TryParse(minEntry.Text, out var parsed))
            {
                minute = Math.Clamp(parsed, 0, 59);
                dial.Minute = minute;
            }
        };

        dial.TimeChanged += (_, tm) =>
        {
            hour = tm.Hour;
            minute = tm.Minute;
            UpdateDigitalDisplay();
        };
        dial.HourSelectionCompleted += (_, _) =>
        {
            dial.Mode = MavTimeDialMode.Minute;
            UpdateDigitalDisplay();
        };

        var digitalRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,*,Auto"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
        };
        digitalRow.Add(hourBox, 0, 0);
        digitalRow.Add(colon, 1, 0);
        digitalRow.Add(minBox, 2, 0);
        digitalRow.Add(amPmCol, 3, 0);

        var dialContainer = new Border
        {
            Margin = new Thickness(0, 8, 0, 4),
            IsVisible = !inputMode,
            Child = dial,
        };

        var modeBtn = new MavIconButton(inputMode ? MavIcons.Schedule : MavIcons.Keyboard);
        modeBtn.Clicked += (_, _) =>
        {
            inputMode = !inputMode;
            overline.Text = inputMode ? MavLocalization.EnterTime.ToUpperInvariant() : MavLocalization.SelectTime.ToUpperInvariant();
            modeBtn.Icon = inputMode ? MavIcons.Schedule : MavIcons.Keyboard;
            dialContainer.IsVisible = !inputMode;
            hourEntry.IsReadOnly = !inputMode;
            hourEntry.IsHitTestVisible = inputMode;
            minEntry.IsReadOnly = !inputMode;
            minEntry.IsHitTestVisible = inputMode;
            UpdateDigitalDisplay();
        };

        var cancelBtn = new MavButton(MavLocalization.Cancel, MavButtonVariant.Text);
        cancelBtn.Clicked += (_, _) =>
        {
            Dismissed?.Invoke(this, EventArgs.Empty);
            Close();
        };

        var okBtn = new MavButton(MavLocalization.Ok, MavButtonVariant.Text);
        okBtn.Clicked += (_, _) =>
        {
            SelectedTime = new TimeSpan(hour, minute, 0);
            TimeSelected?.Invoke(this, SelectedTime);
            Close();
        };

        var footer = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
            ColumnSpacing = 8,
            Margin = new Thickness(0, 8, 0, 0),
        };
        footer.Add(modeBtn, 0, 0);
        footer.Add(cancelBtn, 2, 0);
        footer.Add(okBtn, 3, 0);

        var anchorVisual = anchor ?? (this.Parent as Visual) ?? this;
        var overlaySize = MavOverlay.GetOverlaySize(anchorVisual);
        var availableWidth = Math.Max(0, overlaySize.Width - 40);
        var availableHeight = Math.Max(0, overlaySize.Height - 40);

        // Landscape phones are wide but short: put the clock next to the digital
        // time and size it to the remaining height instead of scrolling a clipped dial.
        var isSideBySide = availableWidth >= 480 &&
                           availableHeight < 520 &&
                           availableWidth > availableHeight;

        Control body;
        double minCardWidthBase;
        if (isSideBySide)
        {
            var dialSide = Math.Clamp(availableHeight - 136, 120, 256);
            dial.Width = dialSide;
            dial.Height = dialSide;

            var bodyGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                ColumnSpacing = 20,
                VerticalAlignment = VerticalAlignment.Center,
            };
            bodyGrid.Add(digitalRow, 0, 0);
            bodyGrid.Add(dialContainer, 1, 0);

            body = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 8,
                Children =
                {
                    overline,
                    bodyGrid,
                },
            };
            minCardWidthBase = 520;
        }
        else
        {
            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 16,
            };
            stack.Children.Add(overline);
            stack.Children.Add(digitalRow);
            stack.Children.Add(dialContainer);

            body = new MavScrollViewer
            {
                Content = stack,
                HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            };
            minCardWidthBase = 312;
        }

        // Keep Cancel/OK reachable while the body scrolls on short portrait screens.
        var content = new DockPanel();
        DockPanel.SetDock(footer, Dock.Bottom);
        content.Children.Add(footer);
        content.Children.Add(body);

        var card = new MavSurface
        {
            Radius = 28,
            Elevation = 3,
            Padding = new Thickness(24),
            FillColor = c.SurfaceContainerHigh,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Content = content,
        };

        // Recompute the caps and safe-area margins whenever the viewport changes. The
        // body/scroll layout itself adapts (footer is docked), so only the card box moves.
        void Reflow()
        {
            var size = MavOverlay.GetOverlaySize(anchorVisual);
            var insets = MavOverlay.SafeAreaOf(anchorVisual);
            var width = Math.Max(0, size.Width - insets.Left - insets.Right - 40);
            var height = Math.Max(0, size.Height - insets.Top - insets.Bottom - 40);

            if (isSideBySide)
            {
                var dialSide = Math.Clamp(Math.Min(height - 136, width - 240), 96, 256);
                dial.Width = dialSide;
                dial.Height = dialSide;
            }

            card.MinWidth = Math.Min(minCardWidthBase, width);
            card.MaxWidth = width;
            card.MaxHeight = Math.Max(180, height);
            card.Margin = new Thickness(
                20 + insets.Left,
                20 + insets.Top,
                20 + insets.Right,
                20 + insets.Bottom);
        }

        Reflow();

        UpdateDigitalDisplay();
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

    /// <summary>Shows the picker next to <paramref name="anchor"/> and returns the picked time, or <c>null</c> if it was cancelled.</summary>
    public static Task<TimeSpan?> PickAsync(
        TimeSpan? initial = null,
        bool is24Hour = false,
        bool startInInputMode = false,
        Visual? anchor = null)
    {
        var tcs = new TaskCompletionSource<TimeSpan?>();
        TimeSpan? picked = null;
        var picker = new MavTimePicker
        {
            SelectedTime = initial ?? new TimeSpan(9, 30, 0),
            Is24Hour = is24Hour,
            StartInInputMode = startInInputMode,
        };
        picker.TimeSelected += (_, tm) =>
        {
            picked = tm;
            tcs.TrySetResult(picked);
        };
        picker.Dismissed += (_, _) => tcs.TrySetResult(null);
        picker.Show(anchor);
        return tcs.Task;
    }

    /// <summary>Shows the picker and returns the picked time, or <c>null</c> if it was cancelled.</summary>
    public static Task<TimeSpan?> PickTimeAsync(
        TimeSpan? initial = null,
        bool is24Hour = false,
        Visual? anchor = null) =>
        PickAsync(initial, is24Hour, startInInputMode: false, anchor: anchor);
}
