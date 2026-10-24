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
/// Material Design 3 tri-state CheckBox with optional label.
/// </summary>
public class MavCheckBox : MavContentView
{
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<MavCheckBox, bool>(nameof(IsChecked), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<MavCheckBox, bool>(nameof(IsIndeterminate), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsThreeStateProperty =
        AvaloniaProperty.Register<MavCheckBox, bool>(nameof(IsThreeState));

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<MavCheckBox, string?>(nameof(Label));

    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<MavCheckBox, bool>(nameof(HasError));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavCheckBox, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavCheckBox, object?>(nameof(CommandParameter));

    private readonly CheckBoxGlyph _box;
    private readonly TextBlock _label;
    private double _animProgress;

    /// <summary>How much of the mark is drawn: 0 off, 1 on. Exposed for tests.</summary>
    internal double AnimationProgress => _animProgress;
    private bool _hovered;
    private bool _pressed;

    public event EventHandler<bool>? CheckedChanged;
    public event EventHandler<MavCheckState>? StateChanged;

    static MavCheckBox()
    {
        IsCheckedProperty.Changed.AddClassHandler<MavCheckBox>((cb, e) => cb.OnStatePropertyChanged());
        IsIndeterminateProperty.Changed.AddClassHandler<MavCheckBox>((cb, _) => cb.OnStatePropertyChanged());
        LabelProperty.Changed.AddClassHandler<MavCheckBox>((cb, e) =>
        {
            var text = e.GetNewValue<string?>();
            cb._label.Text = text ?? string.Empty;
            cb._label.IsVisible = !string.IsNullOrEmpty(text);
            MavAutomation.SetName(cb, text);
        });
        HasErrorProperty.Changed.AddClassHandler<MavCheckBox>((cb, _) => cb._box.InvalidateVisual());
    }

    /// <summary>Creates an unchecked checkbox without a label.</summary>
    public MavCheckBox() : this(null, false, false)
    {
    }

    /// <summary>Creates a checkbox with an optional label and the given checked and indeterminate state.</summary>
    public MavCheckBox(string? label = null, bool isChecked = false, bool isIndeterminate = false)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Hand);
        Background = Brushes.Transparent;

        _box = new CheckBoxGlyph(this)
        {
            Width = 40,
            Height = 40,
            VerticalAlignment = VerticalAlignment.Center,
        };

        _label = new TextBlock
        {
            Text = label ?? string.Empty,
            IsVisible = !string.IsNullOrEmpty(label),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 8, 0),
        };

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 0,
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.Children.Add(_box);
        row.Children.Add(_label);

        Child = row;

        _animProgress = (isChecked || isIndeterminate) ? 1.0 : 0.0;
        if (label is not null) Label = label;
        if (isIndeterminate) IsIndeterminate = true;
        else if (isChecked) IsChecked = true;

        MavAutomation.SetName(this, Label);
        MavAutomation.SetControlType(this, AutomationControlType.CheckBox);
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    public bool IsThreeState
    {
        get => GetValue(IsThreeStateProperty);
        set => SetValue(IsThreeStateProperty, value);
    }

    [Content]
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool HasError
    {
        get => GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }

    public bool IsError
    {
        get => HasError;
        set => HasError = value;
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

    public MavCheckState State
    {
        get => IsIndeterminate ? MavCheckState.Indeterminate : (IsChecked ? MavCheckState.Checked : MavCheckState.Unchecked);
        set
        {
            switch (value)
            {
                case MavCheckState.Indeterminate:
                    IsIndeterminate = true;
                    break;
                case MavCheckState.Checked:
                    IsIndeterminate = false;
                    IsChecked = true;
                    break;
                default:
                    IsIndeterminate = false;
                    IsChecked = false;
                    break;
            }
        }
    }

    /// <summary>Redraws the checkbox when a visual property changes.</summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEnabledProperty || change.Property == IsFocusedProperty)
        {
            ApplyTheme(MavTheme.Current);
            _box.InvalidateVisual();
        }
    }

