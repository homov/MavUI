using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Mav.Platform;

namespace Mav.Controls;

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

    /// <summary>
    /// Gets the form validator attached to the element.
    /// </summary>
    public static MavFormValidator? GetValidator(Control element) => element.GetValue(ValidatorProperty);
    /// <summary>
    /// Attaches a form validator to the element.
    /// </summary>
    public static void SetValidator(Control element, MavFormValidator? value) => element.SetValue(ValidatorProperty, value);

    /// <summary>
    /// Gets whether the element is required to have a value.
    /// </summary>
    public static bool GetIsRequired(Control element) => element.GetValue(IsRequiredProperty);
    /// <summary>
    /// Sets whether the element is required to have a value.
    /// </summary>
    public static void SetIsRequired(Control element, bool value) => element.SetValue(IsRequiredProperty, value);

    /// <summary>
    /// Gets the message shown when the required rule fails.
    /// </summary>
    public static string GetRequiredMessage(Control element) => element.GetValue(RequiredMessageProperty);
    /// <summary>
    /// Sets the message shown when the required rule fails.
    /// </summary>
    public static void SetRequiredMessage(Control element, string value) => element.SetValue(RequiredMessageProperty, value);

    /// <summary>
    /// Gets whether the element must contain a valid email address.
    /// </summary>
    public static bool GetIsEmail(Control element) => element.GetValue(IsEmailProperty);
    /// <summary>
    /// Sets whether the element must contain a valid email address.
    /// </summary>
    public static void SetIsEmail(Control element, bool value) => element.SetValue(IsEmailProperty, value);

    /// <summary>
    /// Gets the message shown when the email rule fails.
    /// </summary>
    public static string GetEmailMessage(Control element) => element.GetValue(EmailMessageProperty);
    /// <summary>
    /// Sets the message shown when the email rule fails.
    /// </summary>
    public static void SetEmailMessage(Control element, string value) => element.SetValue(EmailMessageProperty, value);

    /// <summary>
    /// Gets whether the element must contain a number.
    /// </summary>
    public static bool GetIsNumeric(Control element) => element.GetValue(IsNumericProperty);
    /// <summary>
    /// Sets whether the element must contain a number.
    /// </summary>
    public static void SetIsNumeric(Control element, bool value) => element.SetValue(IsNumericProperty, value);

    /// <summary>
    /// Gets the message shown when the numeric rule fails.
    /// </summary>
    public static string GetNumericMessage(Control element) => element.GetValue(NumericMessageProperty);
    /// <summary>
    /// Sets the message shown when the numeric rule fails.
    /// </summary>
    public static void SetNumericMessage(Control element, string value) => element.SetValue(NumericMessageProperty, value);

    /// <summary>
    /// Gets the minimum number of characters the element must contain.
    /// </summary>
    public static int GetMinLength(Control element) => element.GetValue(MinLengthProperty);
    /// <summary>
    /// Sets the minimum number of characters the element must contain.
    /// </summary>
    public static void SetMinLength(Control element, int value) => element.SetValue(MinLengthProperty, value);

    /// <summary>
    /// Gets the maximum number of characters the element may contain.
    /// </summary>
    public static int GetMaxLength(Control element) => element.GetValue(MaxLengthProperty);
    /// <summary>
    /// Sets the maximum number of characters the element may contain.
    /// </summary>
    public static void SetMaxLength(Control element, int value) => element.SetValue(MaxLengthProperty, value);

    /// <summary>
    /// Gets the regular expression the element's value must match.
    /// </summary>
    public static string? GetPattern(Control element) => element.GetValue(PatternProperty);
    /// <summary>
    /// Sets the regular expression the element's value must match.
    /// </summary>
    public static void SetPattern(Control element, string? value) => element.SetValue(PatternProperty, value);

    /// <summary>
    /// Gets the message shown when the pattern rule fails.
    /// </summary>
    public static string GetPatternMessage(Control element) => element.GetValue(PatternMessageProperty);
    /// <summary>
    /// Sets the message shown when the pattern rule fails.
    /// </summary>
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
