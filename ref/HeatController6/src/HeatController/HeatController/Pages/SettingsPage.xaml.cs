namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class SettingsPage : BasePage
{
    public SettingsPage()
    {
        InitializeComponent();
        this.BindingContext = ContainerService.Container.Resolve<SettingsPageViewModel>();
    }
}