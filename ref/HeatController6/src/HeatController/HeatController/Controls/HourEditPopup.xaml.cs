using HeatController.Models;
using System.Windows.Input;
using Xamarin.CommunityToolkit.UI.Views;

namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class HourEditPopup : Popup
{
    public ICommand CancelCommand { get; }
    public ICommand OkCommand { get; }

    public HourEditPopup(HourRange hourRange):this()
    {
        this.HourEdit.StartValue = hourRange.Start;
        this.HourEdit.EndValue= hourRange.End;
    }

    public HourEditPopup()
    {
        CancelCommand = new Command(Cancel);
        OkCommand = new Command(Ok);

        InitializeComponent();
    }

    private void Ok(object obj)
    {
        var hourRange = new HourRange()
        {
            Start = (int)this.HourEdit.StartValue,
            End = (int)this.HourEdit.EndValue
        };
        this.Dismiss(hourRange);
    }

    private void Cancel(object obj)
    {
        this.Dismiss(null);
    }
}