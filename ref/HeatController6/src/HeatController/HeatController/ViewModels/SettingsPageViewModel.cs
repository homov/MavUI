using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace HeatController;

public partial class SettingsPageViewModel: BaseViewModel
{
    private readonly SettingService settingService;
    private readonly ThemeService themeService;
    private readonly LanguageService languageService;
    private readonly HeatCeramService heatCeramService;

    [ObservableProperty]
    ObservableCollection<SettingsItemViewModel> items = new();


    string[] timeZones;

    [ObservableProperty]
    string email;

    public SettingsPageViewModel(SettingService settingService, ThemeService themeService, LanguageService languageService, HeatCeramService heatCeramService)
    {
        this.settingService = settingService;
        this.themeService = themeService;
        this.languageService = languageService;
        this.heatCeramService = heatCeramService;
        this.email = settingService.UserEmail;

        LoadItems();
    }


    [RelayCommand]
    async Task ChooseTimeZone(SettingsItemViewModel item)
    {
        try
        {
            using var busy = Busy();

            if (timeZones == null)
            {
                timeZones = (await heatCeramService.GetTimeZones()).ToArray();
            }
            var selection = await messageBoxService.ActionSheet(Resources.ChooseTimezone, Resources.Cancel, null, timeZones);
            if (timeZones.Contains(selection))
            {
                await heatCeramService.SetUserTimeZone(selection);
                settingService.PreferredTimezone = selection;
                item.Value = selection;
            }
        }
        catch (Exception exception)
        {
            await messageBoxService.Alert(exception);
        }
    }

    [RelayCommand]
    async Task ChangePassword()
    {
        await navigationService.PushAsync<ChangePasswordPage>();
    }

    [RelayCommand]
    async Task Logout()
    {
        settingService.UserPassword = String.Empty;
        navigationService.SetMainPage<LoginPage>();
    }

    [RelayCommand]
    async Task RemoveAccount()
    {
        try
        {
            if (await messageBoxService.Prompt(Resources.Confirmation, Resources.ConfirmAccountDelete))
            {
                using var busy = Busy();

                await heatCeramService.DeleteUser();
                navigationService.SetMainPage<LoginPage>();
            }
        }
        catch (Exception exception)
        {
            await messageBoxService.Alert(exception);
        }

    }

    [RelayCommand]
    async Task ChooseTheme()
    {
        var themes = themeService.GetSupportedThemes();
        var themeStrings = themes.Select(it => it.Name).ToArray();
        var selection = await messageBoxService.ActionSheet(Resources.ChooseTheme, Resources.Cancel, null, themeStrings);
        var theme = themes.FirstOrDefault(it => it.Name == selection);
        if (theme != null)
            themeService.PreferredTheme = theme.PreferredTheme;

    }

    [RelayCommand]
    async Task ChooseLanguage()
    {
        var languages = languageService.GetSupportedLanguages();
        var languageStrings = languages.Select(it => it.Name).ToArray();
        var selection = await messageBoxService.ActionSheet(Resources.ChooseLanguage, Resources.Cancel, null, languageStrings);
        var language = languages.FirstOrDefault(it => it.Name == selection);
        if (language != null)
        {
            languageService.Language = language.Code;
            await Task.Delay(200);
            await mainThread.InvokeOnMainThreadAsync( async () => {
                await LoadItems();
            });
        }
    }

#if DEBUG
    [RelayCommand]
    async Task Test()
    {
        await navigationService.PushAsync<TestPage>();
    }
#endif

    async  Task LoadItems()
    {
        Items = new ObservableCollection<SettingsItemViewModel>(new SettingsItemViewModel[]
        {
            new(text:Resources.Account,isGroup:true),
            new(Icons.Email, Resources.Email, settingService.UserEmail, null ),
            new(Icons.Web, Resources.ChooseTimezone, settingService.PreferredTimezone, ChooseTimeZoneCommand ),
            new(Icons.Lastpass, Resources.ChangePassword, null, ChangePasswordCommand ),
            new(Icons.Logout, Resources.Logout, null, LogoutCommand ),
            new(Icons.AccountMinus, Resources.RemoveAccount, null, RemoveAccountCommand ),
            
            new(text:Resources.ApplicationSettings,isGroup:true),
            new(Icons.Brightness4, Resources.Theme, null, ChooseThemeCommand ),
            new(Icons.Translate, Resources.Language, null, ChooseLanguageCommand),
#if DEBUG
            new(Icons.Bug, "Test", null, TestCommand),
#endif
        });
    }
}

public partial class SettingsItemViewModel : BaseViewModel
{
    [ObservableProperty]
    bool isGroup;

    [ObservableProperty]
    string icon;

    [ObservableProperty]
    string text;

    [ObservableProperty]
    string value;

    [ObservableProperty]
    ICommand command;

    public SettingsItemViewModel(string icon=null, string text = null, string value = null, ICommand command = null, bool isGroup=false)
    {
        this.icon = icon;
        this.text = text;
        this.value = value;
        this.command = command;
        this.isGroup = isGroup;
    }
    public bool HasCommand => command != null;
    public bool HasIcon => icon != null;
    public bool NotHasIcon => !HasIcon;
    public bool HasValue => !String.IsNullOrEmpty(value);
    public bool NotHasValue => !HasValue;
    public bool NotIsGroup => !IsGroup;
}
