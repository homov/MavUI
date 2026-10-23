using Hc.Services;
using Mav.Controls;

namespace Hc.Pages.Account;

/// <summary>
/// Changes the password of the signed-in account. The server checks the current one. On success the saved
/// password is removed (it is now wrong), and the user signs in again with the new one.
/// </summary>
public partial class ChangePasswordPage : AccountPage
{
    private readonly MavFormValidator _validator = new();

    public ChangePasswordPage()
    {
        InitializeComponent();

        _validator.Field(CurrentField).Required(Tr("validation.required"));
        _validator.Field(NewField).Required(Tr("validation.required"));
        _validator.Field(RepeatField)
            .Required(Tr("validation.required"))
            .Matches(() => NewField.Text, Tr("validation.mismatch"));

        SaveButton.Clicked += OnSave;
    }

    private async void OnSave(object? sender, EventArgs e)
    {
        if (!_validator.Validate())
            return;

        var current = CurrentField.Text ?? string.Empty;
        var newPassword = NewField.Text ?? string.Empty;

        await RunAsync(SaveButton, ErrorBanner, async () =>
        {
            var session = AppServices.Default.Session;
            await AppServices.Default.Account.ChangePasswordAsync(session.Email, current, newPassword);
            session.SignOut();
            Navigator.ShowSignIn();
        });
    }
}
