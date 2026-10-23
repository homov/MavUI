using System.Collections;
using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Numeric Stepper Input with decrement/increment buttons, min/max clamping,
/// step increments, keyboard Up/Down support, and optional label.
/// </summary>
public class MavNumberBox : MavContentView
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MavNumberBox, double>(
            nameof(Value),
            0.0,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<MavNumberBox, double>(nameof(Minimum), double.MinValue);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<MavNumberBox, double>(nameof(Maximum), double.MaxValue);

    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<MavNumberBox, double>(nameof(Step), 1.0);

    public static readonly StyledProperty<int> DecimalPlacesProperty =
        AvaloniaProperty.Register<MavNumberBox, int>(nameof(DecimalPlaces), 0);

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<MavNumberBox, string?>(nameof(Label));

    public static readonly StyledProperty<string?> SuffixProperty =
        AvaloniaProperty.Register<MavNumberBox, string?>(nameof(Suffix));

    public static readonly StyledProperty<string?> FormatStringProperty =
        AvaloniaProperty.Register<MavNumberBox, string?>(nameof(FormatString));

    private readonly MavSurface _box;
    private readonly TextBlock _label;
    private readonly MavIconButton _decBtn;
    private readonly MavNativeEntry _entry;
    private readonly TextBlock _suffixLbl;
    private readonly MavIconButton _incBtn;
    private bool _updating;

    public event EventHandler<double>? ValueChanged;

    static MavNumberBox()
    {
        ValueProperty.Changed.AddClassHandler<MavNumberBox>((n, e) => n.OnValuePropertyChanged(e.GetNewValue<double>()));
        MinimumProperty.Changed.AddClassHandler<MavNumberBox>((n, _) => n.SyncButtons());
        MaximumProperty.Changed.AddClassHandler<MavNumberBox>((n, _) => n.SyncButtons());
        DecimalPlacesProperty.Changed.AddClassHandler<MavNumberBox>((n, _) => n.FormatEntry());
        FormatStringProperty.Changed.AddClassHandler<MavNumberBox>((n, _) => n.FormatEntry());
        LabelProperty.Changed.AddClassHandler<MavNumberBox>((n, e) =>
        {
            var txt = e.GetNewValue<string?>();
            n._label.Text = txt ?? string.Empty;
            n._label.IsVisible = !string.IsNullOrEmpty(txt);
            MavAutomation.SetName(n, txt);
        });
        SuffixProperty.Changed.AddClassHandler<MavNumberBox>((n, e) =>
        {
            var txt = e.GetNewValue<string?>();
            n._suffixLbl.Text = txt ?? string.Empty;
            n._suffixLbl.IsVisible = !string.IsNullOrEmpty(txt);
        });
    }

    /// <summary>Creates a number box with the range 0 to 100 and step 1.</summary>
    public MavNumberBox() : this(0, 0, 100, 1, 0, null, null)
    {
    }

    /// <summary>Creates a number box with the given value range, step and formatting.</summary>
    public MavNumberBox(
        double value = 0,
        double min = 0,
        double max = 100,
        double step = 1,
        int decimalPlaces = 0,
        string? label = null,
        string? suffix = null)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;

        _label = new TextBlock
        {
            Text = label ?? string.Empty,
            IsVisible = !string.IsNullOrEmpty(label),
            Margin = new Thickness(4, 0, 4, 4),
        };

        _decBtn = new MavIconButton(MavIcons.Remove, MavIconButtonVariant.Standard)
        {
            ButtonSize = 36,
        };
        _decBtn.Clicked += (_, _) => Decrement();

        _entry = new MavNativeEntry
        {
            MinWidth = 64,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        TextInputOptions.SetContentType(_entry, TextInputContentType.Number);
        TextInputOptions.SetReturnKeyType(_entry, TextInputReturnKeyType.Done);
        _entry.LostFocus += (_, _) => CommitEntryText();
        _entry.Completed += (_, _) => CommitEntryText();
        _entry.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Up)
            {
                Increment();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                Decrement();
                e.Handled = true;
            }
        };

        _suffixLbl = new TextBlock
        {
            Text = suffix ?? string.Empty,
            IsVisible = !string.IsNullOrEmpty(suffix),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0),
        };

        _incBtn = new MavIconButton(MavIcons.Add, MavIconButtonVariant.Standard)
        {
            ButtonSize = 36,
        };
        _incBtn.Clicked += (_, _) => Increment();

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        row.Add(_decBtn, 0, 0);
        row.Add(_entry, 1, 0);
        row.Add(_suffixLbl, 2, 0);
        row.Add(_incBtn, 3, 0);

        _box = new MavSurface
        {
            MinHeight = 48,
            MinWidth = 156,
            Radius = 12,
            Padding = new Thickness(4, 2),
            BorderStrokeThickness = 1,
            Child = row,
        };

        var root = new StackPanel
        {
            Orientation = Orientation.Vertical,
        };
        root.Children.Add(_label);
        root.Children.Add(_box);

        Child = root;

        Minimum = min;
        Maximum = max;
        Step = step;
        DecimalPlaces = decimalPlaces;
        if (label is not null) Label = label;
        if (suffix is not null) Suffix = suffix;
        Value = Math.Clamp(value, min, max);
        FormatEntry();
        SyncButtons();
        MavAutomation.SetName(this, Label);
        MavAutomation.SetControlType(this, AutomationControlType.Edit);

        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, Math.Clamp(value, Minimum, Maximum));
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Step
    {
        get => GetValue(StepProperty);
        set => SetValue(StepProperty, Math.Max(1e-6, value));
    }

    public int DecimalPlaces
    {
        get => GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, Math.Clamp(value, 0, 6));
    }

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Suffix
    {
        get => GetValue(SuffixProperty);
        set => SetValue(SuffixProperty, value);
    }

    public string? FormatString
    {
        get => GetValue(FormatStringProperty);
        set => SetValue(FormatStringProperty, value);
    }

    /// <summary>Changes the value by one step per wheel notch when the box has focus.</summary>
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (!IsEnabled || !_entry.IsFocused) return;
        if (e.Delta.Y > 0)
        {
            Increment();
            e.Handled = true;
        }
        else if (e.Delta.Y < 0)
        {
            Decrement();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Increases the value by one step, clamped to the range.
    /// </summary>
    public void Increment() => Value = Math.Clamp(Math.Round((Value + Step) / Step) * Step, Minimum, Maximum);
    /// <summary>
    /// Decreases the value by one step, clamped to the range.
    /// </summary>
    public void Decrement() => Value = Math.Clamp(Math.Round((Value - Step) / Step) * Step, Minimum, Maximum);

    private void CommitEntryText()
    {
        if (double.TryParse(_entry.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            Value = Math.Clamp(parsed, Minimum, Maximum);
        }
        FormatEntry();
    }

    private void OnValuePropertyChanged(double val)
    {
        if (_updating) return;
        FormatEntry();
        SyncButtons();
        ValueChanged?.Invoke(this, val);
    }

    private void FormatEntry()
    {
        _updating = true;
        var fmt = !string.IsNullOrWhiteSpace(FormatString)
            ? FormatString!
            : (DecimalPlaces > 0 ? $"F{DecimalPlaces}" : "0");
        _entry.Text = Value.ToString(fmt, CultureInfo.InvariantCulture);
        _updating = false;
    }

    private void SyncButtons()
    {
        _decBtn.IsEnabled = IsEnabled && Value > Minimum + 1e-9;
        _incBtn.IsEnabled = IsEnabled && Value < Maximum - 1e-9;
    }

    private void ApplyTheme(MavTheme t)
    {
        var c = t.Color;
        _label.Apply(MavTypeRole.LabelMedium, t.Typography);
        _label.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);

        _suffixLbl.Apply(MavTypeRole.BodyMedium, t.Typography);
        _suffixLbl.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);

        _entry.FontFamily = t.Typography.PlainFont;
        _entry.FontSize = 16;
        _entry.TextColor = c.OnSurface;

        _box.Radius = t.Shape.Medium;
        _box.FillColor = c.SurfaceContainerLow;
        _box.BorderStrokeColor = c.OutlineVariant;
        SyncButtons();
    }
}
