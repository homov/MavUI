using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Mav.Theming;

namespace Mav.Primitives;

/// <summary>
/// Interactive Material Design 3 surface with custom-drawn rounded container,
/// hover/focus/press state layer, expanding radial ripple, border stroke,
/// keyboard focus ring, and two-layer MD3 elevation shadow.
/// Zero-allocation per-frame rendering via cached brushes and pens.
/// </summary>
public class MavInkSurface : Decorator
{
    public static readonly StyledProperty<bool> IsInteractiveProperty =
        AvaloniaProperty.Register<MavInkSurface, bool>(nameof(IsInteractive), true);

    public static readonly StyledProperty<Color> FillColorProperty =
        AvaloniaProperty.Register<MavInkSurface, Color>(nameof(FillColor), Colors.Transparent);

    public static readonly StyledProperty<Color> StrokeColorProperty =
        AvaloniaProperty.Register<MavInkSurface, Color>(nameof(StrokeColor), Colors.Transparent);

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<MavInkSurface, double>(nameof(StrokeThickness));

    public static readonly StyledProperty<Color> RippleColorProperty =
        AvaloniaProperty.Register<MavInkSurface, Color>(nameof(RippleColor), Colors.Black);

    public static readonly StyledProperty<double> CornerRadiusProperty =
        AvaloniaProperty.Register<MavInkSurface, double>(nameof(CornerRadius), 20.0);

    public static readonly StyledProperty<CornerRadius> CornerRadiiProperty =
        AvaloniaProperty.Register<MavInkSurface, CornerRadius>(nameof(CornerRadii), new CornerRadius(20));

    public static readonly StyledProperty<float> StateOverlayOpacityProperty =
        AvaloniaProperty.Register<MavInkSurface, float>(nameof(StateOverlayOpacity));

    public static readonly StyledProperty<double> ElevationProperty =
        AvaloniaProperty.Register<MavInkSurface, double>(nameof(Elevation));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavInkSurface, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavInkSurface, object?>(nameof(CommandParameter));

    private Point _rippleOrigin;
    private double _rippleProgress;
    private double _rippleAlpha;
    private bool _pressed;
    private bool _hovered;
    private bool _keyboardFocused;
    private bool _interactive = true;

    private Color _fill = Colors.Transparent;
    private Color _stroke = Colors.Transparent;
    private double _strokeThickness;
    private Color _rippleColor = Colors.Black;
    private CornerRadius _radii = new(20);
    private float _stateOverlayOpacity;
    private double _elevation;
    private ICommand? _command;
    private object? _commandParameter;

    // Cached render resources to avoid per-frame allocations during 60fps animations
    private IBrush _fillBrush = Brushes.Transparent;
    private Pen? _strokePen;
    private BoxShadows _cachedShadows;

    public event EventHandler? Clicked;
    public event EventHandler? Pressed;
    public event EventHandler? Released;
    public event EventHandler? Cancelled;

