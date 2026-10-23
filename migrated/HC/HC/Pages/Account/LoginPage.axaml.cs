using Hc.Pages.Models;
using Hc.Pages.Shell;
using Hc.Services;

namespace Hc.Pages.Account;

/// <summary>Sign-in form. The email is prefilled from the last sign-in; "remember me" keeps the password for autologin.</summary>
public partial class LoginPage : AccountPage
{
    private readonly AppNavigator _navigator;

    public LoginPage(AppNavigator navigator)
    {
        InitializeComponent();
        _navigator = navigator;

        var session = AppServices.Default.Session;
        EmailField.Text = session.Email;
        RememberBox.IsChecked = session.AutologinEnabled;

        SignInButton.Clicked += OnSignIn;
        RegisterButton.Clicked += (_, _) => _navigator.Push(new RegisterPage(_navigator));
        RestoreButton.Clicked += (_, _) => _navigator.Push(new RestorePasswordPage(_navigator));
    }

    private async void OnSignIn(object? sender, EventArgs e)
    {
        var email = EmailField.Text.Trim();
        var password = PasswordField.Text ?? string.Empty;

        if (!FormRules.IsValidEmail(email))
        {
            ShowError(ErrorBar, Tr("auth.error.email"));
            return;
        }

        if (password.Length == 0)
        {
            ShowError(ErrorBar, Tr("auth.error.password"));
            return;
        }

        await RunAsync(SignInButton, ErrorBar, async () =>
        {
            await AppServices.Default.Session.LoginAsync(email, password, RememberBox.IsChecked);
            _navigator.ShowHome();
        });
    }
}
