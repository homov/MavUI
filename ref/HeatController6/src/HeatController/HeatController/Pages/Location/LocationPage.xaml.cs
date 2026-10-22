namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class LocationPage : BasePage
{
    public LocationPageViewModel ViewModel { get => BindingContext as LocationPageViewModel; }
    public LocationPage()
    {
        InitializeComponent();
    }

    public LocationPage(LocationPageViewModel viewModel)
    {
        this.BindingContext = viewModel;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (tabView.SelectedIndex == 1)
        {
            deviceListView.ViewModel.ReloadDevices();
            deviceListView.ViewModel.StartAutoreload();
        }
    }

    protected override void OnDisappearing()
    {
        deviceListView.ViewModel.StopAutoreload();
    }

    private void tabView_SelectionChanged(object sender, Syncfusion.XForms.TabView.SelectionChangedEventArgs e)
    {
        if (e.Index == 1 && deviceListView.ViewModel.LocationId == null)
        {
            deviceListView.ViewModel.SetLocation(ViewModel.UserLocation.Info.GroupId);
        }
        if (e.Index == 1)
            deviceListView.ViewModel.StartAutoreload();
        else
            deviceListView.ViewModel.StopAutoreload();

    }
}