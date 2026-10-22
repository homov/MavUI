using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 OTP / PIN segmented input with optional masking and separator dash.
/// </summary>
public class MavPinInput : MavContentView
{
    public static readonly StyledProperty<int> LengthProperty =
        AvaloniaProperty.Register<MavPinInput, int>(nameof(Length), 6);

    public static readonly StyledProperty<string> CodeProperty =
        AvaloniaProperty.Register<MavPinInput, string>(nameof(Code), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsMaskedProperty =
        AvaloniaProperty.Register<MavPinInput, bool>(nameof(IsMasked));

    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<MavPinInput, bool>(nameof(HasError));

    public static readonly StyledProperty<ICommand?> CompletedCommandProperty =
        AvaloniaProperty.Register<MavPinInput, ICommand?>(nameof(CompletedCommand));

    public static readonly StyledProperty<object?> CompletedCommandParameterProperty =
        AvaloniaProperty.Register<MavPinInput, object?>(nameof(CompletedCommandParameter));

    private readonly StackPanel _boxesRow;
    private readonly List<(MavSurface Box, TextBlock CharLabel)> _cells = [];
    private readonly MavNativeEntry _entry;
    private bool _updating;

    public event EventHandler<string>? CodeChanged;
    public event EventHandler<string>? Completed;

    static MavPinInput()
    {
        LengthProperty.Changed.AddClassHandler<MavPinInput>((p, _) => p.RebuildCells());
        CodeProperty.Changed.AddClassHandler<MavPinInput>((p, e) => p.OnOuterCodeChanged(e.GetNewValue<string>()));
        IsMaskedProperty.Changed.AddClassHandler<MavPinInput>((p, _) => p.UpdateVisuals(MavTheme.Current));
        HasErrorProperty.Changed.AddClassHandler<MavPinInput>((p, _) => p.UpdateVisuals(MavTheme.Current));
    }

    /// <summary>Creates a six-digit PIN input that shows the digits.</summary>
    public MavPinInput() : this(6, false)
    {
    }

    /// <summary>Creates a PIN input with <paramref name="length"/> cells, optionally masking the digits.</summary>
    public MavPinInput(int length = 6, bool masked = false)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;
        Cursor = new Cursor(StandardCursorType.Ibeam);

        _boxesRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
        };

