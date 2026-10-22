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

/// <summary>Visual style of a <see cref="MavTextField"/>: filled or outlined.</summary>
public enum MavTextFieldVariant
{
    Filled,
    Outlined,
}

/// <summary>
/// Material Design 3 single-line text field with Filled and Outlined variants, animated floating label,
/// outlined notch cutout, prefix/suffix text, password toggle, clear button, error state, and character counter.
/// </summary>
public class MavTextField : MavContentView
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MavTextField, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavTextField, string>(
            nameof(Text),
            string.Empty,
            defaultBindingMode: BindingMode.TwoWay,
            enableDataValidation: true);

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<MavTextField, string?>(nameof(Placeholder));

    public static readonly StyledProperty<string?> SupportingTextProperty =
        AvaloniaProperty.Register<MavTextField, string?>(nameof(SupportingText));

    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<MavTextField, string?>(nameof(ErrorText));

    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<MavTextField, bool>(nameof(HasError));

    public static readonly StyledProperty<string?> LeadingIconProperty =
        AvaloniaProperty.Register<MavTextField, string?>(nameof(LeadingIcon));

    public static readonly StyledProperty<string?> TrailingIconProperty =
        AvaloniaProperty.Register<MavTextField, string?>(nameof(TrailingIcon));

    public static readonly StyledProperty<string?> PrefixTextProperty =
        AvaloniaProperty.Register<MavTextField, string?>(nameof(PrefixText));

    public static readonly StyledProperty<string?> SuffixTextProperty =
        AvaloniaProperty.Register<MavTextField, string?>(nameof(SuffixText));

    public static readonly StyledProperty<MavTextFieldVariant> VariantProperty =
        AvaloniaProperty.Register<MavTextField, MavTextFieldVariant>(nameof(Variant), MavTextFieldVariant.Outlined);

    public static readonly StyledProperty<bool> IsPasswordProperty =
        AvaloniaProperty.Register<MavTextField, bool>(nameof(IsPassword));

    /// <summary>
    /// Soft-keyboard content type (email, phone, number, url, …).
    /// </summary>
    public static readonly StyledProperty<TextInputContentType> ContentTypeProperty =
        AvaloniaProperty.Register<MavTextField, TextInputContentType>(nameof(ContentType));

    /// <summary>
    /// Soft-keyboard return/enter key label.
    /// </summary>
    public static readonly StyledProperty<TextInputReturnKeyType> ReturnKeyTypeProperty =
        AvaloniaProperty.Register<MavTextField, TextInputReturnKeyType>(nameof(ReturnKeyType));

    /// <summary>
    /// Requests automatic capitalization of the first character on touch keyboards.
    /// </summary>
    public static readonly StyledProperty<bool> AutoCapitalizeProperty =
        AvaloniaProperty.Register<MavTextField, bool>(nameof(AutoCapitalize));

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<MavTextField, bool>(nameof(IsReadOnly));

    public static readonly StyledProperty<bool> ShowClearButtonProperty =
        AvaloniaProperty.Register<MavTextField, bool>(nameof(ShowClearButton));

    public static readonly StyledProperty<int> MaxLengthProperty =
        AvaloniaProperty.Register<MavTextField, int>(nameof(MaxLength), 0);

    public static readonly StyledProperty<bool> ShowCounterProperty =
        AvaloniaProperty.Register<MavTextField, bool>(nameof(ShowCounter));

    public static readonly StyledProperty<ICommand?> CompletedCommandProperty =
        AvaloniaProperty.Register<MavTextField, ICommand?>(nameof(CompletedCommand));

    public static readonly StyledProperty<object?> CompletedCommandParameterProperty =
        AvaloniaProperty.Register<MavTextField, object?>(nameof(CompletedCommandParameter));

    public static readonly StyledProperty<ICommand?> TrailingIconCommandProperty =
        AvaloniaProperty.Register<MavTextField, ICommand?>(nameof(TrailingIconCommand));

    public static readonly StyledProperty<object?> TrailingIconCommandParameterProperty =
        AvaloniaProperty.Register<MavTextField, object?>(nameof(TrailingIconCommandParameter));

    private readonly FieldChrome _chrome;
    private readonly MavFieldChromeCache _chromeCache = new();
    private readonly Grid _fieldBox;
    private readonly MavIcon _leadIcon;
    private readonly TextBlock _prefixLabel;
    private readonly MavNativeEntry _entry;
    private readonly TextBlock _suffixLabel;
    private readonly Border _trailingHit;
    private readonly MavIcon _trailIcon;
    private readonly TextBlock _floatingLabel;
    private readonly Grid _footerRow;
    private readonly TextBlock _supportingLabel;
    private readonly TextBlock _counterLabel;

    private double _floatProgress;
    private bool _passwordRevealed;
    private bool _updatingText;
    private MavTheme? _appliedTheme;
    private bool _appliedEnabled;
    private bool _appliedError;
    private bool _appliedFocused;
    private bool _appliedFloated;

    public event EventHandler<string>? TextChanged;
    public event EventHandler? Completed;
    public event EventHandler? TrailingIconClicked;

    static MavTextField()
    {
        LabelProperty.Changed.AddClassHandler<MavTextField>((f, _) =>
        {
            f.SyncState(animate: false);
            f.SyncAutomation();
        });
        TextProperty.Changed.AddClassHandler<MavTextField>((f, e) => f.OnOuterTextChanged(e.GetNewValue<string>()));
        PlaceholderProperty.Changed.AddClassHandler<MavTextField>((f, _) => f.SyncState(animate: false));
        SupportingTextProperty.Changed.AddClassHandler<MavTextField>((f, _) =>
        {
            f.SyncFooter();
            f.SyncAutomation();
        });
        ErrorTextProperty.Changed.AddClassHandler<MavTextField>((f, _) =>
        {
            f.SyncFooter();
            f.SyncAutomation();
        });
        HasErrorProperty.Changed.AddClassHandler<MavTextField>((f, _) => f.ApplyTheme(MavTheme.Current));
        LeadingIconProperty.Changed.AddClassHandler<MavTextField>((f, _) => f.SyncState(animate: false));
        TrailingIconProperty.Changed.AddClassHandler<MavTextField>((f, _) => f.SyncState(animate: false));
        PrefixTextProperty.Changed.AddClassHandler<MavTextField>((f, _) => f.SyncState(animate: false));
        SuffixTextProperty.Changed.AddClassHandler<MavTextField>((f, _) => f.SyncState(animate: false));
        VariantProperty.Changed.AddClassHandler<MavTextField>((f, _) => f.SyncState(animate: false));
        IsPasswordProperty.Changed.AddClassHandler<MavTextField>((f, _) => f.SyncPassword());
        ContentTypeProperty.Changed.AddClassHandler<MavTextField>((f, _) => f.SyncTextInputOptions());
        ReturnKeyTypeProperty.Changed.AddClassHandler<MavTextField>((f, _) => f.SyncTextInputOptions());
        AutoCapitalizeProperty.Changed.AddClassHandler<MavTextField>((f, _) => f.SyncTextInputOptions());
        IsReadOnlyProperty.Changed.AddClassHandler<MavTextField>((f, e) => f._entry.IsReadOnly = e.GetNewValue<bool>());
        ShowClearButtonProperty.Changed.AddClassHandler<MavTextField>((f, _) => f.SyncTrailing());
        MaxLengthProperty.Changed.AddClassHandler<MavTextField>((f, e) =>
        {
            f._entry.MaxLength = Math.Max(0, e.GetNewValue<int>());
            f.SyncFooter();
        });
        ShowCounterProperty.Changed.AddClassHandler<MavTextField>((f, _) => f.SyncFooter());
    }

    /// <summary>Creates an empty single-line text field.</summary>
    public MavTextField() : this(string.Empty)
    {
    }

    /// <summary>Creates a text field with the given label, text, variant and icons.</summary>
    public MavTextField(
        string label = "",
        MavTextFieldVariant variant = MavTextFieldVariant.Outlined,
        string? placeholder = null,
        string? leadingIcon = null,
        string? supportingText = null)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Center;

        _chrome = new FieldChrome(this);

        _leadIcon = new MavIcon
        {
            Size = 22,
            UseThemeDefaultColor = false,
            IsVisible = false,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
        };

        _prefixLabel = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0),
            IsVisible = false,
        };

        _entry = new MavNativeEntry
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        _entry.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
                OnEntryTextChanged(_entry.Text ?? string.Empty);
            else if (e.Property == IsFocusedProperty)
                OnEntryFocusChanged(_entry.IsFocused);
        };
        _entry.Completed += (_, _) =>
        {
            Completed?.Invoke(this, EventArgs.Empty);
            var param = CompletedCommandParameter ?? Text;
            if (CompletedCommand is { } cCmd && cCmd.CanExecute(param))
                cCmd.Execute(param);
        };

        _suffixLabel = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0),
            IsVisible = false,
        };

        _trailIcon = new MavIcon { Size = 22, UseThemeDefaultColor = false };
        _trailingHit = new Border
        {
            Width = 36,
            Height = 36,
            Background = Brushes.Transparent,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = new Cursor(StandardCursorType.Hand),
            IsVisible = false,
            Child = _trailIcon,
        };
        _trailingHit.PointerPressed += (_, e) => e.Handled = true;
        _trailingHit.PointerReleased += (_, e) =>
        {
            e.Handled = true;
            OnTrailingTapped();
        };

        var inputRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto,Auto"),
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        inputRow.Add(_leadIcon, 0, 0);
        inputRow.Add(_prefixLabel, 1, 0);
        inputRow.Add(_entry, 2, 0);
        inputRow.Add(_suffixLabel, 3, 0);
        inputRow.Add(_trailingHit, 4, 0);

        _floatingLabel = new TextBlock
        {
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            TextWrapping = TextWrapping.NoWrap,
        };

        _fieldBox = new Grid
        {
            MinHeight = 56,
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Ibeam),
        };
        _fieldBox.Children.Add(_chrome);
        _fieldBox.Children.Add(inputRow);
        _fieldBox.Children.Add(_floatingLabel);
        _fieldBox.PointerPressed += (_, _) =>
        {
            if (IsEnabled && !_entry.IsFocused)
                _entry.Focus();
        };
        _fieldBox.ContextRequested += (_, e) =>
        {
            if (!e.Handled && IsEnabled)
            {
                if (!_entry.IsFocused)
                    _entry.Focus();
                _entry.RaiseEvent(e);
            }
        };

        _supportingLabel = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
        };
        _counterLabel = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Right,
        };

        _footerRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(16, 4, 16, 0),
            IsVisible = false,
        };
        _footerRow.Add(_supportingLabel, 0, 0);
        _footerRow.Add(_counterLabel, 1, 0);

        var root = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0,
        };
        root.Children.Add(_fieldBox);
        root.Children.Add(_footerRow);

        Content = root;

        Variant = variant;
        Label = label;
        Placeholder = placeholder;
        LeadingIcon = leadingIcon;
        SupportingText = supportingText;

        SyncState(animate: false);
        SyncTextInputOptions();
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

    public string? LeadingIcon
    {
        get => GetValue(LeadingIconProperty);
        set => SetValue(LeadingIconProperty, value);
    }

    public string? TrailingIcon
    {
        get => GetValue(TrailingIconProperty);
        set => SetValue(TrailingIconProperty, value);
    }

    public string? PrefixText
    {
        get => GetValue(PrefixTextProperty);
        set => SetValue(PrefixTextProperty, value);
    }

    public string? SuffixText
    {
        get => GetValue(SuffixTextProperty);
        set => SetValue(SuffixTextProperty, value);
    }

    public MavTextFieldVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public bool IsPassword
    {
        get => GetValue(IsPasswordProperty);
        set => SetValue(IsPasswordProperty, value);
    }

    public TextInputContentType ContentType
    {
        get => GetValue(ContentTypeProperty);
        set => SetValue(ContentTypeProperty, value);
    }

    public TextInputReturnKeyType ReturnKeyType
    {
        get => GetValue(ReturnKeyTypeProperty);
        set => SetValue(ReturnKeyTypeProperty, value);
    }

    public bool AutoCapitalize
    {
        get => GetValue(AutoCapitalizeProperty);
        set => SetValue(AutoCapitalizeProperty, value);
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public bool ShowClearButton
    {
        get => GetValue(ShowClearButtonProperty);
        set => SetValue(ShowClearButtonProperty, value);
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

    public bool ShowCharacterCounter
    {
        get => ShowCounter;
        set => ShowCounter = value;
    }

    public bool IsMultiline
    {
        get => _entry.AcceptsReturn;
        set
        {
            _entry.AcceptsReturn = value;
            _entry.TextWrapping = value ? TextWrapping.Wrap : TextWrapping.NoWrap;
            _fieldBox.MinHeight = value ? 88 : 56;
        }
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

    public ICommand? TrailingIconCommand
    {
        get => GetValue(TrailingIconCommandProperty);
        set => SetValue(TrailingIconCommandProperty, value);
    }

    public object? TrailingIconCommandParameter
    {
        get => GetValue(TrailingIconCommandParameterProperty);
        set => SetValue(TrailingIconCommandParameterProperty, value);
    }

    public MavNativeEntry InnerEntry => _entry;

    public void Focus() => _entry.Focus();
    public void Unfocus() => _entry.Unfocus();

    /// <summary>Updates the field when a visual property changes.</summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEnabledProperty)
        {
            _entry.IsEnabled = IsEnabled;
            ApplyTheme(MavTheme.Current);
        }
    }

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
        _entry.Text = newText ?? string.Empty;
        _updatingText = false;
        SyncState(animate: true);
        TextChanged?.Invoke(this, _entry.Text ?? string.Empty);
    }

    private void OnEntryTextChanged(string newText)
    {
        if (_updatingText) return;
        _updatingText = true;
        SetValue(TextProperty, newText);
        _updatingText = false;
        SyncState(animate: true);
        TextChanged?.Invoke(this, newText);
    }

    private void OnEntryFocusChanged(bool focused) => SyncState(animate: true);

    private void SyncPassword()
    {
        _entry.IsPassword = IsPassword && !_passwordRevealed;
        SyncTextInputOptions();
        SyncTrailing();
    }

    private void SyncTextInputOptions()
    {
        var contentType = IsPassword && !_passwordRevealed
            ? TextInputContentType.Password
            : ContentType;

        TextInputOptions.SetContentType(_entry, contentType);
        TextInputOptions.SetReturnKeyType(_entry, ReturnKeyType);
        TextInputOptions.SetAutoCapitalization(_entry, AutoCapitalize && !IsPassword);
        TextInputOptions.SetIsSensitive(_entry, IsPassword);
        TextInputOptions.SetMultiline(_entry, false);
    }

    private void SyncTrailing(bool applyTheme = true)
    {
        if (HasError && !IsPassword && string.IsNullOrEmpty(TrailingIcon))
        {
            _trailIcon.Glyph = MavIcons.Error;
            _trailingHit.IsVisible = true;
        }
        else if (IsPassword)
        {
            _trailIcon.Glyph = _passwordRevealed ? MavIcons.VisibilityOff : MavIcons.Visibility;
            _trailingHit.IsVisible = true;
        }
        else if (ShowClearButton && !string.IsNullOrEmpty(Text))
        {
            _trailIcon.Glyph = MavIcons.Cancel;
            _trailingHit.IsVisible = true;
        }
        else if (!string.IsNullOrEmpty(TrailingIcon))
        {
            _trailIcon.Glyph = TrailingIcon;
            _trailingHit.IsVisible = true;
        }
        else
        {
            _trailingHit.IsVisible = false;
        }

        if (applyTheme)
            ApplyTheme(MavTheme.Current);
    }

    private void OnTrailingTapped()
    {
        if (IsPassword)
        {
            _passwordRevealed = !_passwordRevealed;
            SyncPassword();
            return;
        }

        if (ShowClearButton && !string.IsNullOrEmpty(Text))
        {
            Text = string.Empty;
            _entry.Focus();
            return;
        }

        TrailingIconClicked?.Invoke(this, EventArgs.Empty);
        if (TrailingIconCommand is { } tCmd && tCmd.CanExecute(TrailingIconCommandParameter))
            tCmd.Execute(TrailingIconCommandParameter);
    }

    private void SyncState(bool animate)
    {
        var hasLead = !string.IsNullOrEmpty(LeadingIcon);
        _leadIcon.Glyph = LeadingIcon;
        _leadIcon.IsVisible = hasLead;

        var floated = _entry.IsFocused || !string.IsNullOrEmpty(Text) || !string.IsNullOrEmpty(PrefixText);
        var showPrefix = !string.IsNullOrEmpty(PrefixText) && floated;
        _prefixLabel.Text = PrefixText ?? string.Empty;
        _prefixLabel.IsVisible = showPrefix;
        _prefixLabel.Margin = new Thickness(hasLead ? 12 : 16, Variant == MavTextFieldVariant.Filled ? 14 : 0, 4, 0);

        var showSuffix = !string.IsNullOrEmpty(SuffixText) && floated;
        _suffixLabel.Text = SuffixText ?? string.Empty;
        _suffixLabel.IsVisible = showSuffix;
        _suffixLabel.Margin = new Thickness(4, Variant == MavTextFieldVariant.Filled ? 14 : 0, _trailingHit.IsVisible ? 4 : 16, 0);

        var leftMargin = showPrefix ? 0 : (hasLead ? 12 : 16);
        var rightMargin = showSuffix ? 0 : (_trailingHit.IsVisible ? 4 : 16);
        var hasLabel = !string.IsNullOrEmpty(Label);
        var topMargin = Variant == MavTextFieldVariant.Filled && hasLabel ? 16 : 0;
        _entry.Margin = new Thickness(leftMargin, topMargin, rightMargin, 0);

        _entry.Placeholder = (!hasLabel || floated) ? (Placeholder ?? string.Empty) : string.Empty;

        _floatingLabel.Text = Label;
        _floatingLabel.IsVisible = hasLabel;

        var target = floated ? 1.0 : 0.0;
        if (animate && Math.Abs(_floatProgress - target) > 0.001)
        {
            MavMotion.Animate(this, "tf_float", _floatProgress, target, MavMotion.Short3, MavMotion.CubicOut, v =>
            {
                _floatProgress = v;
                UpdateFloatingPosition();
                _chrome.InvalidateVisual();
            });
        }
        else
        {
            MavMotion.Cancel(this, "tf_float");
            _floatProgress = target;
            UpdateFloatingPosition();
            _chrome.InvalidateVisual();
        }

        SyncTrailing(applyTheme: false);
        SyncFooter(applyTheme: false);
        ApplyTheme(MavTheme.Current);
    }

    private void UpdateFloatingPosition()
    {
        var hasLead = !string.IsNullOrEmpty(LeadingIcon);
        var restX = hasLead ? 46.0 : 16.0;
        var floatX = Variant == MavTextFieldVariant.Outlined ? 16.0 : restX;
        var x = restX + (floatX - restX) * _floatProgress;

        var restY = 18.0;
        var floatY = Variant == MavTextFieldVariant.Outlined ? -8.0 : 7.0;
        var y = restY + (floatY - restY) * _floatProgress;

        var size = 16.0 - 4.0 * _floatProgress;
        _floatingLabel.FontSize = size;
        _floatingLabel.Margin = new Thickness(x, y, 0, 0);
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
        var isFocused = _entry.IsFocused;
        var isFloated = _floatProgress > 0.5;

        if (ReferenceEquals(_appliedTheme, t)
            && _appliedEnabled == IsEnabled
            && _appliedError == isErr
            && _appliedFocused == isFocused
            && _appliedFloated == isFloated)
        {
            return;
        }

        _appliedTheme = t;
        _appliedEnabled = IsEnabled;
        _appliedError = isErr;
        _appliedFocused = isFocused;
        _appliedFloated = isFloated;

        _entry.FontFamily = t.Typography.PlainFont;
        _entry.FontSize = 16;
        _entry.TextColor = IsEnabled ? c.OnSurface : c.OnSurface.WithAlpha(0.38f);
        _entry.PlaceholderColor = c.OnSurfaceVariant.WithAlpha(0.6f);

        _floatingLabel.FontFamily = t.Typography.PlainFont;
        _floatingLabel.FontWeight = _floatProgress > 0.5 ? FontWeight.Medium : FontWeight.Regular;
        var labelColor = !IsEnabled
            ? c.OnSurface.WithAlpha(0.38f)
            : isErr
                ? c.Error
                : isFocused
                    ? c.Primary
                    : c.OnSurfaceVariant;
        _floatingLabel.Foreground = MavTheme.BrushOf(labelColor);

        _prefixLabel.Apply(MavTypeRole.BodyLarge, t.Typography);
        _prefixLabel.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);
        _suffixLabel.Apply(MavTypeRole.BodyLarge, t.Typography);
        _suffixLabel.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);

        _leadIcon.Color = !IsEnabled ? c.OnSurface.WithAlpha(0.38f) : c.OnSurfaceVariant;
        _trailIcon.Color = !IsEnabled
            ? c.OnSurface.WithAlpha(0.38f)
            : isErr
                ? c.Error
                : c.OnSurfaceVariant;

        var footColor = isErr ? c.Error : c.OnSurfaceVariant;
        _supportingLabel.Apply(MavTypeRole.BodySmall, t.Typography);
        _supportingLabel.Foreground = MavTheme.BrushOf(footColor);
        _counterLabel.Apply(MavTypeRole.BodySmall, t.Typography);
        _counterLabel.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);

        _chrome.InvalidateVisual();
    }

    private sealed class FieldChrome(MavTextField owner) : Control
    {
        public override void Render(DrawingContext context) =>
            MavDrawingHelpers.DrawFieldChrome(
                context,
                Bounds.Width,
                Bounds.Height,
                owner.Variant == MavTextFieldVariant.Filled,
                owner.IsEnabled,
                owner.HasError,
                owner._entry.IsFocused,
                owner.Label,
                owner._floatProgress,
                owner._chromeCache);
    }
}
