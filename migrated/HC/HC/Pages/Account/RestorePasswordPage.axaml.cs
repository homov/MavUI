using Hc.Pages.Shell;
using Hc.Services;
using Mav.Controls;

namespace Hc.Pages.Account;

/// <summary>Asks the server to send a reset code. The code page then sets the new password.</summary>
public partial class RestorePasswordPage : AccountPage
{
    private readonly AppNavigator _navigator;
    private readonly MavFormValidator _validator = new();

    public RestorePasswordPage(AppNavigator navigator)
    {
        InitializeComponent();
        _navigator = navigator;

        _validator.Field(EmailField).Required(Tr("validation.required")).Email(Tr("validation.email"));

        SendButton.Clicked += OnSend;
    }

    private async void OnSend(object? sender, EventArgs e)
    {
        if (!_validator.Validate())
            return;

        var email = EmailField.Text.Trim();

        await RunAsync(SendButton, ErrorBanner, async () =>
        {
            await AppServices.Default.Account.RequestPasswordResetAsync(email);
            _navigator.Push(new CodeEntryPage(_navigator, email, AccountCodePurpose.RestorePassword));
        });
    }
}
