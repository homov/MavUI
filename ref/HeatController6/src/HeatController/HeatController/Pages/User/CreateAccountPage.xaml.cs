namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class CreateAccountPage : BasePage
{
    public CreateAccountPage()
    {
        InitializeComponent();
        this.BindingContext = ContainerService.Container.Resolve<CreateAccountPageViewModel>();
    }
}