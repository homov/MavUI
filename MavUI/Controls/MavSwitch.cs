using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Switch (52x32 track with morphing 16/24/28dp thumb and optional check/close icons).
/// </summary>
public class MavSwitch : Control
{
    public static readonly StyledProperty<bool> IsToggledProperty =
        AvaloniaProperty.Register<MavSwitch, bool>(nameof(IsToggled), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> ShowIconsProperty =
        AvaloniaProperty.Register<MavSwitch, bool>(nameof(ShowIcons));

    public static readonly StyledProperty<bool> ShowThumbIconProperty =
        AvaloniaProperty.Register<MavSwitch, bool>(nameof(ShowThumbIcon));

    public static readonly StyledProperty<string?> OnIconProperty =
        AvaloniaProperty.Register<MavSwitch, string?>(nameof(OnIcon));

    public static readonly StyledProperty<string?> OffIconProperty =
        AvaloniaProperty.Register<MavSwitch, string?>(nameof(OffIcon));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavSwitch, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavSwitch, object?>(nameof(CommandParameter));

    private double _progress;

    /// <summary>Thumb position: 0 off, 1 on. Exposed for tests.</summary>
    internal double ToggleProgress => _progress;
    private double _pressProgress;
    private bool _hovered;
    private bool _pressed;

    public event EventHandler<bool>? Toggled;

    static MavSwitch()
    {
        IsToggledProperty.Changed.AddClassHandler<MavSwitch>((s, e) => s.OnToggledChanged(e.GetNewValue<bool>()));
        ShowIconsProperty.Changed.AddClassHandler<MavSwitch>((s, _) => s.InvalidateVisual());
        ShowThumbIconProperty.Changed.AddClassHandler<MavSwitch>((s, e) =>
        {
            s.ShowIcons = e.GetNewValue<bool>();
            s.InvalidateVisual();
        });
    }

    /// <summary>Creates an off switch without icons.</summary>
    public MavSwitch() : this(false, false)
    {
    }

    /// <summary>Creates a switch with the given state, optionally showing the on and off icons.</summary>
    public MavSwitch(bool isToggled = false, bool showIcons = false)
    {
        Width = 52;
        Height = 32;
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Hand);

        _progress = isToggled ? 1.0 : 0.0;
        ShowIcons = showIcons;
        ShowThumbIcon = showIcons;
        if (isToggled) SetValue(IsToggledProperty, true);
        MavAutomation.SetControlType(this, AutomationControlType.CheckBox);

        MavTheme.Observe(this, (self, _) => self.InvalidateVisual());
    }

    public bool IsToggled
    {
        get => GetValue(IsToggledProperty);
        set => SetValue(IsToggledProperty, value);
    }

    public bool ShowIcons
    {
        get => GetValue(ShowIconsProperty);
        set => SetValue(ShowIconsProperty, value);
    }

    public bool ShowThumbIcon
    {
        get => GetValue(ShowThumbIconProperty);
        set => SetValue(ShowThumbIconProperty, value);
    }

    public string? OnIcon
    {
        get => GetValue(OnIconProperty);
        set => SetValue(OnIconProperty, value);
    }

    public string? OffIcon
    {
        get => GetValue(OffIconProperty);
        set => SetValue(OffIconProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>
    /// Measures the switch as its fixed 52x32 track size.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize) => new(52, 32);

    /// <summary>Redraws the switch when a visual property changes.</summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEnabledProperty || change.Property == IsFocusedProperty)
            InvalidateVisual();
    }

    /// <summary>Shows the hover state when the pointer enters the switch.</summary>
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (!IsEnabled) return;
        _hovered = true;
        InvalidateVisual();
    }

