namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class LoadingPage : ContentPage
{
    public LoadingPage()
    {
        InitializeComponent();
        BindingContext = ContainerService.Container.Resolve<LoadingPageViewModel>();
    }

    protected override async void OnAppearing()
    {
        (BindingContext as LoadingPageViewModel).Initialize();
    }
}