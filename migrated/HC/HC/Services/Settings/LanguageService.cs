using System.Globalization;

namespace Hc.Services.Settings;

/// <summary>
/// Language of the app. Two languages are shipped: English (default) and Ukrainian. A saved choice is applied to
/// the current UI culture; with no saved choice the system language is used if it is Ukrainian, otherwise English.
/// </summary>
public sealed class LanguageService(SettingsService settings)
{
    public const string English = "en";
    public const string Ukrainian = "uk";

    public static IReadOnlyList<string> SupportedLanguages { get; } = [English, Ukrainian];

    /// <summary>The language in effect: the saved one, or the system language when nothing is saved.</summary>
    public string Current => Resolve(settings.Language);

    /// <summary>Saves the language and applies it. Unknown codes are stored as "system".</summary>
    public void Set(string code)
    {
        settings.Language = SupportedLanguages.Contains(code) ? code : string.Empty;
        Apply();
    }

    /// <summary>Applies the language to the current thread and to new threads. Call once at startup.</summary>
    public void Apply()
    {
        var culture = new CultureInfo(Current);
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public static string Resolve(string saved)
    {
        if (SupportedLanguages.Contains(saved))
            return saved;

        var system = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return system == Ukrainian ? Ukrainian : English;
    }
}