        _entry = new MavNativeEntry
        {
            Width = 1,
            Height = 1,
            Opacity = 0.01,
            MaxLength = length,
        };
        TextInputOptions.SetContentType(_entry, TextInputContentType.Pin);
        TextInputOptions.SetReturnKeyType(_entry, TextInputReturnKeyType.Done);
        _entry.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
                OnEntryTextChanged(_entry.Text ?? string.Empty);
            else if (e.Property == IsFocusedProperty)
                UpdateVisuals(MavTheme.Current);
        };

        var root = new Grid { Background = Brushes.Transparent };
        root.Children.Add(_entry);
        // Shrink the row when the host is narrower than six boxes plus spacing (cards, phones).
        root.Children.Add(new Viewbox
        {
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.DownOnly,
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = _boxesRow,
        });
        root.PointerPressed += (_, e) =>
        {
            e.Handled = true;
            _entry.Focus();
            _entry.CursorPosition = _entry.Text?.Length ?? 0;
        };

        Content = root;

        Length = Math.Clamp(length, 3, 8);
        IsMasked = masked;
        RebuildCells();
        MavAutomation.SetControlType(this, AutomationControlType.Edit);

        MavTheme.Observe(this, (self, theme) => self.UpdateVisuals(theme));
    }

    public int Length
    {
        get => GetValue(LengthProperty);
        set => SetValue(LengthProperty, Math.Clamp(value, 3, 8));
    }

    public string Code
    {
        get => GetValue(CodeProperty);
        set => SetValue(CodeProperty, value ?? string.Empty);
    }

    public string Value
    {
        get => Code;
        set => Code = value;
    }

    public string Text
    {
        get => Code;
        set => Code = value;
    }

    public bool IsMasked
    {
        get => GetValue(IsMaskedProperty);
        set => SetValue(IsMaskedProperty, value);
    }

    public bool HasError
    {
        get => GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }

    public ICommand? CompletedCommand
    {
        get => GetValue(CompletedCommandProperty);
        set => SetValue(CompletedCommandProperty, value);
    }

    public object? CompletedCommandParameter
    {
        get => GetValue(CompletedCommandParameterProperty);
        set => SetValue(CompletedCommandParameterProperty, value);
    }

    /// <summary>
    /// Clears the entered code.
    /// </summary>
    public void Clear() => Code = string.Empty;

    private void RebuildCells()
    {
        _boxesRow.Children.Clear();
        _cells.Clear();
        _entry.MaxLength = Length;

        for (int i = 0; i < Length; i++)
        {
            if (Length == 6 && i == 3)
            {
                var sep = new TextBlock
                {
                    Text = "\u2013",
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2, 0),
                };
                _boxesRow.Children.Add(sep);
            }

            var lbl = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };

            var box = new MavSurface
            {
                Width = 44,
                Height = 52,
                Radius = 10,
                BorderStrokeThickness = 1,
                Content = lbl,
            };

            _cells.Add((box, lbl));
            _boxesRow.Children.Add(box);
        }

        UpdateVisuals(MavTheme.Current);
    }

    private void OnOuterCodeChanged(string? newCode)
    {
        if (_updating) return;
        var clean = Sanitize(newCode);
        _updating = true;
        _entry.Text = clean;
        SetValue(CodeProperty, clean);
        _updating = false;
        UpdateVisuals(MavTheme.Current);
        CodeChanged?.Invoke(this, clean);
        if (clean.Length == Length)
        {
            Completed?.Invoke(this, clean);
            var param = CompletedCommandParameter ?? clean;
            if (CompletedCommand is { } cCmd && cCmd.CanExecute(param))
                cCmd.Execute(param);
        }
    }

    private void OnEntryTextChanged(string raw)
    {
        if (_updating) return;
        var clean = Sanitize(raw);
        _updating = true;
        if (_entry.Text != clean)
            _entry.Text = clean;
        SetValue(CodeProperty, clean);
        _updating = false;

        UpdateVisuals(MavTheme.Current);
        CodeChanged?.Invoke(this, clean);
        if (clean.Length == Length)
        {
            Completed?.Invoke(this, clean);
            var param = CompletedCommandParameter ?? clean;
            if (CompletedCommand is { } cCmd && cCmd.CanExecute(param))
                cCmd.Execute(param);
        }
    }

    private string Sanitize(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var chars = s.Where(char.IsLetterOrDigit).Take(Length).ToArray();
        return new string(chars).ToUpperInvariant();
    }

    private void UpdateVisuals(MavTheme t)
    {
        var c = t.Color;
        var code = Code ?? string.Empty;
        var focused = _entry.IsFocused;
        var activeIdx = Math.Min(code.Length, Length - 1);

        for (int i = 0; i < _cells.Count; i++)
        {
            var (box, lbl) = _cells[i];
            var hasChar = i < code.Length;
            lbl.Text = hasChar ? (IsMasked ? "\u2022" : code[i].ToString()) : string.Empty;
            lbl.Apply(MavTypeRole.HeadlineSmall, t.Typography);
            lbl.Foreground = MavTheme.BrushOf(HasError ? c.Error : c.OnSurface);

            var isCurrent = focused && i == activeIdx;
            box.FillColor = hasChar ? c.SurfaceContainerLow : c.Surface;
            box.BorderStrokeColor = HasError
                ? c.Error
                : isCurrent
                    ? c.Primary
                    : hasChar
                        ? c.Outline
                        : c.OutlineVariant;
            box.BorderStrokeThickness = isCurrent ? 2 : 1;
        }
    }
}
