using HeatController;

namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class ForgotPasswordPage : BasePage
{
    public ForgotPasswordPage()
    {
        InitializeComponent();
        this.BindingContext = ContainerService.Container.Resolve<ForgotPasswordPageViewModel>();
    }
}