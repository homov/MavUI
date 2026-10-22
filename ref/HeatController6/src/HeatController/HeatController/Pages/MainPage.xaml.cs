using CommunityToolkit.Mvvm.Messaging;
using HeatController.Messages;

namespace HeatController;

public partial class MainPage : BasePage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = ContainerService.Container.Resolve<MainPageViewModel>();
    }


    private void UpdateTitle(int index)
    {
        if (tabView.Items.Count > 0 && index >= 0)
            Title = tabView.Items[index].Title;
    }

    private void tabView_SelectionChanged(object sender, Syncfusion.XForms.TabView.SelectionChangedEventArgs e)
    {
        UpdateTitle(e.Index);
        if (deviceListView.ViewModel.LocationId == null && e.Index == 1)
            deviceListView.ViewModel.SetLocation("all");
        if (e.Index == 1)
            deviceListView.ViewModel.StartAutoreload();
        else
            deviceListView.ViewModel.StopAutoreload();
    }

    protected override void OnDisappearing()
    {
        deviceListView.ViewModel.StopAutoreload();
    }

    protected override void OnAppearing()
    {
        UpdateTitle(tabView.SelectedIndex);
        WeakReferenceMessenger.Default.Send(new LocationListChangedMessage(ChangeType.Created));
        if (tabView.SelectedIndex == 1)
        {
            deviceListView.ViewModel.ReloadDevices();
            deviceListView.ViewModel.StartAutoreload();
        }
    }

    private void tabView_SelectionChanging(object sender, Syncfusion.XForms.TabView.SelectionChangingEventArgs e)
    {
    }
}
