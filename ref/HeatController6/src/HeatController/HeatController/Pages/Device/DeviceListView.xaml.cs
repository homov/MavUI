namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class DeviceListView : ContentView
{
    public DeviceListViewModel ViewModel { get => this.BindingContext as DeviceListViewModel; }
    public DeviceListView()
    {
        InitializeComponent();
        this.BindingContext = ContainerService.Container.Resolve<DeviceListViewModel>();
    }

    public static readonly BindableProperty LocationIdProperty = BindableProperty.Create(
        nameof(LocationId),
        typeof(string),
        typeof(DeviceListView),
        null,
        BindingMode.OneWay,
        propertyChanged: OnLocationIdPropertyChanged);

    private static void OnLocationIdPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is DeviceListView deviceListView)
        {
            deviceListView.ViewModel.SetLocation((string)newValue);
        }
    }

    public string LocationId
    {
        get => (string)GetValue(LocationIdProperty);
        set => SetValue(LocationIdProperty, value);
    }

}