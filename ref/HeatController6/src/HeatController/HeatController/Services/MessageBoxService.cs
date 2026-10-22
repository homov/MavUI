
namespace HeatController;

public class MessageBoxService
{
    private readonly IMainThread mainThread;

    private static Page CurrentMainPage { get { return Application.Current.MainPage; } }

    public MessageBoxService( IMainThread mainThread)
    {
        this.mainThread = mainThread;
    }

    public async Task Alert(string title, string message, Action onClosed = null)
    {
        await mainThread.InvokeOnMainThreadAsync(async () =>
        {
            await CurrentMainPage.DisplayAlert(title, message, "OK");
        });
        onClosed?.Invoke();
    }

    public async Task Information(string message, Action onClosed = null)
    {
        await mainThread.InvokeOnMainThreadAsync(async () =>
        {
            await CurrentMainPage.DisplayAlert(Resources.Information, message, "OK");
        });
        onClosed?.Invoke();
    }

    public async Task Alert(string message, Action onClosed = null)
    {
        await mainThread.InvokeOnMainThreadAsync(async () =>
        {
            await CurrentMainPage.DisplayAlert(Resources.Error, message, "OK");
        });
        onClosed?.Invoke();
    }

    public async Task Alert(Exception exception, Action onClosed = null)
    {
        var message = ExceptionHelper.GetUserMessage(exception);
        await mainThread.InvokeOnMainThreadAsync(async () =>
        {
            await CurrentMainPage.DisplayAlert(Resources.Error, message, "OK");
        });
        onClosed?.Invoke();
    }

    public async Task<bool> Prompt(string title, string message)
    {
        return await mainThread.InvokeOnMainThreadAsync(async () =>
        { 
            return await CurrentMainPage.DisplayAlert(title, message, "OK", Resources.Cancel);
        });
    }


    public async Task<string> PromptEdit(string title, string message)
    {
        return await mainThread.InvokeOnMainThreadAsync(async () =>
        {
            return await CurrentMainPage.DisplayPromptAsync(title, message, cancel: Resources.Cancel);
        });
    }

    public async Task<string> ActionSheet(string title, string cancel, string destruction, string[] buttons = null)
    {
        var displayButtons = buttons ?? new string[] { };
        return await mainThread.InvokeOnMainThreadAsync(async () =>
        {
            return await CurrentMainPage.DisplayActionSheet(title, cancel, destruction, displayButtons);
        });
    }
}
