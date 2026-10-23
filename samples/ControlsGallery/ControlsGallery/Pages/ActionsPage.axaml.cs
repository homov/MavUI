using Avalonia.Controls;
using Mav;
using Mav.Controls;

namespace Mav.ControlsGallery.Pages;

public partial class ActionsPage : UserControl
{
    private int _clicks;

    public ActionsPage()
    {
        InitializeComponent();

        ClickButton.Clicked += (_, _) =>
        {
            _clicks++;
            ClickCounter.Text = MavLocalization.Format("Actions.Click.Counter.Value", "Clicks: {0}", _clicks);
        };

        LoadingButton.Clicked += async (_, _) =>
        {
            LoadingButton.IsLoading = true;
            await Task.Delay(1200);
            LoadingButton.IsLoading = false;
        };
    }
}
