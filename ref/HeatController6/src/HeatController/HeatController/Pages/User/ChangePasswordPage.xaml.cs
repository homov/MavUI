namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class ChangePasswordPage : BasePage
{
    public ChangePasswordPage()
    {
        InitializeComponent();
        this.BindingContext = ContainerService.Container.Resolve<ChangePasswordPageViewModel>();
    }
}