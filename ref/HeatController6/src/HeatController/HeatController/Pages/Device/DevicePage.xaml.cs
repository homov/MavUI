namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class DevicePage : BasePage
{
    public DevicePage()
    {
        InitializeComponent();
    }

    public DevicePage(DevicePageViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}