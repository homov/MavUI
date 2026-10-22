using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Mav.Platform;

namespace Mav.Controls;

/// <summary>
/// Fluent validation rule builder for a single Material Design 3 input control.
/// </summary>
public sealed class MavFieldRuleBuilder
{
    private readonly Func<string?> _getValue;
    private readonly Action<string?> _setError;
    private readonly List<Func<string?, string?>> _rules = [];
    private bool _hasBeenValidated;

    internal MavFieldRuleBuilder(
        Func<string?> getValue,
        Action<string?> setError,
        Action<Action>? subscribeChanged = null,
        Func<bool>? validateOnChangeGetter = null)
    {
        _getValue = getValue;
        _setError = setError;

        subscribeChanged?.Invoke(() =>
        {
            if (_hasBeenValidated && (validateOnChangeGetter?.Invoke() ?? true))
                Validate();
        });
    }

    public MavFieldRuleBuilder Required(string message = "This field is required")
    {
        _rules.Add(v => string.IsNullOrWhiteSpace(v) ? message : null);
        return this;
    }

    public MavFieldRuleBuilder MinLength(int minLength, string? message = null)
    {
        var msg = message ?? $"Must be at least {minLength} characters";
        _rules.Add(v =>
        {
            if (string.IsNullOrEmpty(v)) return null;
            return v.Length < minLength ? msg : null;
        });
        return this;
    }

    public MavFieldRuleBuilder MaxLength(int maxLength, string? message = null)
    {
        var msg = message ?? $"Must be at most {maxLength} characters";
        _rules.Add(v =>
        {
            if (string.IsNullOrEmpty(v)) return null;
            return v.Length > maxLength ? msg : null;
        });
        return this;
    }

    public MavFieldRuleBuilder Email(string message = "Enter a valid email address")
    {
        _rules.Add(v =>
        {
            if (string.IsNullOrWhiteSpace(v)) return null;
            var at = v.IndexOf('@');
            var dot = v.LastIndexOf('.');
            return at > 0 && dot > at + 1 && dot < v.Length - 1 ? null : message;
        });
        return this;
    }

    public MavFieldRuleBuilder Numeric(string message = "Enter a valid number")
    {
        _rules.Add(v =>
        {
            if (string.IsNullOrWhiteSpace(v)) return null;
            return double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ||
                   double.TryParse(v, NumberStyles.Float, CultureInfo.CurrentCulture, out _)
                ? null
                : message;
        });
        return this;
    }

    public MavFieldRuleBuilder Pattern(string regexPattern, string message = "Invalid format")
    {
        var regex = new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        _rules.Add(v =>
        {
            if (string.IsNullOrEmpty(v)) return null;
            return regex.IsMatch(v) ? null : message;
        });
        return this;
    }

    public MavFieldRuleBuilder Matches(Func<string?> otherValueGetter, string message = "Values do not match")
    {
        _rules.Add(v => string.Equals(v ?? string.Empty, otherValueGetter() ?? string.Empty, StringComparison.Ordinal)
            ? null
            : message);
        return this;
    }

    public MavFieldRuleBuilder Must(Func<string?, bool> predicate, string message)
    {
        _rules.Add(v => predicate(v) ? null : message);
        return this;
    }

    internal string? Validate()
    {
        _hasBeenValidated = true;
        var val = _getValue();
        foreach (var rule in _rules)
        {
            var err = rule(val);
            if (!string.IsNullOrEmpty(err))
            {
                _setError(err);
                return err;
            }
        }

        _setError(null);
        return null;
    }

    internal void ClearError()
    {
        _hasBeenValidated = false;
        _setError(null);
    }
}

/// <summary>
/// Declarative Material Design 3 form validator that coordinates validation rules and error messages
/// across <see cref="MavTextField"/>, <see cref="MavTextArea"/>, <see cref="MavSelect"/>, <see cref="MavComboBox"/>,
/// <see cref="MavPinInput"/>, <see cref="MavDatePickerField"/>, <see cref="MavDateRangePickerField"/>, and <see cref="MavTimePickerField"/>.
/// </summary>
public sealed class MavFormValidator
{
    private readonly List<MavFieldRuleBuilder> _fields = [];
    private readonly List<string> _lastErrors = [];

    public event EventHandler<bool>? ValidationChanged;

    public bool ValidateOnChange { get; set; } = true;

    public bool IsValid => _lastErrors.Count == 0;

    public IReadOnlyList<string> Errors => _lastErrors;

