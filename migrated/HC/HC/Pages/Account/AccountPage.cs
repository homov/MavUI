using Hc.Pages.Models;
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
    /// <summary>Runs one cloud call. A failure is shown in <paramref name="banner"/>; the page does not crash.</summary>
    protected static async Task RunAsync(MavButton button, MavBanner banner, Func<Task> action)
    {
        banner.Dismiss();
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
            button.IsLoading = false;
        }
    }

    /// <summary>Localized text by key; the fallback is used only when the key is missing.</summary>
    protected static string Tr(string key, string? fallback = null) => MavLocalization.Get(key, fallback ?? key);
}
