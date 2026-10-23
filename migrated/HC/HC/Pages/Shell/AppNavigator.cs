using Hc.Pages.Account;
using Hc.Pages.Home;
using Mav.Controls;

namespace Hc.Pages.Shell;

/// <summary>
/// The app's navigation in one place. Pages call these methods instead of touching the navigation control,
/// so the start-up flow (autologin, sign-in, home) stays in one file.
/// </summary>
public sealed class AppNavigator(MavNavigationPage navigation)
{
    /// <summary>Resets the stack to the sign-in form.</summary>
    public void ShowSignIn() => navigation.SetRoot(new LoginPage { Navigator = this });

    /// <summary>Resets the stack to the signed-in home page.</summary>
    public void ShowHome() => navigation.SetRoot(new HomePage { Navigator = this });

    public void Push(MavContentPage page) => navigation.Push(page);

    /// <summary>Returns to the previous page.</summary>
    public void Pop() => navigation.Pop();

    public void Replace(MavContentPage page) => navigation.Replace(page);
}