    /// <summary>Shows the hover state when the pointer enters the checkbox.</summary>
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (!IsEnabled) return;
        _hovered = true;
        _box.InvalidateVisual();
    }

    /// <summary>Clears the hover state when the pointer leaves the checkbox.</summary>
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hovered = false;
        _pressed = false;
        _box.InvalidateVisual();
    }

    /// <summary>Starts a press on the checkbox and shows the pressed state.</summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEnabled) return;
        _pressed = true;
        _box.InvalidateVisual();
    }

    /// <summary>Completes a click and toggles the checkbox when the pointer is released over it.</summary>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!IsEnabled) return;
        var wasPressed = _pressed;
        _pressed = false;
        _box.InvalidateVisual();
        if (wasPressed)
        {
            e.Handled = true;
            Toggle();
        }
    }

    /// <summary>Toggles the checkbox with the Space key.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!IsEnabled) return;
        if (e.Key is Key.Space or Key.Enter)
        {
            e.Handled = true;
            Toggle();
        }
    }

    /// <summary>Switches between checked and unchecked, and clears the indeterminate state.</summary>
    public void Toggle()
    {
        if (IsThreeState)
        {
            if (IsIndeterminate)
            {
                IsIndeterminate = false;
                IsChecked = false;
            }
            else if (!IsChecked)
            {
                IsChecked = true;
            }
            else
            {
                IsIndeterminate = true;
            }
        }
        else
        {
            if (IsIndeterminate)
            {
                IsIndeterminate = false;
                IsChecked = true;
            }
            else
            {
                IsChecked = !IsChecked;
            }
        }
    }

    private void OnStatePropertyChanged()
    {
        var active = IsChecked || IsIndeterminate;
        var target = active ? 1.0 : 0.0;
        MavMotion.AnimateState(this, "cb_anim", _animProgress, target, MavMotion.Short3, MavMotion.CubicOut, v =>
        {
            _animProgress = v;
            _box.InvalidateVisual();
        });
        CheckedChanged?.Invoke(this, IsChecked);
        StateChanged?.Invoke(this, State);
        var param = CommandParameter ?? IsChecked;
        if (Command?.CanExecute(param) == true)
            Command.Execute(param);
    }

    private void ApplyTheme(MavTheme t)
    {
        _label.Apply(MavTypeRole.BodyLarge, t.Typography);
        _label.Foreground = MavTheme.BrushOf(
            IsEnabled ? t.Color.OnSurface : t.Color.OnSurface.WithAlpha(t.StateLayer.DisabledContent));
        _box.InvalidateVisual();
    }

    private sealed class CheckBoxGlyph(MavCheckBox owner) : Control
    {
        /// <summary>
        /// Draws the check box box, check mark, and state layer.
        /// </summary>
        public override void Render(DrawingContext context)
        {
            var t = MavTheme.Current;
            var c = t.Color;
            var cx = Bounds.Width / 2.0;
            var cy = Bounds.Height / 2.0;

            context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, Bounds.Width, Bounds.Height));

            var activeColor = owner.HasError ? c.Error : c.Primary;
            var onActiveColor = owner.HasError ? c.OnError : c.OnPrimary;
            var outlineColor = owner.HasError ? c.Error : c.OnSurfaceVariant;

            if (!owner.IsEnabled)
            {
                activeColor = c.OnSurface.WithAlpha(0.38f);
                outlineColor = c.OnSurface.WithAlpha(0.38f);
                onActiveColor = c.Surface;
            }

            if (owner.IsEnabled && (owner._hovered || owner._pressed || owner.IsFocused))
            {
                var haloBase = (owner.IsChecked || owner.IsIndeterminate) ? activeColor : c.OnSurface;
                var alpha = owner._pressed ? 0.12f : 0.08f;
                context.DrawEllipse(MavTheme.BrushOf(haloBase.WithAlpha(alpha)), null, new Point(cx, cy), 20, 20);
            }

            var boxRect = new Rect(cx - 9, cy - 9, 18, 18);
            var rrect = new RoundedRect(boxRect, 2);

            if (owner._animProgress < 0.99)
            {
                context.DrawRectangle(null, MavTheme.PenOf(outlineColor, 2), rrect);
            }

            if (owner._animProgress > 0.01)
            {
                context.DrawRectangle(
                    MavTheme.BrushOf(activeColor.WithAlpha((float)owner._animProgress)),
                    null,
                    rrect);

                var markPen = MavTheme.PenOf(
                    onActiveColor.WithAlpha((float)owner._animProgress),
                    2,
                    PenLineCap.Round,
                    PenLineJoin.Round);

                if (owner.IsIndeterminate)
                {
                    context.DrawLine(markPen, new Point(cx - 5, cy), new Point(cx + 5, cy));
                }
                else
                {
                    var p1 = new Point(cx - 5.0, cy + 0.2);
                    var p2 = new Point(cx - 1.5, cy + 3.8);
                    var p3 = new Point(cx + 5.2, cy - 3.6);
                    context.DrawLine(markPen, p1, p2);
                    context.DrawLine(markPen, p2, p3);
                }
            }
        }
    }
}
