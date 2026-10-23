using Hc.Services;
using Mav.Controls;

namespace Hc.Pages.Shell;

/// <summary>
/// Shown at launch. Tries autologin with the saved password; on success goes home, otherwise to sign-in.
/// Any failure (including secure storage not available) leads to sign-in, never to a blank screen.
/// </summary>
public partial class StartPage : MavContentPage
{
    private readonly AppNavigator _navigator;
    private bool _started;

    public StartPage(AppNavigator navigator)
    {
        InitializeComponent();
        _navigator = navigator;
        Loaded += OnLoaded;
    }

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
            _navigator.ShowHome();
        else
            _navigator.ShowSignIn();
    }
}
