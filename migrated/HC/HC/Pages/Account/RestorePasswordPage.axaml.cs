using Hc.Pages.Models;
using Hc.Pages.Shell;
using Hc.Services;

namespace Hc.Pages.Account;

/// <summary>Asks the server to send a reset code. The code page then sets the new password.</summary>
public partial class RestorePasswordPage : AccountPage
{
    private readonly AppNavigator _navigator;

    public RestorePasswordPage(AppNavigator navigator)
    {
        InitializeComponent();
        _navigator = navigator;
        SendButton.Clicked += OnSend;
    }

    private async void OnSend(object? sender, EventArgs e)
    {
        var email = EmailField.Text.Trim();

        if (!FormRules.IsValidEmail(email))
        {
            ShowError(ErrorLabel, Tr("auth.error.email"));
            return;
        }

        await RunAsync(SendButton, ErrorLabel, async () =>
        {
            await AppServices.Default.Account.RequestPasswordResetAsync(email);
            _navigator.Push(new CodeEntryPage(_navigator, email, AccountCodePurpose.RestorePassword));
        });
    }
}
