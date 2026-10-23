using Hc.Pages.Models;
using Hc.Pages.Shell;
using Hc.Services;

namespace Hc.Pages.Account;

/// <summary>
/// Enters the 4-digit code from the email. For a new account it confirms the email; for a forgotten password
/// it also takes the new password. On success the stack returns to sign-in.
/// </summary>
public partial class CodeEntryPage : AccountPage
{
    private readonly AppNavigator _navigator;
    private readonly string _email;
    private readonly AccountCodePurpose _purpose;

    public CodeEntryPage(AppNavigator navigator, string email, AccountCodePurpose purpose)
    {
        InitializeComponent();
        _navigator = navigator;
        _email = email;
        _purpose = purpose;

        HintLabel.Text = string.Format(Tr("auth.code.hint"), email);
        var isReset = purpose == AccountCodePurpose.RestorePassword;
        NewPasswordField.IsVisible = isReset;
        ConfirmButton.Text = Tr(isReset ? "auth.code.reset" : "auth.code.confirm");
        ConfirmButton.Clicked += OnConfirm;
    }

    private async void OnConfirm(object? sender, EventArgs e)
    {
        var code = CodeInput.Code ?? string.Empty;
        if (!FormRules.IsValidCode(code))
        {
            ShowError(ErrorLabel, Tr("auth.error.code"));
            return;
        }

        var password = NewPasswordField.Text ?? string.Empty;
        if (_purpose == AccountCodePurpose.RestorePassword && password.Length == 0)
        {
            ShowError(ErrorLabel, Tr("auth.error.password"));
            return;
        }

        await RunAsync(ConfirmButton, ErrorLabel, async () =>
        {
            if (_purpose == AccountCodePurpose.ConfirmEmail)
                await AppServices.Default.Account.ConfirmEmailAsync(_email, code);
            else
                await AppServices.Default.Account.ResetPasswordAsync(_email, password, code);

            _navigator.ShowSignIn();
        });
    }
}
