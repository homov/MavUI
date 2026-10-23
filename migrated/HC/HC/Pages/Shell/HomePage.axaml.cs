using Hc.Services;
using Mav.Controls;

namespace Hc.Pages.Shell;

/// <summary>Signed-in start page. Groups and devices come next; for now it only offers sign-out.</summary>
public partial class HomePage : MavContentPage
{
    public HomePage(AppNavigator navigator)
    {
        InitializeComponent();
        SignOutButton.Clicked += (_, _) =>
        {
            AppServices.Default.Session.SignOut();
            navigator.ShowSignIn();
        };
    }
}
