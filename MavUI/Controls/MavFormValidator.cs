using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Mav.Platform;

namespace Mav.Controls;

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

    /// <summary>Adds a field with a custom value getter and a field name for the error message.</summary>
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

    /// <summary>Adds a text field to the validation.</summary>
    public MavFieldRuleBuilder Field(MavTextField textField) =>
        Field(
            () => textField.Text,
            err => textField.ErrorText = err,
            cb => textField.TextChanged += (_, _) => cb());

    /// <summary>Adds a multi-line text field to the validation.</summary>
    public MavFieldRuleBuilder Field(MavTextArea textArea) =>
        Field(
            () => textArea.Text,
            err => textArea.ErrorText = err,
            cb => textArea.TextChanged += (_, _) => cb());

    /// <summary>Adds a select field to the validation.</summary>
    public MavFieldRuleBuilder Field(MavSelect select) =>
        Field(
            () => select.SelectedItem,
            err => select.ErrorText = err,
            cb => select.SelectedIndexChanged += (_, _) => cb());

    /// <summary>Adds a combo box to the validation.</summary>
    public MavFieldRuleBuilder Field(MavComboBox comboBox) =>
        Field(
            () => comboBox.Text,
            err => comboBox.ErrorText = err,
            cb => comboBox.TextChanged += (_, _) => cb());

    /// <summary>Adds a PIN input to the validation.</summary>
    public MavFieldRuleBuilder Field(MavPinInput pinInput) =>
        Field(
            () => pinInput.Code.Length >= pinInput.Length ? pinInput.Code : string.Empty,
            err => pinInput.HasError = !string.IsNullOrEmpty(err),
            cb => pinInput.CodeChanged += (_, _) => cb());

    /// <summary>Adds a date field to the validation.</summary>
    public MavFieldRuleBuilder Field(MavDatePickerField dateField) =>
        Field(
            () => dateField.SelectedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            err => dateField.ErrorText = err,
            cb => dateField.SelectedDateChanged += (_, _) => cb());

    /// <summary>Adds a date range field to the validation.</summary>
    public MavFieldRuleBuilder Field(MavDateRangePickerField rangeField) =>
        Field(
            () => rangeField.StartDate is not null && rangeField.EndDate is not null
                ? $"{rangeField.StartDate:yyyy-MM-dd}..{rangeField.EndDate:yyyy-MM-dd}"
                : null,
            err => rangeField.ErrorText = err,
            cb => rangeField.RangeChanged += (_, _) => cb());

    /// <summary>Adds a time field to the validation.</summary>
    public MavFieldRuleBuilder Field(MavTimePickerField timeField) =>
        Field(
            () => timeField.SelectedTime?.ToString(),
            err => timeField.ErrorText = err,
            cb => timeField.SelectedTimeChanged += (_, _) => cb());

    /// <summary>Validates all fields, shows the errors and returns <c>true</c> if every field is valid.</summary>
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

    /// <summary>Clears the error messages of all fields.</summary>
    public void ClearErrors()
    {
        _lastErrors.Clear();
        foreach (var field in _fields)
            field.ClearError();
        ValidationChanged?.Invoke(this, true);
    }
}
