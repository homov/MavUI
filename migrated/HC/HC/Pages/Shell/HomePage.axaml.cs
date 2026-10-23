using Hc.Services;
using Mav.Controls;

namespace Hc.Pages.Shell;

/// <summary>Signed-in start page. Groups and devices come next; for now it only offers sign-out.</summary>
public partial class HomePage : MavContentPage
{
    public HomePage()
    {
        InitializeComponent();
        SignOutButton.Clicked += (_, _) =>
        {
            AppServices.Default.Session.SignOut();
            Navigator.ShowSignIn();
        };
    }

    /// <summary>Navigation of the app. Set before the page is shown.</summary>
    public required AppNavigator Navigator { get; init; }
}