    static MavInkSurface()
    {
        IsInteractiveProperty.Changed.AddClassHandler<MavInkSurface>((s, e) =>
        {
            var val = e.GetNewValue<bool>();
            if (s._interactive == val) return;
            s._interactive = val;
            s.UpdateInteractiveState();
        });
        FillColorProperty.Changed.AddClassHandler<MavInkSurface>((s, e) =>
        {
            var val = e.GetNewValue<Color>();
            if (s._fill == val) return;
            s._fill = val;
            s._fillBrush = val.A > 0 ? MavTheme.BrushOf(val) : Brushes.Transparent;
            s.InvalidateVisual();
        });
        StrokeColorProperty.Changed.AddClassHandler<MavInkSurface>((s, e) =>
        {
            var val = e.GetNewValue<Color>();
            if (s._stroke == val) return;
            s._stroke = val;
            s.RebuildStrokePen();
            s.InvalidateVisual();
        });
        StrokeThicknessProperty.Changed.AddClassHandler<MavInkSurface>((s, e) =>
        {
            var val = e.GetNewValue<double>();
            if (Math.Abs(s._strokeThickness - val) < 1e-6) return;
            s._strokeThickness = val;
            s.RebuildStrokePen();
            s.InvalidateVisual();
        });
        RippleColorProperty.Changed.AddClassHandler<MavInkSurface>((s, e) =>
        {
            var val = e.GetNewValue<Color>();
            if (s._rippleColor == val) return;
            s._rippleColor = val;
            s.InvalidateVisual();
        });
        CornerRadiusProperty.Changed.AddClassHandler<MavInkSurface>((s, e) =>
        {
            var r = Math.Max(0, e.GetNewValue<double>());
            s._radii = new CornerRadius(r);
            s.InvalidateVisual();
        });
        CornerRadiiProperty.Changed.AddClassHandler<MavInkSurface>((s, e) =>
        {
            s._radii = e.GetNewValue<CornerRadius>();
            s.InvalidateVisual();
        });
        StateOverlayOpacityProperty.Changed.AddClassHandler<MavInkSurface>((s, e) =>
        {
            var clamped = Math.Clamp(e.GetNewValue<float>(), 0f, 1f);
            if (Math.Abs(s._stateOverlayOpacity - clamped) < 1e-4f) return;
            s._stateOverlayOpacity = clamped;
            s.InvalidateVisual();
        });
        ElevationProperty.Changed.AddClassHandler<MavInkSurface>((s, e) =>
        {
            var clamped = Math.Max(0, e.GetNewValue<double>());
            if (Math.Abs(s._elevation - clamped) < 1e-4) return;
            s._elevation = clamped;
            s._cachedShadows = MavElevationTokens.ToBoxShadows(s._elevation);
            s.InvalidateVisual();
        });
        CommandProperty.Changed.AddClassHandler<MavInkSurface>((s, e) => s._command = e.GetNewValue<ICommand?>());
        CommandParameterProperty.Changed.AddClassHandler<MavInkSurface>((s, e) => s._commandParameter = e.GetNewValue<object?>());
    }

    /// <summary>Creates an empty ink surface that draws the state layers.</summary>
    public MavInkSurface()
    {
        ClipToBounds = false;
        UpdateInteractiveState();
    }

    public static readonly StyledProperty<Control?> ContentProperty =
        ChildProperty.AddOwner<MavInkSurface>();

    public Control? Content
    {
        get => Child;
        set => Child = value;
    }

    public bool IsInteractive
    {
        get => _interactive;
        set => SetValue(IsInteractiveProperty, value);
    }

    public Color FillColor
    {
        get => _fill;
        set => SetValue(FillColorProperty, value);
    }

    public Color StrokeColor
    {
        get => _stroke;
        set => SetValue(StrokeColorProperty, value);
    }

    public double StrokeThickness
    {
        get => _strokeThickness;
        set => SetValue(StrokeThicknessProperty, value);
    }

    public Color BorderStrokeColor
    {
        get => StrokeColor;
        set => StrokeColor = value;
    }

    public double BorderStrokeThickness
    {
        get => StrokeThickness;
        set => StrokeThickness = value;
    }

    public Color RippleColor
    {
        get => _rippleColor;
        set => SetValue(RippleColorProperty, value);
    }

    public double CornerRadius
    {
        get => _radii.TopLeft;
        set => SetValue(CornerRadiusProperty, Math.Max(0, value));
    }

    public CornerRadius CornerRadii
    {
        get => _radii;
        set => SetValue(CornerRadiiProperty, value);
    }

    /// <summary>Sets a separate corner radius for each corner.</summary>
    public void SetCornerRadii(double topLeft, double topRight, double bottomLeft, double bottomRight)
    {
        SetValue(CornerRadiiProperty, new CornerRadius(topLeft, topRight, bottomRight, bottomLeft));
    }

    public float StateOverlayOpacity
    {
        get => _stateOverlayOpacity;
        set => SetValue(StateOverlayOpacityProperty, value);
    }

    public double Elevation
    {
        get => _elevation;
        set => SetValue(ElevationProperty, value);
    }

    public bool IsHovered => _hovered;
    public bool IsPressedState => _pressed;

    public double DisabledOpacity { get; set; } = 1.0;

    public ICommand? Command
    {
        get => _command;
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => _commandParameter;
        set => SetValue(CommandParameterProperty, value);
    }

    private void RebuildStrokePen()
    {
        _strokePen = _strokeThickness > 0 && _stroke.A > 0
            ? new Pen(MavTheme.BrushOf(_stroke), _strokeThickness)
            : null;
    }

