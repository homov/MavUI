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
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Material Design 3 Switch (52x32 track with morphing 16/24/28dp thumb and optional check/close icons).
/// </summary>
public class MdSwitch : Control
{
    public static readonly StyledProperty<bool> IsToggledProperty =
        AvaloniaProperty.Register<MdSwitch, bool>(nameof(IsToggled), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> ShowIconsProperty =
        AvaloniaProperty.Register<MdSwitch, bool>(nameof(ShowIcons));

    public static readonly StyledProperty<bool> ShowThumbIconProperty =
        AvaloniaProperty.Register<MdSwitch, bool>(nameof(ShowThumbIcon));

    public static readonly StyledProperty<string?> OnIconProperty =
        AvaloniaProperty.Register<MdSwitch, string?>(nameof(OnIcon));

    public static readonly StyledProperty<string?> OffIconProperty =
        AvaloniaProperty.Register<MdSwitch, string?>(nameof(OffIcon));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MdSwitch, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MdSwitch, object?>(nameof(CommandParameter));

    private double _progress;
    private double _pressProgress;
    private bool _hovered;
    private bool _pressed;

    public event EventHandler<bool>? Toggled;

    static MdSwitch()
    {
        IsToggledProperty.Changed.AddClassHandler<MdSwitch>((s, e) => s.OnToggledChanged(e.GetNewValue<bool>()));
        ShowIconsProperty.Changed.AddClassHandler<MdSwitch>((s, _) => s.InvalidateVisual());
        ShowThumbIconProperty.Changed.AddClassHandler<MdSwitch>((s, e) =>
        {
            s.ShowIcons = e.GetNewValue<bool>();
            s.InvalidateVisual();
        });
    }

    public MdSwitch() : this(false, false)
    {
    }

    public MdSwitch(bool isToggled = false, bool showIcons = false)
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
        MdAutomation.SetControlType(this, AutomationControlType.CheckBox);

        MdTheme.Observe(this, (self, _) => self.InvalidateVisual());
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

    protected override Size MeasureOverride(Size availableSize) => new(52, 32);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEnabledProperty || change.Property == IsFocusedProperty)
            InvalidateVisual();
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (!IsEnabled) return;
        _hovered = true;
        InvalidateVisual();
    }

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

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEnabled) return;
        _pressed = true;
        AnimatePress(1);
    }

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
        MdMotion.Animate(this, "sw_press", _pressProgress, target, MdMotion.Short3, MdMotion.CubicOut, v =>
        {
            _pressProgress = v;
            InvalidateVisual();
        });
    }

    private void OnToggledChanged(bool on)
    {
        var target = on ? 1.0 : 0.0;
        MdMotion.Animate(this, "sw_toggle", _progress, target, MdMotion.Short4, MdMotion.CubicOut, v =>
        {
            _progress = v;
            InvalidateVisual();
        });
        Toggled?.Invoke(this, on);
        var param = CommandParameter ?? on;
        if (Command?.CanExecute(param) == true)
            Command.Execute(param);
    }

    public override void Render(DrawingContext context)
    {
        var t = MdTheme.Current;
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

        var trackFill = MdColorScheme.Blend(trackOff, trackOn, _progress);
        var rrect = new RoundedRect(rect, 16);
        context.DrawRectangle(MdTheme.BrushOf(trackFill), null, rrect);

        var borderAlpha = 1.0 - _progress;
        if (borderAlpha > 0.01)
        {
            var strokeRect = new RoundedRect(new Rect(1, 1, w - 2, h - 2), 15);
            context.DrawRectangle(null, MdTheme.PenOf(borderOff.WithAlpha((float)borderAlpha), 2), strokeRect);
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
            context.DrawEllipse(MdTheme.BrushOf(haloColor), null, new Point(cx, cy), 20, 20);
        }

        var thumbColor = MdColorScheme.Blend(thumbOff, thumbOn, _progress);
        context.DrawEllipse(MdTheme.BrushOf(thumbColor), null, new Point(cx, cy), thumbSize / 2.0, thumbSize / 2.0);

        if (hasIcons && thumbSize >= 20)
        {
            var glyph = _progress >= 0.5
                ? (!string.IsNullOrEmpty(OnIcon) ? OnIcon! : MdIcons.Check)
                : (!string.IsNullOrEmpty(OffIcon) ? OffIcon! : MdIcons.Close);
            var iconCol = _progress >= 0.5 ? c.OnPrimaryContainer : c.SurfaceContainerHighest;
            if (!IsEnabled) iconCol = c.OnSurface.WithAlpha(0.38f);

            var ft = MdTextMetrics.GetFormattedText(
                glyph,
                16,
                MdFonts.MaterialIconsTypeface,
                iconCol);
            context.DrawText(ft, new Point(cx - ft.Width / 2.0, cy - ft.Height / 2.0));
        }
    }
}

