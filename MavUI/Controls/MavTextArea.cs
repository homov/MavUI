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
/// Material Design 3 multi-line text area with Filled/Outlined variants, floating label,
/// supporting/error text, and optional character counter.
/// </summary>
public class MavTextArea : MavContentView
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MavTextArea, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavTextArea, string>(
            nameof(Text),
            string.Empty,
            defaultBindingMode: BindingMode.TwoWay,
            enableDataValidation: true);

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<MavTextArea, string?>(nameof(Placeholder));

    public static readonly StyledProperty<string?> SupportingTextProperty =
        AvaloniaProperty.Register<MavTextArea, string?>(nameof(SupportingText));

    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<MavTextArea, string?>(nameof(ErrorText));

    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<MavTextArea, bool>(nameof(HasError));

    public static readonly StyledProperty<MavTextFieldVariant> VariantProperty =
        AvaloniaProperty.Register<MavTextArea, MavTextFieldVariant>(nameof(Variant), MavTextFieldVariant.Outlined);

    public static readonly StyledProperty<int> MaxLengthProperty =
        AvaloniaProperty.Register<MavTextArea, int>(nameof(MaxLength), 0);

    public static readonly StyledProperty<bool> ShowCounterProperty =
        AvaloniaProperty.Register<MavTextArea, bool>(nameof(ShowCounter));

    public static readonly StyledProperty<double> MinBoxHeightProperty =
        AvaloniaProperty.Register<MavTextArea, double>(nameof(MinBoxHeight), 112.0);

    private readonly AreaChrome _chrome;
    private readonly MavFieldChromeCache _chromeCache = new();
    private readonly Grid _fieldBox;
    private readonly MavNativeEditor _editor;
    private readonly TextBlock _floatingLabel;
    private readonly Grid _footerRow;
    private readonly TextBlock _supportingLabel;
    private readonly TextBlock _counterLabel;

    private double _floatProgress;
    private bool _updatingText;
    private MavTheme? _appliedTheme;
    private bool _appliedError;
    private bool _appliedFocused;
    private bool _appliedFloated;

    public event EventHandler<string>? TextChanged;

    static MavTextArea()
    {
        LabelProperty.Changed.AddClassHandler<MavTextArea>((a, _) =>
        {
            a.SyncState(animate: false);
            a.SyncAutomation();
        });
        TextProperty.Changed.AddClassHandler<MavTextArea>((a, e) => a.OnOuterTextChanged(e.GetNewValue<string>()));
        PlaceholderProperty.Changed.AddClassHandler<MavTextArea>((a, _) => a.SyncState(animate: false));
        SupportingTextProperty.Changed.AddClassHandler<MavTextArea>((a, _) => a.SyncFooter());
        ErrorTextProperty.Changed.AddClassHandler<MavTextArea>((a, _) => a.SyncFooter());
        HasErrorProperty.Changed.AddClassHandler<MavTextArea>((a, _) => a.ApplyTheme(MavTheme.Current));
        VariantProperty.Changed.AddClassHandler<MavTextArea>((a, _) => a.SyncState(animate: false));
        MaxLengthProperty.Changed.AddClassHandler<MavTextArea>((a, e) =>
        {
            a._editor.MaxLength = Math.Max(0, e.GetNewValue<int>());
            a.SyncFooter();
        });
        ShowCounterProperty.Changed.AddClassHandler<MavTextArea>((a, _) => a.SyncFooter());
        MinBoxHeightProperty.Changed.AddClassHandler<MavTextArea>((a, e) => a._fieldBox.MinHeight = e.GetNewValue<double>());
    }

    /// <summary>Creates an empty multi-line text field.</summary>
    public MavTextArea() : this(string.Empty)
    {
    }

    /// <summary>Creates a multi-line text field with the given label, text and variant.</summary>
    public MavTextArea(
        string label = "",
        MavTextFieldVariant variant = MavTextFieldVariant.Outlined,
        string? placeholder = null,
        string? supportingText = null,
        double minHeight = 112)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _chrome = new AreaChrome(this);

        _editor = new MavNativeEditor
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

        Child = root;

        Variant = variant;
        Label = label;
        Placeholder = placeholder;
        SupportingText = supportingText;
        MinBoxHeight = minHeight;

        SyncState(animate: false);
        SyncAutomation();
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    private void SyncAutomation()
    {
        MavAutomation.SetName(this, Label);
        MavAutomation.SetHelpText(this,
            !string.IsNullOrEmpty(ErrorText) ? ErrorText : SupportingText);
        MavAutomation.SetControlType(this, AutomationControlType.Edit);
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

    public MavTextFieldVariant Variant
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

    public MavNativeEditor InnerEditor => _editor;

    /// <summary>Shows or clears the validation message when the bound data validation state changes.</summary>
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
        TextChanged?.Invoke(this, _editor.Text ?? string.Empty);
    }

    private void OnEditorTextChanged(string newText)
    {
        if (_updatingText) return;
        _updatingText = true;
        SetValue(TextProperty, newText);
        _updatingText = false;
        SyncState(animate: true);
        TextChanged?.Invoke(this, newText);
    }

    private void SyncState(bool animate)
    {
        var hasLabel = !string.IsNullOrEmpty(Label);
        var floated = _editor.IsFocused || !string.IsNullOrEmpty(Text);
        _floatingLabel.Text = Label;
        _floatingLabel.IsVisible = hasLabel;

        _editor.Margin = new Thickness(16, Variant == MavTextFieldVariant.Filled && hasLabel ? 26 : 16, 16, 12);
        _editor.Placeholder = (!hasLabel || floated) ? (Placeholder ?? string.Empty) : string.Empty;

        var target = floated ? 1.0 : 0.0;
        if (animate)
        {
            MavMotion.Animate(this, "ta_float", _floatProgress, target, MavMotion.Short3, MavMotion.CubicOut, v =>
            {
                _floatProgress = v;
                UpdateFloatingPosition();
                _chrome.InvalidateVisual();
            });
        }
        else
        {
            MavMotion.Cancel(this, "ta_float");
            _floatProgress = target;
            UpdateFloatingPosition();
            _chrome.InvalidateVisual();
        }
        SyncFooter(applyTheme: false);
        ApplyTheme(MavTheme.Current);
    }

    private void UpdateFloatingPosition()
    {
        var restY = 16.0;
        var floatY = Variant == MavTextFieldVariant.Outlined ? -8.0 : 7.0;
        var y = restY + (floatY - restY) * _floatProgress;
        _floatingLabel.FontSize = 16.0 - 4.0 * _floatProgress;
        _floatingLabel.Margin = new Thickness(16, y, 0, 0);
    }

    private void SyncFooter(bool applyTheme = true)
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
        if (applyTheme)
            ApplyTheme(MavTheme.Current);
    }

    private void ApplyTheme(MavTheme t)
    {
        var c = t.Color;
        var isErr = HasError;
        var isFocused = _editor.IsFocused;
        var isFloated = _floatProgress > 0.5;

        if (ReferenceEquals(_appliedTheme, t)
            && _appliedError == isErr
            && _appliedFocused == isFocused
            && _appliedFloated == isFloated)
        {
            return;
        }

        _appliedTheme = t;
        _appliedError = isErr;
        _appliedFocused = isFocused;
        _appliedFloated = isFloated;

        _editor.FontFamily = t.Typography.PlainFont;
        _editor.FontSize = 16;
        _editor.TextColor = c.OnSurface;
        _editor.PlaceholderColor = c.OnSurfaceVariant.WithAlpha(0.6f);

        _floatingLabel.FontFamily = t.Typography.PlainFont;
        _floatingLabel.FontWeight = _floatProgress > 0.5 ? FontWeight.Medium : FontWeight.Regular;
        _floatingLabel.Foreground = MavTheme.BrushOf(
            isErr ? c.Error : (isFocused ? c.Primary : c.OnSurfaceVariant));

        _supportingLabel.Apply(MavTypeRole.BodySmall, t.Typography);
        _supportingLabel.Foreground = MavTheme.BrushOf(isErr ? c.Error : c.OnSurfaceVariant);
        _counterLabel.Apply(MavTypeRole.BodySmall, t.Typography);
        _counterLabel.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);

        _chrome.InvalidateVisual();
    }

    private sealed class AreaChrome(MavTextArea owner) : Control
    {
        /// <summary>
        /// Draws the text area chrome and text.
        /// </summary>
        public override void Render(DrawingContext context) =>
            MavDrawingHelpers.DrawFieldChrome(
                context,
                Bounds.Width,
                Bounds.Height,
                owner.Variant == MavTextFieldVariant.Filled,
                owner.IsEnabled,
                owner.HasError,
                owner._editor.IsFocused,
                owner.Label,
                owner._floatProgress,
                owner._chromeCache);
    }
}
