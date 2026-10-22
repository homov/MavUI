using System.Windows.Input;
using Xamarin.CommunityToolkit.UI.Views;

namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class SelectDevicePopup : Popup
{
    public ICommand OkCommand { get; set; }
    public ICommand CancelCommand { get; set; }
    
    public SelectDevicePopup(SelectDeviceViewModel model)
    {
        this.OkCommand = new Command(Ok);
        this.CancelCommand = new Command(Cancel);
        this.BindingContext = model;
        InitializeComponent();
    }

    private void Cancel(object obj)
    {
        this.Dismiss(null);
    }

    private void Ok(object obj)
    {
        this.Dismiss(obj);
    }
}