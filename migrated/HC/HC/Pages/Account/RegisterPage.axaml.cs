using Hc.Pages.Shell;
using Hc.Services;
using Mav.Controls;

namespace Hc.Pages.Account;

/// <summary>Creates an account. The server then emails a code, which the next page asks for.</summary>
public partial class RegisterPage : AccountPage
{
    private readonly AppNavigator _navigator;
    private readonly MavFormValidator _validator = new();

    public RegisterPage(AppNavigator navigator)
    {
        InitializeComponent();
        _navigator = navigator;

        _validator.Field(EmailField).Required(Tr("validation.required")).Email(Tr("validation.email"));
        _validator.Field(PasswordField).Required(Tr("validation.required"));
        _validator.Field(RepeatField)
            .Required(Tr("validation.required"))
            .Matches(() => PasswordField.Text, Tr("validation.mismatch"));

        CreateButton.Clicked += OnCreate;
    }

    private async void OnCreate(object? sender, EventArgs e)
    {
        if (!_validator.Validate())
            return;

        var email = EmailField.Text.Trim();
        var password = PasswordField.Text ?? string.Empty;

        await RunAsync(CreateButton, ErrorBanner, async () =>
        {
            await AppServices.Default.Account.RegisterAsync(email, password);
            _navigator.Push(new CodeEntryPage(_navigator, email, AccountCodePurpose.ConfirmEmail));
        });
    }
}