    public MavFieldRuleBuilder Field(
        Func<string?> valueGetter,
        Action<string?> errorSetter,
        Action<Action>? subscribeChanged = null)
    {
        var builder = new MavFieldRuleBuilder(
            valueGetter,
            errorSetter,
            subscribeChanged,
            () => ValidateOnChange);
        _fields.Add(builder);
        return builder;
    }

    public MavFieldRuleBuilder Field(MavTextField textField) =>
        Field(
            () => textField.Text,
            err => textField.ErrorText = err,
            cb => textField.TextChanged += (_, _) => cb());

    public MavFieldRuleBuilder Field(MavTextArea textArea) =>
        Field(
            () => textArea.Text,
            err => textArea.ErrorText = err,
            cb => textArea.TextChanged += (_, _) => cb());

    public MavFieldRuleBuilder Field(MavSelect select) =>
        Field(
            () => select.SelectedItem,
            err => select.ErrorText = err,
            cb => select.SelectedIndexChanged += (_, _) => cb());

    public MavFieldRuleBuilder Field(MavComboBox comboBox) =>
        Field(
            () => comboBox.Text,
            err => comboBox.ErrorText = err,
            cb => comboBox.TextChanged += (_, _) => cb());

    public MavFieldRuleBuilder Field(MavPinInput pinInput) =>
        Field(
            () => pinInput.Code.Length >= pinInput.Length ? pinInput.Code : string.Empty,
            err => pinInput.HasError = !string.IsNullOrEmpty(err),
            cb => pinInput.CodeChanged += (_, _) => cb());

    public MavFieldRuleBuilder Field(MavDatePickerField dateField) =>
        Field(
            () => dateField.SelectedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            err => dateField.ErrorText = err,
            cb => dateField.SelectedDateChanged += (_, _) => cb());

    public MavFieldRuleBuilder Field(MavDateRangePickerField rangeField) =>
        Field(
            () => rangeField.StartDate is not null && rangeField.EndDate is not null
                ? $"{rangeField.StartDate:yyyy-MM-dd}..{rangeField.EndDate:yyyy-MM-dd}"
                : null,
            err => rangeField.ErrorText = err,
            cb => rangeField.RangeChanged += (_, _) => cb());

    public MavFieldRuleBuilder Field(MavTimePickerField timeField) =>
        Field(
            () => timeField.SelectedTime?.ToString(),
            err => timeField.ErrorText = err,
            cb => timeField.SelectedTimeChanged += (_, _) => cb());

    public bool Validate()
    {
        _lastErrors.Clear();
        foreach (var field in _fields)
        {
            var err = field.Validate();
            if (!string.IsNullOrEmpty(err))
                _lastErrors.Add(err);
        }

        var valid = _lastErrors.Count == 0;
        ValidationChanged?.Invoke(this, valid);
        return valid;
    }

    public void ClearErrors()
    {
        _lastErrors.Clear();
        foreach (var field in _fields)
            field.ClearError();
        ValidationChanged?.Invoke(this, true);
    }
}

/// <summary>
/// Attached properties for declarative AXAML validation on Material Design 3 input controls
/// (<c>mav:MavValidation.IsRequired="True"</c>, <c>mav:MavValidation.IsEmail="True"</c>, etc.).
/// </summary>
public static class MavValidation
{
    public static readonly AttachedProperty<MavFormValidator?> ValidatorProperty =
        AvaloniaProperty.RegisterAttached<Control, MavFormValidator?>("Validator", typeof(MavValidation), inherits: true);

    public static readonly AttachedProperty<bool> IsRequiredProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsRequired", typeof(MavValidation));

    public static readonly AttachedProperty<string> RequiredMessageProperty =
        AvaloniaProperty.RegisterAttached<Control, string>("RequiredMessage", typeof(MavValidation), "This field is required");

    public static readonly AttachedProperty<bool> IsEmailProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEmail", typeof(MavValidation));

    public static readonly AttachedProperty<string> EmailMessageProperty =
        AvaloniaProperty.RegisterAttached<Control, string>("EmailMessage", typeof(MavValidation), "Enter a valid email address");

    public static readonly AttachedProperty<bool> IsNumericProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsNumeric", typeof(MavValidation));

    public static readonly AttachedProperty<string> NumericMessageProperty =
        AvaloniaProperty.RegisterAttached<Control, string>("NumericMessage", typeof(MavValidation), "Enter a valid number");

