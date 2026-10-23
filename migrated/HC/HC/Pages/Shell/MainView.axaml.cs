using Avalonia.Controls;

namespace Hc.Pages.Shell;

/// <summary>Shell: owns the page stack. Starts on the start page, which decides between home and sign-in.</summary>
public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        Navigation.SetRoot(new StartPage { Navigator = new AppNavigator(Navigation) });
    }
}
