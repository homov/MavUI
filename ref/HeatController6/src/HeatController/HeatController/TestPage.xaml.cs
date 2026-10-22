using HeatController.Models;

namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class TestPage : BasePage
{
    public TestPage()
    {
        InitializeComponent();
    }

    protected async override void OnAppearing()
    {
    }
}