    public static readonly AttachedProperty<int> MinLengthProperty =
        AvaloniaProperty.RegisterAttached<Control, int>("MinLength", typeof(MavValidation), 0);

    public static readonly AttachedProperty<int> MaxLengthProperty =
        AvaloniaProperty.RegisterAttached<Control, int>("MaxLength", typeof(MavValidation), 0);

    public static readonly AttachedProperty<string?> PatternProperty =
        AvaloniaProperty.RegisterAttached<Control, string?>("Pattern", typeof(MavValidation));

    public static readonly AttachedProperty<string> PatternMessageProperty =
        AvaloniaProperty.RegisterAttached<Control, string>("PatternMessage", typeof(MavValidation), "Invalid format");

    private static readonly AttachedProperty<MavFieldRuleBuilder?> InternalBuilderProperty =
        AvaloniaProperty.RegisterAttached<Control, MavFieldRuleBuilder?>("InternalBuilder", typeof(MavValidation));

    static MavValidation()
    {
        IsRequiredProperty.Changed.AddClassHandler<Control>((c, _) => RebuildRules(c));
        IsRequiredProperty.Changed.AddClassHandler<Control>((c, e) =>
            MavAutomation.SetRequired(c, e.GetNewValue<bool>()));
        RequiredMessageProperty.Changed.AddClassHandler<Control>((c, _) => RebuildRules(c));
        IsEmailProperty.Changed.AddClassHandler<Control>((c, _) => RebuildRules(c));
        EmailMessageProperty.Changed.AddClassHandler<Control>((c, _) => RebuildRules(c));
        IsNumericProperty.Changed.AddClassHandler<Control>((c, _) => RebuildRules(c));
        NumericMessageProperty.Changed.AddClassHandler<Control>((c, _) => RebuildRules(c));
        MinLengthProperty.Changed.AddClassHandler<Control>((c, _) => RebuildRules(c));
        MaxLengthProperty.Changed.AddClassHandler<Control>((c, _) => RebuildRules(c));
        PatternProperty.Changed.AddClassHandler<Control>((c, _) => RebuildRules(c));
        PatternMessageProperty.Changed.AddClassHandler<Control>((c, _) => RebuildRules(c));
        ValidatorProperty.Changed.AddClassHandler<Control>((c, _) => RebuildRules(c));
    }

    public static MavFormValidator? GetValidator(Control element) => element.GetValue(ValidatorProperty);
    public static void SetValidator(Control element, MavFormValidator? value) => element.SetValue(ValidatorProperty, value);

    public static bool GetIsRequired(Control element) => element.GetValue(IsRequiredProperty);
    public static void SetIsRequired(Control element, bool value) => element.SetValue(IsRequiredProperty, value);

    public static bool GetRequired(Control element) => element.GetValue(IsRequiredProperty);
    public static void SetRequired(Control element, bool value) => element.SetValue(IsRequiredProperty, value);

    public static string GetRequiredMessage(Control element) => element.GetValue(RequiredMessageProperty);
    public static void SetRequiredMessage(Control element, string value) => element.SetValue(RequiredMessageProperty, value);

    public static bool GetIsEmail(Control element) => element.GetValue(IsEmailProperty);
    public static void SetIsEmail(Control element, bool value) => element.SetValue(IsEmailProperty, value);

    public static bool GetEmail(Control element) => element.GetValue(IsEmailProperty);
    public static void SetEmail(Control element, bool value) => element.SetValue(IsEmailProperty, value);

    public static string GetEmailMessage(Control element) => element.GetValue(EmailMessageProperty);
    public static void SetEmailMessage(Control element, string value) => element.SetValue(EmailMessageProperty, value);

    public static bool GetIsNumeric(Control element) => element.GetValue(IsNumericProperty);
    public static void SetIsNumeric(Control element, bool value) => element.SetValue(IsNumericProperty, value);

    public static string GetNumericMessage(Control element) => element.GetValue(NumericMessageProperty);
    public static void SetNumericMessage(Control element, string value) => element.SetValue(NumericMessageProperty, value);

    public static int GetMinLength(Control element) => element.GetValue(MinLengthProperty);
    public static void SetMinLength(Control element, int value) => element.SetValue(MinLengthProperty, value);

    public static int GetMaxLength(Control element) => element.GetValue(MaxLengthProperty);
    public static void SetMaxLength(Control element, int value) => element.SetValue(MaxLengthProperty, value);

    public static string? GetPattern(Control element) => element.GetValue(PatternProperty);
    public static void SetPattern(Control element, string? value) => element.SetValue(PatternProperty, value);

