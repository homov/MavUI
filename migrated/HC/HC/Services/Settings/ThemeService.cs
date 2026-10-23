using Mav.Theming;

namespace Hc.Services.Settings;

/// <summary>
/// Light, dark or system theme. The choice is saved in <see cref="SettingsService"/> and applied to MavUI.
/// </summary>
public sealed class ThemeService(SettingsService settings)
{
    public const string SystemName = "system";
    public const string LightName = "light";
    public const string DarkName = "dark";

    /// <summary>The theme name in effect ("system", "light" or "dark").</summary>
    public string Current => Normalize(settings.Theme);

    /// <summary>Saves the theme and applies it to MavUI. Unknown names fall back to "system".</summary>
    public void Set(string name)
    {
        var normalized = Normalize(name);
        settings.Theme = normalized;
        Apply(normalized);
    }

    /// <summary>Applies the saved theme. Call after <c>UseMav</c> has configured the theme.</summary>
    public void Apply() => Apply(Current);

    private static void Apply(string name) => MavTheme.Mode = name switch
    {
        LightName => MavThemeMode.Light,
        DarkName => MavThemeMode.Dark,
        _ => MavThemeMode.System,
    };

    private static string Normalize(string name) =>
        name is LightName or DarkName ? name : SystemName;
}
