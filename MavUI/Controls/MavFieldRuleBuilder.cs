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

    /// <summary>Creates a rule builder for one field, which the validator uses to store the rules and the error.</summary>
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

    /// <summary>Adds a rule that fails for empty text.</summary>
    public MavFieldRuleBuilder Required(string message = "This field is required")
    {
        _rules.Add(v => string.IsNullOrWhiteSpace(v) ? message : null);
        return this;
    }

    /// <summary>Adds a rule that fails for text shorter than <paramref name="minLength"/>.</summary>
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

    /// <summary>Adds a rule that fails for text longer than <paramref name="maxLength"/>.</summary>
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

    /// <summary>Adds a rule that fails for text that is not an email address.</summary>
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

    /// <summary>Adds a rule that fails for text that is not a number.</summary>
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

    /// <summary>Adds a rule that fails for text that does not match <paramref name="regexPattern"/>.</summary>
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

    /// <summary>Adds a rule that fails when the text differs from the value returned by <paramref name="otherValueGetter"/>.</summary>
    public MavFieldRuleBuilder Matches(Func<string?> otherValueGetter, string message = "Values do not match")
    {
        _rules.Add(v => string.Equals(v ?? string.Empty, otherValueGetter() ?? string.Empty, StringComparison.Ordinal)
            ? null
            : message);
        return this;
    }

    /// <summary>Adds a rule that fails when <paramref name="predicate"/> returns <c>false</c>.</summary>
    public MavFieldRuleBuilder Must(Func<string?, bool> predicate, string message)
    {
        _rules.Add(v => predicate(v) ? null : message);
        return this;
    }

    /// <summary>Runs the rules in order and returns the first error message, or <c>null</c> if the value is valid.</summary>
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

    /// <summary>Clears the error message shown for the field.</summary>
    internal void ClearError()
    {
        _hasBeenValidated = false;
        _setError(null);
    }
}
