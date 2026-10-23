using Hc.Services;
using Mav.Controls;

namespace Hc.Pages.Account;

/// <summary>
/// Enters the 4-digit code from the email. For a new account it confirms the email; for a forgotten password
/// it also takes the new password. On success the stack returns to sign-in.
/// </summary>
public partial class CodeEntryPage : AccountPage
{
    private readonly MavFormValidator _validator = new();
    private string _email = string.Empty;
    private AccountCodePurpose _purpose;

    public CodeEntryPage()
    {
        InitializeComponent();
        ConfirmButton.Clicked += OnConfirm;
    }

    /// <summary>The address the code was sent to. Shown in the hint.</summary>
    public required string Email
    {
        get => _email;
        init
        {
            _email = value;
            HintLabel.Text = string.Format(Tr("auth.code.hint"), value);
        }
    }

    /// <summary>Decides what the code confirms. Sets the password field and the field rules.</summary>
    public required AccountCodePurpose Purpose
    {
        get => _purpose;
        init
        {
            _purpose = value;
            var isReset = value == AccountCodePurpose.RestorePassword;
            NewPasswordField.IsVisible = isReset;
            ConfirmButton.Text = Tr(isReset ? "auth.code.reset" : "auth.code.confirm");

            _validator.Field(CodeInput).Required(Tr("validation.code")).Pattern(@"^\d{4}$", Tr("validation.code"));
            if (isReset)
                _validator.Field(NewPasswordField).Required(Tr("validation.required"));
        }
    }

    private async void OnConfirm(object? sender, EventArgs e)
    {
        if (!_validator.Validate())
            return;

        var code = CodeInput.Code;
        var password = NewPasswordField.Text ?? string.Empty;

        await RunAsync(ConfirmButton, ErrorBanner, async () =>
        {
            if (_purpose == AccountCodePurpose.ConfirmEmail)
                await AppServices.Default.Account.ConfirmEmailAsync(_email, code);
            else
                await AppServices.Default.Account.ResetPasswordAsync(_email, password, code);

            Navigator.ShowSignIn();
        });
    }
}