    /// <summary>Clears the hover state when the pointer leaves the switch.</summary>
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hovered = false;
        if (_pressed)
        {
            _pressed = false;
            AnimatePress(0);
        }
        InvalidateVisual();
    }

    /// <summary>Starts a press on the switch and shows the pressed state.</summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEnabled) return;
        _pressed = true;
        AnimatePress(1);
    }

    /// <summary>Completes a click and toggles the switch when the pointer is released over it.</summary>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!IsEnabled) return;
        var wasPressed = _pressed;
        _pressed = false;
        AnimatePress(0);
        if (wasPressed)
        {
            e.Handled = true;
            IsToggled = !IsToggled;
        }
    }

    /// <summary>Toggles the switch with the Space or Enter key.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!IsEnabled) return;
        if (e.Key is Key.Space or Key.Enter)
        {
            e.Handled = true;
            IsToggled = !IsToggled;
        }
    }

    private void AnimatePress(double target)
    {
        MavMotion.Animate(this, "sw_press", _pressProgress, target, MavMotion.Short3, MavMotion.CubicOut, v =>
        {
            _pressProgress = v;
            InvalidateVisual();
        });
    }

    private void OnToggledChanged(bool on)
    {
        var target = on ? 1.0 : 0.0;
        MavMotion.AnimateState(this, "sw_toggle", _progress, target, MavMotion.Short4, MavMotion.CubicOut, v =>
        {
            _progress = v;
            InvalidateVisual();
        });
        Toggled?.Invoke(this, on);
        var param = CommandParameter ?? on;
        if (Command?.CanExecute(param) == true)
            Command.Execute(param);
    }

    /// <summary>Draws the track, the thumb and the optional icons for the current state.</summary>
    public override void Render(DrawingContext context)
    {
        var t = MavTheme.Current;
        var c = t.Color;
        var w = 52.0;
        var h = 32.0;
        var rect = new Rect(0, 0, w, h);

        context.DrawRectangle(Brushes.Transparent, null, rect);

        var trackOff = c.SurfaceContainerHighest;
        var trackOn = c.Primary;
        var borderOff = c.Outline;
        var thumbOff = (ShowIcons || ShowThumbIcon) ? c.Outline : c.Outline;
        var thumbOn = c.OnPrimary;

        if (!IsEnabled)
        {
            trackOff = c.SurfaceContainerHighest.WithAlpha(0.12f);
            trackOn = c.OnSurface.WithAlpha(0.12f);
            borderOff = c.OnSurface.WithAlpha(0.12f);
            thumbOff = c.OnSurface.WithAlpha(0.38f);
            thumbOn = c.Surface;
        }

        var trackFill = MavColorScheme.Blend(trackOff, trackOn, _progress);
        var rrect = new RoundedRect(rect, 16);
        context.DrawRectangle(MavTheme.BrushOf(trackFill), null, rrect);

        var borderAlpha = 1.0 - _progress;
        if (borderAlpha > 0.01)
        {
            var strokeRect = new RoundedRect(new Rect(1, 1, w - 2, h - 2), 15);
            context.DrawRectangle(null, MavTheme.PenOf(borderOff.WithAlpha((float)borderAlpha), 2), strokeRect);
        }

        var hasIcons = ShowIcons || ShowThumbIcon;
        var baseOffSize = hasIcons ? 24.0 : 16.0;
        var baseOnSize = 24.0;
        var baseSize = baseOffSize + (baseOnSize - baseOffSize) * _progress;
        var thumbSize = baseSize + (28.0 - baseSize) * _pressProgress;

        var minX = 16.0;
        var maxX = w - 16.0;
        var cx = minX + (maxX - minX) * _progress;
        var cy = h / 2.0;

        if (IsEnabled && (_hovered || _pressed || IsFocused))
        {
            var haloColor = (_progress > 0.5 ? c.Primary : c.OnSurface).WithAlpha(_pressed ? 0.12f : 0.08f);
            context.DrawEllipse(MavTheme.BrushOf(haloColor), null, new Point(cx, cy), 20, 20);
        }

        var thumbColor = MavColorScheme.Blend(thumbOff, thumbOn, _progress);
        context.DrawEllipse(MavTheme.BrushOf(thumbColor), null, new Point(cx, cy), thumbSize / 2.0, thumbSize / 2.0);

        if (hasIcons && thumbSize >= 20)
        {
            var glyph = _progress >= 0.5
                ? (!string.IsNullOrEmpty(OnIcon) ? OnIcon! : MavIcons.Check)
                : (!string.IsNullOrEmpty(OffIcon) ? OffIcon! : MavIcons.Close);
            var iconCol = _progress >= 0.5 ? c.OnPrimaryContainer : c.SurfaceContainerHighest;
            if (!IsEnabled) iconCol = c.OnSurface.WithAlpha(0.38f);

            var ft = MavTextMetrics.GetFormattedText(
                glyph,
                16,
                MavFonts.MaterialIconsTypeface,
                iconCol);
            context.DrawText(ft, new Point(cx - ft.Width / 2.0, cy - ft.Height / 2.0));
        }
    }
}