public enum MdCheckState
{
    Unchecked,
    Checked,
    Indeterminate,
}

/// <summary>
/// Material Design 3 tri-state CheckBox with optional label.
/// </summary>
public class MdCheckBox : MdContentView
{
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<MdCheckBox, bool>(nameof(IsChecked), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<MdCheckBox, bool>(nameof(IsIndeterminate), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsThreeStateProperty =
        AvaloniaProperty.Register<MdCheckBox, bool>(nameof(IsThreeState));

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<MdCheckBox, string?>(nameof(Label));

    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<MdCheckBox, bool>(nameof(HasError));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MdCheckBox, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MdCheckBox, object?>(nameof(CommandParameter));

    private readonly CheckBoxGlyph _box;
    private readonly TextBlock _label;
    private double _animProgress;
    private bool _hovered;
    private bool _pressed;

    public event EventHandler<bool>? CheckedChanged;
    public event EventHandler<MdCheckState>? StateChanged;

    static MdCheckBox()
    {
        IsCheckedProperty.Changed.AddClassHandler<MdCheckBox>((cb, e) => cb.OnStatePropertyChanged());
        IsIndeterminateProperty.Changed.AddClassHandler<MdCheckBox>((cb, _) => cb.OnStatePropertyChanged());
        LabelProperty.Changed.AddClassHandler<MdCheckBox>((cb, e) =>
        {
            var text = e.GetNewValue<string?>();
            cb._label.Text = text ?? string.Empty;
            cb._label.IsVisible = !string.IsNullOrEmpty(text);
            MdAutomation.SetName(cb, text);
        });
        HasErrorProperty.Changed.AddClassHandler<MdCheckBox>((cb, _) => cb._box.InvalidateVisual());
    }

    public MdCheckBox() : this(null, false, false)
    {
    }

    public MdCheckBox(string? label = null, bool isChecked = false, bool isIndeterminate = false)
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

        Content = row;

        _animProgress = (isChecked || isIndeterminate) ? 1.0 : 0.0;
        if (label is not null) Label = label;
        if (isIndeterminate) IsIndeterminate = true;
        else if (isChecked) IsChecked = true;

        MdAutomation.SetName(this, Label);
        MdAutomation.SetControlType(this, AutomationControlType.CheckBox);
        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
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

    public MdCheckState State
    {
        get => IsIndeterminate ? MdCheckState.Indeterminate : (IsChecked ? MdCheckState.Checked : MdCheckState.Unchecked);
        set
        {
            switch (value)
            {
                case MdCheckState.Indeterminate:
                    IsIndeterminate = true;
                    break;
                case MdCheckState.Checked:
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEnabledProperty || change.Property == IsFocusedProperty)
        {
            ApplyTheme(MdTheme.Current);
            _box.InvalidateVisual();
        }
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (!IsEnabled) return;
        _hovered = true;
        _box.InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hovered = false;
        _pressed = false;
        _box.InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEnabled) return;
        _pressed = true;
        _box.InvalidateVisual();
    }

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
        MdMotion.Animate(this, "cb_anim", _animProgress, target, MdMotion.Short3, MdMotion.CubicOut, v =>
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

    private void ApplyTheme(MdTheme t)
    {
        _label.Apply(MdTypeRole.BodyLarge, t.Typography);
        _label.Foreground = MdTheme.BrushOf(
            IsEnabled ? t.Color.OnSurface : t.Color.OnSurface.WithAlpha(t.StateLayer.DisabledContent));
        _box.InvalidateVisual();
    }

    private sealed class CheckBoxGlyph(MdCheckBox owner) : Control
    {
        public override void Render(DrawingContext context)
        {
            var t = MdTheme.Current;
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
                context.DrawEllipse(MdTheme.BrushOf(haloBase.WithAlpha(alpha)), null, new Point(cx, cy), 20, 20);
            }

            var boxRect = new Rect(cx - 9, cy - 9, 18, 18);
            var rrect = new RoundedRect(boxRect, 2);

            if (owner._animProgress < 0.99)
            {
                context.DrawRectangle(null, MdTheme.PenOf(outlineColor, 2), rrect);
            }

            if (owner._animProgress > 0.01)
            {
                context.DrawRectangle(
                    MdTheme.BrushOf(activeColor.WithAlpha((float)owner._animProgress)),
                    null,
                    rrect);

                var markPen = MdTheme.PenOf(
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

/// <summary>
/// Material Design 3 RadioButton with optional label and group value.
/// </summary>
public class MdRadioButton : MdContentView
{
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<MdRadioButton, bool>(nameof(IsChecked), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<MdRadioButton, string?>(nameof(Label));

    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<MdRadioButton, object?>(nameof(Value));

    public static readonly StyledProperty<string?> GroupNameProperty =
        AvaloniaProperty.Register<MdRadioButton, string?>(nameof(GroupName));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MdRadioButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MdRadioButton, object?>(nameof(CommandParameter));

    private readonly RadioGlyph _glyph;
    private readonly TextBlock _label;
    private double _dotProgress;
    private bool _hovered;
    private bool _pressed;

    public event EventHandler<bool>? CheckedChanged;

    static MdRadioButton()
    {
        IsCheckedProperty.Changed.AddClassHandler<MdRadioButton>((rb, e) => rb.OnCheckedChanged(e.GetNewValue<bool>()));
        LabelProperty.Changed.AddClassHandler<MdRadioButton>((rb, e) =>
        {
            var text = e.GetNewValue<string?>();
            rb._label.Text = text ?? string.Empty;
            rb._label.IsVisible = !string.IsNullOrEmpty(text);
            MdAutomation.SetName(rb, text);
        });
    }

    public MdRadioButton() : this(null, false, null)
    {
    }

    public MdRadioButton(string? label = null, bool isChecked = false, object? value = null)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Hand);
        Background = Brushes.Transparent;

        _glyph = new RadioGlyph(this)
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
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.Children.Add(_glyph);
        row.Children.Add(_label);

        Content = row;

        _dotProgress = isChecked ? 1.0 : 0.0;
        if (label is not null) Label = label;
        if (value is not null) Value = value;
        if (isChecked) IsChecked = true;

        MdAutomation.SetName(this, Label);
        MdAutomation.SetControlType(this, AutomationControlType.RadioButton);
        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    [Content]
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? GroupName
    {
        get => GetValue(GroupNameProperty);
        set => SetValue(GroupNameProperty, value);
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEnabledProperty || change.Property == IsFocusedProperty)
        {
            ApplyTheme(MdTheme.Current);
            _glyph.InvalidateVisual();
        }
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (!IsEnabled) return;
        _hovered = true;
        _glyph.InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hovered = false;
        _pressed = false;
        _glyph.InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEnabled) return;
        _pressed = true;
        _glyph.InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!IsEnabled) return;
        var wasPressed = _pressed;
        _pressed = false;
        _glyph.InvalidateVisual();
        if (wasPressed)
        {
            e.Handled = true;
            IsChecked = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!IsEnabled) return;
        if (e.Key is Key.Space or Key.Enter)
        {
            e.Handled = true;
            IsChecked = true;
        }
    }

    private void OnCheckedChanged(bool on)
    {
        if (on && !string.IsNullOrEmpty(GroupName) && Parent is Panel parentPanel)
        {
            foreach (var sibling in parentPanel.Children.OfType<MdRadioButton>())
            {
                if (!ReferenceEquals(sibling, this) &&
                    string.Equals(sibling.GroupName, GroupName, StringComparison.Ordinal) &&
                    sibling.IsChecked)
                {
                    sibling.IsChecked = false;
                }
            }
        }

        MdMotion.Animate(this, "rb_dot", _dotProgress, on ? 1.0 : 0.0, MdMotion.Short3, MdMotion.CubicOut, v =>
        {
            _dotProgress = v;
            _glyph.InvalidateVisual();
        });
        CheckedChanged?.Invoke(this, on);
        if (on)
        {
            var param = CommandParameter ?? Value ?? (object?)Label ?? true;
            if (Command?.CanExecute(param) == true)
                Command.Execute(param);
        }
    }

    private void ApplyTheme(MdTheme t)
    {
        _label.Apply(MdTypeRole.BodyLarge, t.Typography);
        _label.Foreground = MdTheme.BrushOf(
            IsEnabled ? t.Color.OnSurface : t.Color.OnSurface.WithAlpha(t.StateLayer.DisabledContent));
        _glyph.InvalidateVisual();
    }

    private sealed class RadioGlyph(MdRadioButton owner) : Control
    {
        public override void Render(DrawingContext context)
        {
            var t = MdTheme.Current;
            var c = t.Color;
            var cx = Bounds.Width / 2.0;
            var cy = Bounds.Height / 2.0;

            context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, Bounds.Width, Bounds.Height));

            var active = c.Primary;
            var inactive = c.OnSurfaceVariant;
            if (!owner.IsEnabled)
            {
                active = c.OnSurface.WithAlpha(0.38f);
                inactive = c.OnSurface.WithAlpha(0.38f);
            }

            if (owner.IsEnabled && (owner._hovered || owner._pressed || owner.IsFocused))
            {
                var haloBase = owner.IsChecked ? active : c.OnSurface;
                context.DrawEllipse(
                    MdTheme.BrushOf(haloBase.WithAlpha(owner._pressed ? 0.12f : 0.08f)),
                    null,
                    new Point(cx, cy),
                    20,
                    20);
            }

            var ringColor = MdColorScheme.Blend(inactive, active, owner._dotProgress);
            context.DrawEllipse(null, MdTheme.PenOf(ringColor, 2), new Point(cx, cy), 9, 9);

            if (owner._dotProgress > 0.01)
            {
                var r = 5.0 * owner._dotProgress;
                context.DrawEllipse(MdTheme.BrushOf(active), null, new Point(cx, cy), r, r);
            }
        }
    }
}

/// <summary>
/// Mutual-exclusion container for <see cref="MdRadioButton"/> items.
/// </summary>
public class MdRadioGroup : StackPanel
{
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MdRadioGroup, int>(nameof(SelectedIndex), -1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<MdRadioGroup, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<System.Collections.IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MdRadioGroup, System.Collections.IEnumerable?>(nameof(ItemsSource));

    private readonly List<MdRadioButton> _buttons = [];
    private readonly MdItemEventWiring<MdRadioButton, EventHandler<bool>> _wiredButtons = new(
        static (button, handler) => button.CheckedChanged += handler,
        static (button, handler) => button.CheckedChanged -= handler);
    private readonly MdCollectionObserver _observer;
    private int _selectedIndex = -1;
    private bool _updating;

    public event EventHandler<int>? SelectedIndexChanged;

    static MdRadioGroup()
    {
        SelectedIndexProperty.Changed.AddClassHandler<MdRadioGroup>((g, e) =>
            g.OnSelectedIndexPropertyChanged(e.GetNewValue<int>()));
        SelectedItemProperty.Changed.AddClassHandler<MdRadioGroup>((g, e) =>
            g.OnSelectedItemPropertyChanged(e.GetNewValue<object?>()));
        ItemsSourceProperty.Changed.AddClassHandler<MdRadioGroup>((g, e) =>
            g.OnItemsSourceChanged(e.GetNewValue<System.Collections.IEnumerable?>()));
    }

    public MdRadioGroup() : this(Orientation.Vertical)
    {
    }

    public MdRadioGroup(Orientation orientation = Orientation.Vertical)
    {
        Orientation = orientation;
        Spacing = 4;
        _observer = new MdCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));
        Children.CollectionChanged += (_, e) =>
        {
            _wiredButtons.HandleCollectionChanged(e);
            SyncChildrenToRadioButtons();
        };
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetValue(SelectedIndexProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public System.Collections.IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private void OnItemsSourceChanged(System.Collections.IEnumerable? src)
    {
        _observer.Attach(src);
        RebuildFromItemsSource(src);
    }

    private void RebuildFromItemsSource(System.Collections.IEnumerable? src)
    {
        if (src is null) return;
        Children.Clear();
        foreach (var item in src)
        {
            if (item is MdRadioButton rb)
                Children.Add(rb);
            else if (item is not null)
                AddOption(item.ToString() ?? string.Empty, item);
        }
    }

    private void OnSelectedIndexPropertyChanged(int value)
    {
        if (_selectedIndex == value) return;
        _selectedIndex = value;
        _updating = true;
        for (int i = 0; i < _buttons.Count; i++)
            _buttons[i].IsChecked = i == value;
        SetValue(SelectedItemProperty, SelectedValue);
        _updating = false;
        SelectedIndexChanged?.Invoke(this, _selectedIndex);
    }

    private void OnSelectedItemPropertyChanged(object? item)
    {
        if (_updating) return;
        if (item is null)
        {
            SelectedIndex = -1;
            return;
        }
        for (int i = 0; i < _buttons.Count; i++)
        {
            var val = _buttons[i].Value ?? _buttons[i].Label;
            if (Equals(val, item) || (item is string s && string.Equals(_buttons[i].Label, s, StringComparison.OrdinalIgnoreCase)))
            {
                SelectedIndex = i;
                return;
            }
        }
    }

    public object? SelectedValue =>
        _selectedIndex >= 0 && _selectedIndex < _buttons.Count
            ? (_buttons[_selectedIndex].Value ?? _buttons[_selectedIndex].Label)
            : null;

    public MdRadioButton AddOption(string label, object? value = null, bool selected = false)
    {
        var rb = new MdRadioButton(label, selected, value);
        AddRadio(rb);
        return rb;
    }

    public void AddRadio(MdRadioButton button)
    {
        if (!Children.Contains(button))
            Children.Add(button);
        else
            SyncChildrenToRadioButtons();
    }

    private void SyncChildrenToRadioButtons()
    {
        _buttons.Clear();
        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i] is not MdRadioButton rb) continue;
            var idx = _buttons.Count;
            _buttons.Add(rb);
            _wiredButtons.Add(rb, (s, on) =>
            {
                if (_updating || !on || s is not MdRadioButton senderBtn) return;
                var index = _buttons.IndexOf(senderBtn);
                if (index >= 0) SelectedIndex = index;
            });
            if (rb.IsChecked)
            {
                _selectedIndex = idx;
                SetValue(SelectedIndexProperty, idx);
            }
        }
    }
}
