using Mav.Essentials;

namespace Hc.Services.Settings;

/// <summary>
/// App settings kept in plain preferences: time zone, language and theme. Nothing secret goes here.
/// Empty values mean "use the default" (system time zone, system language, system theme).
/// </summary>
public sealed class SettingsService(IMavPreferences preferences)
{
    public const string TimezoneKey = "settings.timezone";
    public const string LanguageKey = "settings.language";
    public const string ThemeKey = "settings.theme";

    /// <summary>Time zone id, for example "Europe/Kyiv". Defaults to the device's time zone.</summary>
    public string PreferredTimezone
    {
        get
        {
            var saved = preferences.Get(TimezoneKey, string.Empty);
            return saved.Length > 0 ? saved : TimeZoneInfo.Local.Id;
        }
        set => preferences.Set(TimezoneKey, value);
    }

    /// <summary>Language code ("en" or "uk"). Empty means the system language.</summary>
    public string Language
    {
        get => preferences.Get(LanguageKey, string.Empty);
        set => preferences.Set(LanguageKey, value);
    }

    /// <summary>Theme name: "system", "light" or "dark". Empty means "system".</summary>
    public string Theme
    {
        get => preferences.Get(ThemeKey, string.Empty);
        set => preferences.Set(ThemeKey, value);
    }
}
