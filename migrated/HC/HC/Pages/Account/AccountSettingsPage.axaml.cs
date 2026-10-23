using Hc.Services;
using Hc.Services.Settings;
using Mav.Controls;

namespace Hc.Pages.Account;

/// <summary>Account settings: the app language and the entry to change the password.</summary>
public partial class AccountSettingsPage : AccountPage
{
    public AccountSettingsPage()
    {
        InitializeComponent();

        foreach (var code in LanguageService.SupportedLanguages)
            LanguageSelect.AddOption(LanguageName(code));
        LanguageSelect.SelectedIndex = LanguageService.SupportedLanguages.ToList().IndexOf(AppServices.Default.Language.Current);
        LanguageSelect.SelectedIndexChanged += OnLanguageChanged;

        PasswordButton.Clicked += OnChangePassword;
    }

    private static string LanguageName(string code) => code switch
    {
        LanguageService.Ukrainian => "Українська",
        _ => "English",
    };

    private void OnLanguageChanged(object? sender, int index)
    {
        if (index < 0 || index >= LanguageService.SupportedLanguages.Count)
            return;

        var code = LanguageService.SupportedLanguages[index];
        if (code == AppServices.Default.Language.Current)
            return;

        AppServices.Default.Language.Set(code);

        // Rebuild the stack so every page is created in the new language, and keep this page open.
        Navigator.ShowHome();
        Navigator.Push(new AccountSettingsPage { Navigator = Navigator });
    }

    private void OnChangePassword(object? sender, EventArgs e) =>
        Navigator.Push(new ChangePasswordPage { Navigator = Navigator });
}
