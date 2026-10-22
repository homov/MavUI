using Xamarin.CommunityToolkit.Extensions;
using Xamarin.CommunityToolkit.UI.Views;

namespace HeatController;

public class NavigationService
{
    private readonly IMainThread mainThread;

    public NavigationService(IMainThread mainThread)
    {
        this.mainThread = mainThread;
    }

    public void SetMainPage<P>()
    {
        var page = ContainerService.Container.Resolve<P>() as ContentPage;
        mainThread.BeginInvokeOnMainThread(() => { Application.Current.MainPage = new NavigationPage(page); });
    }

    public void SetMainPage(ContentPage page)
    {
        Application.Current.MainPage = new NavigationPage(page);
    }


    public async Task<object> ShowPopup( Popup popup )
    {
        var value = await Application.Current.MainPage.Navigation.ShowPopupAsync(popup);
        return value;
    }

    public Task PopToRootAsync()
    {
        return mainThread.InvokeOnMainThreadAsync(Application.Current.MainPage.Navigation.PopToRootAsync);
    }

    public Task PopAsync()
    {
        return mainThread.InvokeOnMainThreadAsync(Application.Current.MainPage.Navigation.PopAsync);
    }

    public async Task PushAsync(ContentPage page)    
    {
        await Application.Current.MainPage.Navigation.PushAsync(page);
    }

    public async Task<P> PushAsync<P>() where P: Page
    {
        P page = ContainerService.Container.Resolve<P>() as P;
        await Application.Current.MainPage.Navigation.PushAsync(page);
        return page;
    }
}
