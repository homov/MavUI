using HeatCeram.Api;
using System;
using System.Collections.Generic;
using System.Text;

namespace HeatController;

public partial class RestorePasswordPageViewModel: BaseViewModel
{
    private readonly SettingService settingService;
    private readonly HeatCeramService heatCeramService;

    public RestorePasswordPageViewModel(SettingService settingService,  HeatCeramService heatCeramService)
    {
        this.settingService = settingService;
        this.heatCeramService = heatCeramService;
    }

    [ObservableProperty]
    string code;

    [ObservableProperty]
    string password;

    [ObservableProperty]
    string confirmPassword;

    private void Validate()
    {
        if (String.IsNullOrWhiteSpace(this.Code))
            throw new ValidationException(Resources.CodeNotSpecified);
        if (String.IsNullOrWhiteSpace(this.Password))
            throw new ValidationException(Resources.PasswordNotSpecified);
        if (this.Password != this.ConfirmPassword)
            throw new ValidationException(Resources.PasswordsDoNotMatch);
    }

    [RelayCommand]
    async Task Save()
    {
        try
        {
            Validate();

            using var busy = Busy();

            var encryptedPassword = PasswordEncrypter.Encrypt(this.Password);
            await heatCeramService.RestorePassword(settingService.UserEmail, encryptedPassword, this.Code);
            await navigationService.PopToRootAsync(); 
        }
        catch (Exception exception)
        {
            await messageBoxService.Alert(exception);
        }
    }
}
