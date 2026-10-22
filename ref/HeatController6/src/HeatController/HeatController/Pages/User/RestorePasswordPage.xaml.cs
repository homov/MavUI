namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class RestorePasswordPage : BasePage
{
    public RestorePasswordPage()
    {
        InitializeComponent();
        this.BindingContext = ContainerService.Container.Resolve<RestorePasswordPageViewModel>();
    }
}