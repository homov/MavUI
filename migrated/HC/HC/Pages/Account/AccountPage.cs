using Hc.Pages.Models;
using Hc.Pages.Shell;
using Mav;
using Mav.Controls;

namespace Hc.Pages.Account;

/// <summary>
/// Base for the account pages. Field rules are checked with <see cref="MavFormValidator"/> (errors appear under
/// the fields). Server calls run through <see cref="PageRunner"/>: the button shows a busy state, and a failed call
/// shows its message in the form's <see cref="MavBanner"/>.
/// </summary>
public abstract class AccountPage : MavContentPage
{
    /// <summary>Navigation of the app. Set by the page that opens this one, before it is shown.</summary>
    public required AppNavigator Navigator { get; init; }

    /// <summary>Runs one cloud call. See <see cref="PageRunner.RunAsync"/>.</summary>
    protected static Task RunAsync(MavButton? button, MavBanner banner, Func<Task> action) =>
        PageRunner.RunAsync(button, banner, action);

    /// <summary>Localized text by key; the fallback is used only when the key is missing.</summary>
    protected static string Tr(string key, string? fallback = null) => MavLocalization.Get(key, fallback ?? key);
}
