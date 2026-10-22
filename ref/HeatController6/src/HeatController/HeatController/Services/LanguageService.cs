using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Xamarin.CommunityToolkit.Helpers;

namespace HeatController;

public class LanguageService
{
    private readonly SettingService settingService;
    private readonly HeatCeramService heatCeramService;
    private readonly IMainThread mainThread;

    public Language[] GetSupportedLanguages()
    {
        return new Language[] {
            new(String.Empty, Resources.Default),
            new("en", "English"),
            new("uk", "Українська"),
            new("pl", "Polski"),
            new("cs", "Čeština"),
            new("de", "Deutsch"),
            new("hu", "Magyar"),
            new("it", "Italiana"),
            new("ro", "Română"),
        };
    }

    public LanguageService(SettingService settingService, HeatCeramService heatCeramService, IMainThread mainThread)
    {
        this.settingService = settingService;
        this.heatCeramService = heatCeramService;
        this.mainThread = mainThread;
    }

    public string Language
    {
        get
        {
            if (string.IsNullOrEmpty(settingService.Language))
                return Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName.Substring(0, 2);
            return settingService.Language;
        }
        set 
        {
            settingService.Language = value;
            SetLanguage(value).SafeFireAndForget();
        }
    }

    public Task Initialize()
    {
        return SetLanguage(Language);
    }


    public Task SetLanguage(string language)
    {
        var l = GetSupportedLanguages().Where(it => it.Code == language).FirstOrDefault();
        if (l == null) language = "en";
        try
        {
            //var code = l == null || String.IsNullOrEmpty(l.Code) ? "en" : l.Code;
            heatCeramService.GetErrorCodes(language).ContinueWith(it =>
            {
                ExceptionHelper.ErrorMessages = it.Result;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        return mainThread.InvokeOnMainThreadAsync(() =>
        {
            var cultureInfo = GetCultureInfo(language);
           
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            Resources.Culture = Thread.CurrentThread.CurrentUICulture;
            LocalizationResourceManager.Current.CurrentCulture = Thread.CurrentThread.CurrentUICulture;
        });
    }

    static CultureInfo? GetCultureInfo(string? culture) => culture switch
    {
        null => null,
        _ => new CultureInfo(culture, false)
    };

}

 public class Language
{
    public Language(string code, string name)
    {
        Code = code;
        Name = name;
    }
    public string Code { get; }
    public string Name { get; }
}