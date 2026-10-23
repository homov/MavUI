using Hc.Services;
using Mav.Controls;

namespace Hc.Pages.Shell;

/// <summary>
/// Shown at launch. Tries autologin with the saved password; on success goes home, otherwise to sign-in.
/// Any failure (including secure storage not available) leads to sign-in, never to a blank screen.
/// </summary>
public partial class StartPage : MavContentPage
{
    private bool _started;

    public StartPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    /// <summary>Navigation of the app. Set before the page is shown.</summary>
    public required AppNavigator Navigator { get; init; }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (_started)
            return;
        _started = true;

        bool signedIn;
        try
        {
            signedIn = await AppServices.Default.Session.TryAutoLoginAsync();
        }
        catch (Exception)
        {
            signedIn = false;
        }

        if (signedIn)
            Navigator.ShowHome();
        else
            Navigator.ShowSignIn();
    }
}
