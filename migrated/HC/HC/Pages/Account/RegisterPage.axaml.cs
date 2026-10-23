using Hc.Pages.Models;
using Hc.Pages.Shell;
using Hc.Services;

namespace Hc.Pages.Account;

/// <summary>Creates an account. The server then emails a code, which the next page asks for.</summary>
public partial class RegisterPage : AccountPage
{
    private readonly AppNavigator _navigator;

    public RegisterPage(AppNavigator navigator)
    {
        InitializeComponent();
        _navigator = navigator;
        CreateButton.Clicked += OnCreate;
    }

    private async void OnCreate(object? sender, EventArgs e)
    {
        var email = EmailField.Text.Trim();
        var password = PasswordField.Text ?? string.Empty;

        if (!FormRules.IsValidEmail(email))
        {
            ShowError(ErrorLabel, Tr("auth.error.email"));
            return;
        }

        if (password.Length == 0)
        {
            ShowError(ErrorLabel, Tr("auth.error.password"));
            return;
        }

        if (password != RepeatField.Text)
        {
            ShowError(ErrorLabel, Tr("auth.error.mismatch"));
            return;
        }

        await RunAsync(CreateButton, ErrorLabel, async () =>
        {
            await AppServices.Default.Account.RegisterAsync(email, password);
            _navigator.Push(new CodeEntryPage(_navigator, email, AccountCodePurpose.ConfirmEmail));
        });
    }
}
