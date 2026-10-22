namespace HeatController;

public class SettingService
{

    public SettingService(IPreferences preferences)
    {
        Preferences = preferences;
    }

    public string UserEmail
    {
        get => Preferences.Get(nameof(UserEmail), string.Empty);
        set => Preferences.Set(nameof(UserEmail), value);
    }

    public string UserPassword
    {
        get => Preferences.Get(nameof(UserPassword), string.Empty);
        set => Preferences.Set(nameof(UserPassword), value);
    }

    public bool UserAutologin
    {
        get => Preferences.Get(nameof(UserAutologin), false);
        set => Preferences.Set(nameof(UserAutologin), value);
    }

    public string PreferredTimezone
    {
        get => Preferences.Get(nameof(PreferredTimezone), TimeZoneInfo.Local.Id);
        set => Preferences.Set(nameof(PreferredTimezone), value);
    }

    public string Language
    {
        get => Preferences.Get(nameof(Language), string.Empty);
        set => Preferences.Set(nameof(Language), value);
    }

    public PreferredTheme PreferredTheme
    {
        get => (PreferredTheme)Preferences.Get(nameof(PreferredTheme), (int)PreferredTheme.Default);
        set => Preferences.Set(nameof(PreferredTheme), (int)value);
    }
    public IPreferences Preferences { get; }
}
