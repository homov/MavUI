using Hc.Pages.Shell;
using Hc.Services;
using Mav.Controls;

namespace Hc.Pages.Account;

/// <summary>Sign-in form. The email is prefilled from the last sign-in; "remember me" keeps the password for autologin.</summary>
public partial class LoginPage : AccountPage
{
    private readonly AppNavigator _navigator;
    private readonly MavFormValidator _validator = new();

    public LoginPage(AppNavigator navigator)
    {
        InitializeComponent();
        _navigator = navigator;

        var session = AppServices.Default.Session;
        EmailField.Text = session.Email;
        RememberBox.IsChecked = session.AutologinEnabled;

        _validator.Field(EmailField).Required(Tr("validation.required")).Email(Tr("validation.email"));
        _validator.Field(PasswordField).Required(Tr("validation.required"));

        SignInButton.Clicked += OnSignIn;
        RegisterButton.Clicked += (_, _) => _navigator.Push(new RegisterPage(_navigator));
        RestoreButton.Clicked += (_, _) => _navigator.Push(new RestorePasswordPage(_navigator));
    }

    private async void OnSignIn(object? sender, EventArgs e)
    {
        if (!_validator.Validate())
            return;

        var email = EmailField.Text.Trim();
        var password = PasswordField.Text ?? string.Empty;

        await RunAsync(SignInButton, ErrorBanner, async () =>
        {
            await AppServices.Default.Session.LoginAsync(email, password, RememberBox.IsChecked);
            _navigator.ShowHome();
        });
    }
}
