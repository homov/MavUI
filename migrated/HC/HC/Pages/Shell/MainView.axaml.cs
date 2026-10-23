using Avalonia.Controls;
using Mav;

namespace Hc.Pages.Shell;

/// <summary>Shell: owns the page stack. The real start page (login or device list) is set in stage 5.</summary>
public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        Navigation.SetRoot(new StartPage());
    }
}
