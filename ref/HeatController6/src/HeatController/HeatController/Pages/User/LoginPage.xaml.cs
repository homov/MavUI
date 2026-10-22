
namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class LoginPage : BasePage
{
    public LoginPage()
    {
        InitializeComponent();
        BindingContext = ContainerService.Container.Resolve<LoginPageViewModel>();
    }
}