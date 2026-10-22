using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Layout;
using Avalonia.Media;
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Material Design 3 multi-line text area with Filled/Outlined variants, floating label,
/// supporting/error text, and optional character counter.
/// </summary>
public class MdTextArea : MdContentView
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MdTextArea, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MdTextArea, string>(
            nameof(Text),
            string.Empty,
            defaultBindingMode: BindingMode.TwoWay,
            enableDataValidation: true);

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<MdTextArea, string?>(nameof(Placeholder));

    public static readonly StyledProperty<string?> SupportingTextProperty =
        AvaloniaProperty.Register<MdTextArea, string?>(nameof(SupportingText));

    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<MdTextArea, string?>(nameof(ErrorText));

    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<MdTextArea, bool>(nameof(HasError));

    public static readonly StyledProperty<MdTextFieldVariant> VariantProperty =
        AvaloniaProperty.Register<MdTextArea, MdTextFieldVariant>(nameof(Variant), MdTextFieldVariant.Outlined);

    public static readonly StyledProperty<int> MaxLengthProperty =
        AvaloniaProperty.Register<MdTextArea, int>(nameof(MaxLength), 0);

    public static readonly StyledProperty<bool> ShowCounterProperty =
        AvaloniaProperty.Register<MdTextArea, bool>(nameof(ShowCounter));

    public static readonly StyledProperty<double> MinBoxHeightProperty =
        AvaloniaProperty.Register<MdTextArea, double>(nameof(MinBoxHeight), 112.0);

    private readonly AreaChrome _chrome;
    private readonly Grid _fieldBox;
    private readonly MdNativeEditor _editor;
    private readonly TextBlock _floatingLabel;
    private readonly Grid _footerRow;
    private readonly TextBlock _supportingLabel;
    private readonly TextBlock _counterLabel;

    private double _floatProgress;
    private bool _updatingText;

    public event EventHandler<string>? TextChanged;

    static MdTextArea()
    {
        LabelProperty.Changed.AddClassHandler<MdTextArea>((a, _) =>
        {
            a.SyncState(animate: false);
            a.SyncAutomation();
        });
        TextProperty.Changed.AddClassHandler<MdTextArea>((a, e) => a.OnOuterTextChanged(e.GetNewValue<string>()));
        PlaceholderProperty.Changed.AddClassHandler<MdTextArea>((a, _) => a.SyncState(animate: false));
        SupportingTextProperty.Changed.AddClassHandler<MdTextArea>((a, _) => a.SyncFooter());
        ErrorTextProperty.Changed.AddClassHandler<MdTextArea>((a, _) => a.SyncFooter());
        HasErrorProperty.Changed.AddClassHandler<MdTextArea>((a, _) => a.ApplyTheme(MdTheme.Current));
        VariantProperty.Changed.AddClassHandler<MdTextArea>((a, _) => a.SyncState(animate: false));
        MaxLengthProperty.Changed.AddClassHandler<MdTextArea>((a, e) =>
        {
            a._editor.MaxLength = Math.Max(0, e.GetNewValue<int>());
            a.SyncFooter();
        });
        ShowCounterProperty.Changed.AddClassHandler<MdTextArea>((a, _) => a.SyncFooter());
        MinBoxHeightProperty.Changed.AddClassHandler<MdTextArea>((a, e) => a._fieldBox.MinHeight = e.GetNewValue<double>());
    }

    public MdTextArea() : this(string.Empty)
    {
    }

    public MdTextArea(
        string label = "",
        MdTextFieldVariant variant = MdTextFieldVariant.Outlined,
        string? placeholder = null,
        string? supportingText = null,
        double minHeight = 112)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _chrome = new AreaChrome(this);

        _editor = new MdNativeEditor
        {
            Margin = new Thickness(16, 20, 16, 12),
        };
        TextInputOptions.SetMultiline(_editor, true);
        TextInputOptions.SetReturnKeyType(_editor, TextInputReturnKeyType.Return);
        _editor.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
                OnEditorTextChanged(_editor.Text ?? string.Empty);
            else if (e.Property == IsFocusedProperty)
            {
                SyncState(animate: true);
                ApplyTheme(MdTheme.Current);
            }
        };

        _floatingLabel = new TextBlock
        {
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            TextWrapping = TextWrapping.NoWrap,
        };

        _fieldBox = new Grid
        {
            MinHeight = minHeight,
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Ibeam),
        };
        _fieldBox.Children.Add(_chrome);
        _fieldBox.Children.Add(_editor);
        _fieldBox.Children.Add(_floatingLabel);
        _fieldBox.PointerPressed += (_, _) =>
        {
            if (IsEnabled && !_editor.IsFocused)
                _editor.Focus();
        };
        _fieldBox.ContextRequested += (_, e) =>
        {
            if (!e.Handled && IsEnabled)
            {
                if (!_editor.IsFocused)
                    _editor.Focus();
                _editor.RaiseEvent(e);
            }
        };

        _supportingLabel = new TextBlock { TextWrapping = TextWrapping.Wrap };
        _counterLabel = new TextBlock { HorizontalAlignment = HorizontalAlignment.Right };

        _footerRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(16, 4, 16, 0),
            IsVisible = false,
        };
        _footerRow.Add(_supportingLabel, 0, 0);
        _footerRow.Add(_counterLabel, 1, 0);

        var root = new StackPanel { Orientation = Orientation.Vertical };
        root.Children.Add(_fieldBox);
        root.Children.Add(_footerRow);

        Content = root;

        Variant = variant;
        Label = label;
        Placeholder = placeholder;
        SupportingText = supportingText;
        MinBoxHeight = minHeight;

        SyncState(animate: false);
        SyncAutomation();
        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    private void SyncAutomation()
    {
        MdAutomation.SetName(this, Label);
        MdAutomation.SetHelpText(this,
            !string.IsNullOrEmpty(ErrorText) ? ErrorText : SupportingText);
        MdAutomation.SetControlType(this, AutomationControlType.Edit);
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value ?? string.Empty);
    }

    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
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

    public bool HasError
    {
        get => GetValue(HasErrorProperty) || !string.IsNullOrEmpty(ErrorText);
        set => SetValue(HasErrorProperty, value);
    }

    public MdTextFieldVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public int MaxLength
    {
        get => GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    public bool ShowCounter
    {
        get => GetValue(ShowCounterProperty);
        set => SetValue(ShowCounterProperty, value);
    }

    public double MinBoxHeight
    {
        get => GetValue(MinBoxHeightProperty);
        set => SetValue(MinBoxHeightProperty, value);
    }

    public MdNativeEditor InnerEditor => _editor;

    protected override void UpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
    {
        base.UpdateDataValidation(property, state, error);
        if (property == TextProperty)
        {
            DataValidationErrors.SetError(this, error);
            ErrorText = error?.Message;
        }
    }

    private void OnOuterTextChanged(string? newText)
    {
        if (_updatingText) return;
        _updatingText = true;
        _editor.Text = newText ?? string.Empty;
        _updatingText = false;
        SyncState(animate: true);
        SyncFooter();
        TextChanged?.Invoke(this, _editor.Text ?? string.Empty);
    }

    private void OnEditorTextChanged(string newText)
    {
        if (_updatingText) return;
        _updatingText = true;
        SetValue(TextProperty, newText);
        _updatingText = false;
        SyncState(animate: true);
        SyncFooter();
        TextChanged?.Invoke(this, newText);
    }

    private void SyncState(bool animate)
    {
        var hasLabel = !string.IsNullOrEmpty(Label);
        var floated = _editor.IsFocused || !string.IsNullOrEmpty(Text);
        _floatingLabel.Text = Label;
        _floatingLabel.IsVisible = hasLabel;

        _editor.Margin = new Thickness(16, Variant == MdTextFieldVariant.Filled && hasLabel ? 26 : 16, 16, 12);
        _editor.Placeholder = (!hasLabel || floated) ? (Placeholder ?? string.Empty) : string.Empty;

        var target = floated ? 1.0 : 0.0;
        if (animate)
        {
            MdMotion.Animate(this, "ta_float", _floatProgress, target, MdMotion.Short3, MdMotion.CubicOut, v =>
            {
                _floatProgress = v;
                UpdateFloatingPosition();
                _chrome.InvalidateVisual();
            });
        }
        else
        {
            MdMotion.Cancel(this, "ta_float");
            _floatProgress = target;
            UpdateFloatingPosition();
            _chrome.InvalidateVisual();
        }
        SyncFooter();
    }

    private void UpdateFloatingPosition()
    {
        var restY = 16.0;
        var floatY = Variant == MdTextFieldVariant.Outlined ? -8.0 : 7.0;
        var y = restY + (floatY - restY) * _floatProgress;
        _floatingLabel.FontSize = 16.0 - 4.0 * _floatProgress;
        _floatingLabel.Margin = new Thickness(16, y, 0, 0);
    }

    private void SyncFooter()
    {
        var err = ErrorText;
        var sup = !string.IsNullOrEmpty(err) ? err : SupportingText;
        var hasSup = !string.IsNullOrEmpty(sup);
        _supportingLabel.Text = sup ?? string.Empty;
        _supportingLabel.IsVisible = hasSup;

        var hasCounter = ShowCounter || MaxLength > 0;
        if (hasCounter)
        {
            var len = Text?.Length ?? 0;
            _counterLabel.Text = MaxLength > 0 ? $"{len}/{MaxLength}" : $"{len}";
        }
        _counterLabel.IsVisible = hasCounter;
        _footerRow.IsVisible = hasSup || hasCounter;
        ApplyTheme(MdTheme.Current);
    }

    private void ApplyTheme(MdTheme t)
    {
        var c = t.Color;
        var isErr = HasError;
        var isFocused = _editor.IsFocused;

        _editor.FontFamily = t.Typography.PlainFont;
        _editor.FontSize = 16;
        _editor.TextColor = c.OnSurface;
        _editor.PlaceholderColor = c.OnSurfaceVariant.WithAlpha(0.6f);

        _floatingLabel.FontFamily = t.Typography.PlainFont;
        _floatingLabel.FontWeight = _floatProgress > 0.5 ? FontWeight.Medium : FontWeight.Regular;
        _floatingLabel.Foreground = MdTheme.BrushOf(
            isErr ? c.Error : (isFocused ? c.Primary : c.OnSurfaceVariant));

        _supportingLabel.Apply(MdTypeRole.BodySmall, t.Typography);
        _supportingLabel.Foreground = MdTheme.BrushOf(isErr ? c.Error : c.OnSurfaceVariant);
        _counterLabel.Apply(MdTypeRole.BodySmall, t.Typography);
        _counterLabel.Foreground = MdTheme.BrushOf(c.OnSurfaceVariant);

        _chrome.InvalidateVisual();
    }

    private sealed class AreaChrome(MdTextArea owner) : Control
    {
        public override void Render(DrawingContext context) =>
            MdDrawingHelpers.DrawFieldChrome(
                context,
                Bounds.Width,
                Bounds.Height,
                owner.Variant == MdTextFieldVariant.Filled,
                owner.IsEnabled,
                owner.HasError,
                owner._editor.IsFocused,
                owner.Label,
                owner._floatProgress);
    }
}
