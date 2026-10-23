using Avalonia.Interactivity;
using Hc.Pages.Account;
using Hc.Pages.Models;
using Hc.Services;
using Hc.Services.Settings;
using Mav;
using Mav.Controls;

namespace Hc.Pages.Settings;

/// <summary>
/// Settings: language, theme, time zone, and the account (email, password, sign-out, account deletion).
/// A language change rebuilds the stack, so every page opens in the new language.
/// </summary>
public partial class SettingsPage : AccountPage
{
    private static readonly string[] ThemeNames = [ThemeService.SystemName, ThemeService.LightName, ThemeService.DarkName];

    private IReadOnlyList<string> _zones = [];

    public SettingsPage()
    {
        InitializeComponent();

        FillLanguages();
        FillThemes();
        BuildAccountRows();

        LanguageSelect.SelectedIndexChanged += OnLanguageChanged;
        ThemeSelect.SelectedIndexChanged += OnThemeChanged;
        TimezoneSelect.SelectedIndexChanged += OnTimezoneChanged;
        Loaded += OnLoaded;
    }

    private void FillLanguages()
    {
        foreach (var code in LanguageService.SupportedLanguages)
            LanguageSelect.AddOption(LanguageName(code));
        LanguageSelect.SelectedIndex = LanguageService.SupportedLanguages.ToList().IndexOf(AppServices.Default.Language.Current);
    }

    private void FillThemes()
    {
        foreach (var name in ThemeNames)
            ThemeSelect.AddOption(ThemeLabel(name));
        ThemeSelect.SelectedIndex = ThemeNames.ToList().IndexOf(AppServices.Default.Theme.Current);
    }

    private void BuildAccountRows()
    {
        AccountRows.Children.Add(new MavSettingsCard(
            Tr("settings.email", "Email"), AppServices.Default.Session.Email, MavIcons.Email));
        AddRow(Tr("password.change.title", "Change password"), MavIcons.Lock, OnChangePassword);
        AddRow(Tr("settings.signout", "Sign out"), MavIcons.Logout, OnSignOut);
        AddRow(Tr("settings.delete", "Delete account"), MavIcons.AccountRemove, OnDeleteAccount);
    }

    private void AddRow(string header, string icon, Action onClick)
    {
        var row = new MavSettingsCard(header, null, icon, null, true);
        row.Clicked += (_, _) => onClick();
        AccountRows.Children.Add(row);
    }

    private static string LanguageName(string code) => code switch
    {
        LanguageService.Ukrainian => "Українська",
        _ => "English",
    };

    private static string ThemeLabel(string name) => name switch
    {
        ThemeService.LightName => Tr("settings.theme.light", "Light"),
        ThemeService.DarkName => Tr("settings.theme.dark", "Dark"),
        _ => Tr("settings.theme.system", "System"),
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
        Navigator.Push(new SettingsPage { Navigator = Navigator });
    }

    private void OnThemeChanged(object? sender, int index)
    {
        if (index < 0 || index >= ThemeNames.Length)
            return;

        var name = ThemeNames[index];
        if (name == AppServices.Default.Theme.Current)
            return;

        AppServices.Default.Theme.Set(name);
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e) => await LoadTimezonesAsync();

    /// <summary>Loads the server's time zones. The saved zone is always in the list, even if the load fails.</summary>
    private async Task LoadTimezonesAsync()
    {
        var current = AppServices.Default.Settings.PreferredTimezone;
        var zones = new List<string>();
        try
        {
            zones.AddRange(await AppServices.Default.Account.GetTimezonesAsync());
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            ErrorBanner.Message = ErrorText.From(ex);
            ErrorBanner.Show();
        }

        if (!zones.Contains(current))
            zones.Insert(0, current);

        _zones = zones;
        TimezoneSelect.Options.Clear();
        foreach (var zone in zones)
            TimezoneSelect.AddOption(zone);
        TimezoneSelect.SelectedIndex = zones.IndexOf(current);
    }

    private async void OnTimezoneChanged(object? sender, int index)
    {
        if (index < 0 || index >= _zones.Count)
            return;

        var zone = _zones[index];
        var previous = AppServices.Default.Settings.PreferredTimezone;
        if (zone == previous)
            return;

        await RunAsync(null, ErrorBanner, async () =>
        {
            try
            {
                await AppServices.Default.Account.SetTimezoneAsync(zone);
            }
            catch
            {
                // The zone was not saved: show the previous one again.
                TimezoneSelect.SelectedIndex = _zones.IndexOf(previous);
                throw;
            }
        });
    }

    private void OnChangePassword() =>
        Navigator.Push(new ChangePasswordPage { Navigator = Navigator });

    private void OnSignOut()
    {
        AppServices.Default.Session.SignOut();
        Navigator.ShowSignIn();
    }

    private async void OnDeleteAccount()
    {
        var confirmed = await MavDialogService.Instance.ShowConfirmAsync(
            Tr("settings.delete.confirm.title", "Confirmation"),
            Tr("settings.delete.confirm", "Delete the account? Continue?"),
            Tr("settings.delete", "Delete account"),
            Tr("common.cancel", "Cancel"));
        if (!confirmed)
            return;

        await RunAsync(null, ErrorBanner, async () =>
        {
            await AppServices.Default.Account.DeleteAccountAsync();
            AppServices.Default.Session.SignOut();
            Navigator.ShowSignIn();
        });
    }
}