    public static string GetPatternMessage(Control element) => element.GetValue(PatternMessageProperty);
    public static void SetPatternMessage(Control element, string value) => element.SetValue(PatternMessageProperty, value);

    private static bool HasAnyRule(Control c) =>
        GetIsRequired(c) ||
        GetIsEmail(c) ||
        GetIsNumeric(c) ||
        GetMinLength(c) > 0 ||
        GetMaxLength(c) > 0 ||
        !string.IsNullOrEmpty(GetPattern(c));

    private static void RebuildRules(Control c)
    {
        if (!HasAnyRule(c)) return;

        var validator = GetValidator(c);
        MavFieldRuleBuilder? builder = c switch
        {
            MavTextField tf => validator is not null
                ? validator.Field(tf)
                : new MavFieldRuleBuilder(() => tf.Text, err => tf.ErrorText = err, cb => tf.TextChanged += (_, _) => cb()),
            MavTextArea ta => validator is not null
                ? validator.Field(ta)
                : new MavFieldRuleBuilder(() => ta.Text, err => ta.ErrorText = err, cb => ta.TextChanged += (_, _) => cb()),
            MavSelect sel => validator is not null
                ? validator.Field(sel)
                : new MavFieldRuleBuilder(() => sel.SelectedItem, err => sel.ErrorText = err, cb => sel.SelectedIndexChanged += (_, _) => cb()),
            MavComboBox cb => validator is not null
                ? validator.Field(cb)
                : new MavFieldRuleBuilder(() => cb.Text, err => cb.ErrorText = err, ch => cb.TextChanged += (_, _) => ch()),
            MavPinInput pin => validator is not null
                ? validator.Field(pin)
                : new MavFieldRuleBuilder(() => pin.Code.Length >= pin.Length ? pin.Code : string.Empty, err => pin.HasError = !string.IsNullOrEmpty(err), ch => pin.CodeChanged += (_, _) => ch()),
            MavDatePickerField df => validator is not null
                ? validator.Field(df)
                : new MavFieldRuleBuilder(() => df.SelectedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), err => df.ErrorText = err, ch => df.SelectedDateChanged += (_, _) => ch()),
            MavDateRangePickerField rf => validator is not null
                ? validator.Field(rf)
                : new MavFieldRuleBuilder(() => rf.StartDate is not null && rf.EndDate is not null ? $"{rf.StartDate:yyyy-MM-dd}..{rf.EndDate:yyyy-MM-dd}" : null, err => rf.ErrorText = err, ch => rf.RangeChanged += (_, _) => ch()),
            MavTimePickerField tpf => validator is not null
                ? validator.Field(tpf)
                : new MavFieldRuleBuilder(() => tpf.SelectedTime?.ToString(), err => tpf.ErrorText = err, ch => tpf.SelectedTimeChanged += (_, _) => ch()),
            _ => null,
        };

        if (builder is null) return;

        if (GetIsRequired(c))
            builder.Required(GetRequiredMessage(c));
        if (GetMinLength(c) is > 0 and var min)
            builder.MinLength(min);
        if (GetMaxLength(c) is > 0 and var max)
            builder.MaxLength(max);
        if (GetIsEmail(c))
            builder.Email(GetEmailMessage(c));
        if (GetIsNumeric(c))
            builder.Numeric(GetNumericMessage(c));
        if (GetPattern(c) is { Length: > 0 } pat)
            builder.Pattern(pat, GetPatternMessage(c));

        c.SetValue(InternalBuilderProperty, builder);
        c.LostFocus -= OnControlLostFocus;
        c.LostFocus += OnControlLostFocus;
    }

    private static void OnControlLostFocus(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Control c && c.GetValue(InternalBuilderProperty) is { } b)
            b.Validate();
    }

    /// <summary>
    /// Validates all descendant controls inside <paramref name="root"/> that have <see cref="MavValidation"/> attached rules.
    /// </summary>
    public static bool ValidateTree(Control root)
    {
        bool allValid = true;
        if (root.GetValue(InternalBuilderProperty) is { } rootBuilder)
        {
            if (!string.IsNullOrEmpty(rootBuilder.Validate()))
                allValid = false;
        }
        foreach (var desc in root.GetVisualDescendants())
        {
            if (desc is Control ctrl && ctrl.GetValue(InternalBuilderProperty) is { } b)
            {
                if (!string.IsNullOrEmpty(b.Validate()))
                    allValid = false;
            }
        }
        return allValid;
    }
}
