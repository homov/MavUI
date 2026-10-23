using Hc.Pages.Models;
using Mav;
using Mav.Controls;

namespace Hc.Pages.Account;

/// <summary>
/// Base for the account pages. Runs one server action while its button shows a busy state; a failure is shown
/// under the form instead of crashing the page.
/// </summary>
public abstract class AccountPage : MavContentPage
{
    protected static async Task RunAsync(MavButton button, MavLabel error, Func<Task> action)
    {
        error.IsVisible = false;
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
            error.Text = ErrorText.From(ex);
            error.IsVisible = true;
        }
        finally
        {
            button.IsLoading = false;
        }
    }

    /// <summary>Localized text by key; the fallback is used only when the key is missing.</summary>
    protected static string Tr(string key, string? fallback = null) => MavLocalization.Get(key, fallback ?? key);

    protected static void ShowError(MavLabel error, string text)
    {
        error.Text = text;
        error.IsVisible = true;
    }
}
