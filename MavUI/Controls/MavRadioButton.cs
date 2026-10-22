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
/// Material Design 3 RadioButton with optional label and group value.
/// </summary>
public class MavRadioButton : MavContentView
{
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<MavRadioButton, bool>(nameof(IsChecked), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<MavRadioButton, string?>(nameof(Label));

    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<MavRadioButton, object?>(nameof(Value));

    public static readonly StyledProperty<string?> GroupNameProperty =
        AvaloniaProperty.Register<MavRadioButton, string?>(nameof(GroupName));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavRadioButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavRadioButton, object?>(nameof(CommandParameter));

    private readonly RadioGlyph _glyph;
    private readonly TextBlock _label;
    private double _dotProgress;
    private bool _hovered;
    private bool _pressed;

    public event EventHandler<bool>? CheckedChanged;

    static MavRadioButton()
    {
        IsCheckedProperty.Changed.AddClassHandler<MavRadioButton>((rb, e) => rb.OnCheckedChanged(e.GetNewValue<bool>()));
        LabelProperty.Changed.AddClassHandler<MavRadioButton>((rb, e) =>
        {
            var text = e.GetNewValue<string?>();
            rb._label.Text = text ?? string.Empty;
            rb._label.IsVisible = !string.IsNullOrEmpty(text);
            MavAutomation.SetName(rb, text);
        });
    }

    /// <summary>Creates an unchecked radio button without a label or value.</summary>
    public MavRadioButton() : this(null, false, null)
    {
    }

    /// <summary>Creates a radio button with an optional label, checked state and value.</summary>
    public MavRadioButton(string? label = null, bool isChecked = false, object? value = null)
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

        MavAutomation.SetName(this, Label);
        MavAutomation.SetControlType(this, AutomationControlType.RadioButton);
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
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

    /// <summary>Redraws the radio button when a visual property changes.</summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEnabledProperty || change.Property == IsFocusedProperty)
        {
            ApplyTheme(MavTheme.Current);
            _glyph.InvalidateVisual();
        }
    }

    /// <summary>Shows the hover state when the pointer enters the radio button.</summary>
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (!IsEnabled) return;
        _hovered = true;
        _glyph.InvalidateVisual();
    }

    /// <summary>Clears the hover state when the pointer leaves the radio button.</summary>
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hovered = false;
        _pressed = false;
        _glyph.InvalidateVisual();
    }

    /// <summary>Starts a press on the radio button and shows the pressed state.</summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEnabled) return;
        _pressed = true;
        _glyph.InvalidateVisual();
    }

    /// <summary>Completes a click and selects the radio button when the pointer is released over it.</summary>
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

    /// <summary>Selects the radio button with the Space key.</summary>
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
            foreach (var sibling in parentPanel.Children.OfType<MavRadioButton>())
            {
                if (!ReferenceEquals(sibling, this) &&
                    string.Equals(sibling.GroupName, GroupName, StringComparison.Ordinal) &&
                    sibling.IsChecked)
                {
                    sibling.IsChecked = false;
                }
            }
        }

        MavMotion.Animate(this, "rb_dot", _dotProgress, on ? 1.0 : 0.0, MavMotion.Short3, MavMotion.CubicOut, v =>
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

    private void ApplyTheme(MavTheme t)
    {
        _label.Apply(MavTypeRole.BodyLarge, t.Typography);
        _label.Foreground = MavTheme.BrushOf(
            IsEnabled ? t.Color.OnSurface : t.Color.OnSurface.WithAlpha(t.StateLayer.DisabledContent));
        _glyph.InvalidateVisual();
    }

    private sealed class RadioGlyph(MavRadioButton owner) : Control
    {
        public override void Render(DrawingContext context)
        {
            var t = MavTheme.Current;
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
                    MavTheme.BrushOf(haloBase.WithAlpha(owner._pressed ? 0.12f : 0.08f)),
                    null,
                    new Point(cx, cy),
                    20,
                    20);
            }

            var ringColor = MavColorScheme.Blend(inactive, active, owner._dotProgress);
            context.DrawEllipse(null, MavTheme.PenOf(ringColor, 2), new Point(cx, cy), 9, 9);

            if (owner._dotProgress > 0.01)
            {
                var r = 5.0 * owner._dotProgress;
                context.DrawEllipse(MavTheme.BrushOf(active), null, new Point(cx, cy), r, r);
            }
        }
    }
}
