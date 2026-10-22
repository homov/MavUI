using System.Globalization;
using System.Resources;
using Avalonia;
using Avalonia.Controls;

namespace Mav;

/// <summary>
/// Pluggable localization provider for Material Design 3 built-in strings
/// (date/time picker dialogs, tooltips, accessibility labels, action buttons).
/// Return <see langword="null"/> or an empty string when a key is not available;
/// <see cref="MavLocalization.Get"/> then uses its fallback text.
/// </summary>
public interface IMavLocalizationProvider
{
    string? GetString(string key, CultureInfo? culture = null);
}