    private void UpdateInteractiveState()
    {
        Focusable = _interactive;
        Cursor = _interactive ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
    }

    /// <summary>Shows the focus ring when the surface gets keyboard focus.</summary>
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        _keyboardFocused = e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional;
        InvalidateVisual();
    }

    /// <summary>Hides the focus ring when the surface loses keyboard focus.</summary>
    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        _keyboardFocused = false;
        InvalidateVisual();
    }

    /// <summary>Redraws the surface when a visual property changes.</summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEnabledProperty)
        {
            Opacity = IsEnabled ? 1.0 : DisabledOpacity;
            if (!IsEnabled && (_pressed || _hovered))
            {
                _pressed = false;
                _hovered = false;
                FadeOutRipple();
            }
            InvalidateVisual();
        }
        else if (change.Property == IsFocusedProperty || change.Property == BoundsProperty)
        {
            InvalidateVisual();
        }
    }

    /// <summary>Shows the hover state when the pointer enters the surface.</summary>
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (!IsEnabled || !_interactive) return;
        _hovered = true;
        InvalidateVisual();
    }

    /// <summary>Clears the hover state when the pointer leaves the surface.</summary>
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hovered = false;
        if (_pressed)
        {
            _pressed = false;
            FadeOutRipple();
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            InvalidateVisual();
        }
    }

    /// <summary>Shows the pressed state and starts a ripple at the pointer position.</summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEnabled || !_interactive) return;

        var props = e.GetCurrentPoint(this).Properties;
        if (!props.IsLeftButtonPressed && e.Pointer.Type == PointerType.Mouse)
            return;

        _keyboardFocused = false;
        _pressed = true;
        _rippleOrigin = e.GetPosition(this);
        _rippleProgress = 0.08;
        _rippleAlpha = 0.14;
        Pressed?.Invoke(this, EventArgs.Empty);

        MavMotion.Cancel(this, "ink_fade");
        MavMotion.Animate(this, "ink_expand", _rippleProgress, 1.0, MavMotion.Medium2, MavMotion.CubicOut, v =>
        {
            _rippleProgress = v;
            InvalidateVisual();
        });
    }

    /// <summary>Moves the ripple origin while the pointer is pressed.</summary>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_pressed) return;
        var pt = e.GetPosition(this);
        if (pt.X < -12 || pt.Y < -12 || pt.X > Bounds.Width + 12 || pt.Y > Bounds.Height + 12)
        {
            _pressed = false;
            FadeOutRipple();
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Ends the pressed state and fades out the ripple.</summary>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_interactive) return;

        var wasPressed = _pressed;
        _pressed = false;

        if (e.Handled)
        {
            FadeOutRipple();
            return;
        }

        var pt = e.GetPosition(this);
        var inside = pt.X >= -8 && pt.Y >= -8 && pt.X <= Bounds.Width + 8 && pt.Y <= Bounds.Height + 8;

        FadeOutRipple();
        Released?.Invoke(this, EventArgs.Empty);

        if (wasPressed && inside && IsEnabled)
        {
            e.Handled = true;
            TriggerClick();
        }
    }

    /// <summary>Ends the pressed state when the pointer capture is lost.</summary>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_pressed)
        {
            _pressed = false;
            FadeOutRipple();
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Starts the pressed state for Space and Enter.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!IsEnabled || !_interactive) return;

        if (e.Key is Key.Space or Key.Enter)
        {
            e.Handled = true;
            TriggerKeyboardClick();
        }
    }

    /// <summary>Plays the keyboard press animation, as if the user had clicked.</summary>
    public void TriggerKeyboardClick()
    {
        if (!IsEnabled || !_interactive) return;
        _rippleOrigin = new Point(Bounds.Width / 2, Bounds.Height / 2);
        _rippleProgress = 0.2;
        _rippleAlpha = 0.14;
        MavMotion.Animate(this, "ink_expand", 0.2, 1.0, MavMotion.Short4, MavMotion.CubicOut, v =>
        {
            _rippleProgress = v;
            InvalidateVisual();
        }, FadeOutRipple);

        TriggerClick();
    }

    private void TriggerClick()
    {
        Clicked?.Invoke(this, EventArgs.Empty);
        if (_command?.CanExecute(_commandParameter) == true)
            _command.Execute(_commandParameter);
    }

    private void FadeOutRipple()
    {
        var startAlpha = _rippleAlpha;
        var startProg = _rippleProgress;
        MavMotion.Animate(this, "ink_fade", 0, 1, MavMotion.Medium1, MavMotion.CubicOut, v =>
        {
            _rippleProgress = Math.Min(1.0, startProg + (1.0 - startProg) * v);
            _rippleAlpha = startAlpha * (1.0 - v);
            InvalidateVisual();
        }, () =>
        {
            _rippleAlpha = 0;
            InvalidateVisual();
        });
    }

    /// <summary>Draws the background, the state layer and the ripple.</summary>
    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0)
        {
            base.Render(context);
            return;
        }

        var rect = new Rect(0, 0, w, h);
        var half = Math.Min(w, h) / 2.0;
        var tl = Math.Min(_radii.TopLeft, half);
        var tr = Math.Min(_radii.TopRight, half);
        var br = Math.Min(_radii.BottomRight, half);
        var bl = Math.Min(_radii.BottomLeft, half);
        var rrect = new RoundedRect(rect, new Vector(tl, tl), new Vector(tr, tr), new Vector(br, br), new Vector(bl, bl));

        // Always draw at least a Transparent brush so the surface bounds are hit-testable.
        context.DrawRectangle(_fillBrush, null, rrect, _cachedShadows);

        // State overlay (hover / focus / explicit StateOverlayOpacity)
        float stateAlpha = _stateOverlayOpacity;
        if (_interactive)
        {
            if (_hovered) stateAlpha = Math.Max(stateAlpha, MavStateLayerTokens.Default.Hover);
            if (IsFocused) stateAlpha = Math.Max(stateAlpha, MavStateLayerTokens.Default.Focus);
        }

        if (stateAlpha > 0.001f)
        {
            var stateBrush = MavTheme.BrushOf(_rippleColor.WithAlpha(stateAlpha));
            context.DrawRectangle(stateBrush, null, rrect);
        }

        // Expanding radial ripple clipped to the rounded rectangle
        if (_interactive && _rippleAlpha > 0.002)
        {
            var rippleBrush = MavTheme.BrushOf(_rippleColor.WithAlpha((float)_rippleAlpha));
            using (context.PushClip(rrect))
            {
                var maxDx = Math.Max(_rippleOrigin.X, w - _rippleOrigin.X);
                var maxDy = Math.Max(_rippleOrigin.Y, h - _rippleOrigin.Y);
                var maxR = Math.Sqrt(maxDx * maxDx + maxDy * maxDy) * 1.08;
                var r = Math.Max(8.0, maxR * _rippleProgress);
                context.DrawEllipse(rippleBrush, null, _rippleOrigin, r, r);
            }
        }

        // Border stroke
        if (_strokePen is not null)
        {
            var inset = _strokeThickness / 2.0;
            var strokeRect = new Rect(
                inset,
                inset,
                Math.Max(0, w - _strokeThickness),
                Math.Max(0, h - _strokeThickness));
            var strokeRRect = new RoundedRect(
                strokeRect,
                new Vector(Math.Max(0, tl - inset), Math.Max(0, tl - inset)),
                new Vector(Math.Max(0, tr - inset), Math.Max(0, tr - inset)),
                new Vector(Math.Max(0, br - inset), Math.Max(0, br - inset)),
                new Vector(Math.Max(0, bl - inset), Math.Max(0, bl - inset)));
            context.DrawRectangle(null, _strokePen, strokeRRect);
        }

        // MD3 Keyboard Focus Ring (2dp Secondary indicator with 2dp outer offset)
        if (_interactive && IsFocused && _keyboardFocused)
        {
            const double ringWidth = 2.0;
            const double ringOffset = 2.0;
            var ringRect = rect.Inflate(ringOffset);
            var ringRRect = new RoundedRect(
                ringRect,
                new Vector(tl + ringOffset, tl + ringOffset),
                new Vector(tr + ringOffset, tr + ringOffset),
                new Vector(br + ringOffset, br + ringOffset),
                new Vector(bl + ringOffset, bl + ringOffset));
            var ringPen = MavTheme.PenOf(MavTheme.Current.Color.Secondary, ringWidth);
            context.DrawRectangle(null, ringPen, ringRRect);
        }

        base.Render(context);
    }
}
