using System.Globalization;
using Mav;

namespace Hc.Localization;

/// <summary>
/// Strings for code-behind and ViewModels. Two languages only: English (default) and Ukrainian.
/// Any other language falls back to English. XAML keys keep their English fallback text.
/// </summary>
public sealed class HcLocalization : IMavLocalizationProvider
{
    public static readonly HcLocalization Instance = new();

    public string? GetString(string key, CultureInfo? culture = null)
    {
        var language = (culture ?? CultureInfo.CurrentUICulture).TwoLetterISOLanguageName;
        var table = language == "uk" ? Uk.Strings : En.Strings;
        return table.TryGetValue(key, out var value) ? value : null;
    }
}
