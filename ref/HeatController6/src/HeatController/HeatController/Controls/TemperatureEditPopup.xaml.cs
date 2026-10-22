using Syncfusion.SfGauge.XForms;
using System.Windows.Input;
using Xamarin.CommunityToolkit.UI.Views;
using Xamarin.Forms;

namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class TemperatureEditPopup : Popup
{
    public ICommand CancelCommand { get; }
    public ICommand OkCommand { get; }
    
    public TemperatureEditPopup()
    {
        CancelCommand = new Command(Cancel);
        OkCommand= new Command(Ok);
        InitializeComponent();

        //this.Size = PopupSize.GetSize();
    }

    public TemperatureEditPopup(double value):this()
    {
        temperatureEdit.Value = value;
    }

    public TemperatureEditPopup(double value, double minimum, double maximum, string valueName, string valueFormat) : this()
    {
        temperatureEdit.MinValue = minimum;
        temperatureEdit.MaxValue = maximum;
        temperatureEdit.Value = value;
        temperatureEdit.ValueName = valueName;
        temperatureEdit.ValueFormat = valueFormat;
        this.header.Text = valueName;
    }

    private void Ok(object obj)
    {
        double value = temperatureEdit.Value;
        this.Dismiss(value);
    }

    private void Cancel(object obj)
    {
        this.Dismiss(null);
    }
}