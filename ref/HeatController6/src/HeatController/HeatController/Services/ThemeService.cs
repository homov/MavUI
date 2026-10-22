
using Syncfusion.XForms.Themes;

namespace HeatController;

public class ThemeService
{
    private readonly SettingService settingService;
    private readonly IMainThread mainThread;

    public Theme[] GetSupportedThemes()
    {
        return new Theme[] {
            new(PreferredTheme.Default, Resources.Default),
            new(PreferredTheme.Dark, Resources.Dark),
            new(PreferredTheme.Light, Resources.Light)
        };
    }

    public ThemeService(SettingService settingService, IMainThread mainThread)
    {
        this.settingService = settingService;
        this.mainThread = mainThread;
    }

    public PreferredTheme PreferredTheme
    {
        get => settingService.PreferredTheme;
        set
        {
            settingService.PreferredTheme = value;
            SetTheme(value).SafeFireAndForget();
        }
    }

    public Task Initialize()
    {
        if (Application.Current != null)
            Application.Current.RequestedThemeChanged += HandleRequestedThemeChanged;
        return SetTheme(PreferredTheme);
    }

    private void HandleRequestedThemeChanged(object sender, AppThemeChangedEventArgs e)
    {
        if (PreferredTheme is PreferredTheme.Default)
            SetTheme(PreferredTheme.Default);
    }


    public Task SetTheme(PreferredTheme theme)
    {
        if (Application.Current == null)
            return Task.CompletedTask;

        if (Application.Current.Resources.GetType() == theme.GetType())
            return Task.CompletedTask;

        return mainThread.InvokeOnMainThreadAsync(() =>
        {
            ResourceDictionary newTheme = theme switch
            {
                PreferredTheme.Dark => new Dark(),
                PreferredTheme.Light => new Light(),
                PreferredTheme.Default => Application.Current.RequestedTheme is OSAppTheme.Dark ? new Dark() : new Light(),
                _ => throw new NotSupportedException()
            };
            Application.Current.Resources = newTheme;
        });
    }
}

public class Theme
{
    public Theme(PreferredTheme preferredTheme, string name)
    {
        PreferredTheme = preferredTheme;
        Name = name;
    }

    public PreferredTheme PreferredTheme { get; }
    public string Name { get; }
}
