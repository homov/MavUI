namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class SimpleDevicePage : BasePage
{
    public SimpleDevicePage()
    {
        InitializeComponent();
    }

    public SimpleDevicePage(DevicePageViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}