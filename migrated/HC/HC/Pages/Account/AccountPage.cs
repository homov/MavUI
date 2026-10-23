using Hc.Pages.Models;
using Hc.Pages.Shell;
using Mav;
using Mav.Controls;

namespace Hc.Pages.Account;

/// <summary>
/// Base for the account pages. Field rules are checked with <see cref="MavFormValidator"/> (errors appear under
/// the fields). Server calls run here: the button shows a busy state, and a failed call shows its message in the
/// form's <see cref="MavBanner"/>.
/// </summary>
public abstract class AccountPage : MavContentPage
{
    /// <summary>Navigation of the app. Set by the page that opens this one, before it is shown.</summary>
    public required AppNavigator Navigator { get; init; }

    /// <summary>Runs one cloud call. The button (if any) shows busy. A failure is shown in <paramref name="banner"/>; the page does not crash.</summary>
    protected static async Task RunAsync(MavButton? button, MavBanner banner, Func<Task> action)
    {
        banner.Dismiss();
        if (button is not null)
            button.IsLoading = true;
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            // The page was left while the request was running: nothing to show.
        }
        catch (Exception ex)
        {
            banner.Message = ErrorText.From(ex);
            banner.Show();
        }
        finally
        {
            if (button is not null)
                button.IsLoading = false;
        }
    }

    /// <summary>Localized text by key; the fallback is used only when the key is missing.</summary>
    protected static string Tr(string key, string? fallback = null) => MavLocalization.Get(key, fallback ?? key);
}
