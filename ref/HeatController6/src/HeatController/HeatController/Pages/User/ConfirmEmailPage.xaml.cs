namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class ConfirmEmailPage : BasePage
{
    public ConfirmEmailPage()
    {
        InitializeComponent();
        this.BindingContext = ContainerService.Container.Resolve<ConfirmEmailPageViewModel>();
    }
}