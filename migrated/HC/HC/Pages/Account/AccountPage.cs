using Hc.Pages.Models;
using Mav;
using Mav.Controls;

namespace Hc.Pages.Account;

/// <summary>
/// Base for the account pages. Runs one server action while its button shows a busy state; a failure is shown
/// in the form's <see cref="MavInfoBar"/> (error severity) instead of crashing the page.
/// </summary>
public abstract class AccountPage : MavContentPage
{
    protected static async Task RunAsync(MavButton button, MavInfoBar error, Func<Task> action)
    {
        error.IsOpen = false;
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
            error.Message = ErrorText.From(ex);
            error.IsOpen = true;
        }
        finally
        {
            button.IsLoading = false;
        }
    }

    /// <summary>Localized text by key; the fallback is used only when the key is missing.</summary>
    protected static string Tr(string key, string? fallback = null) => MavLocalization.Get(key, fallback ?? key);

    /// <summary>Shows a validation message in the form's error bar.</summary>
    protected static void ShowError(MavInfoBar error, string text)
    {
        error.Message = text;
        error.IsOpen = true;
    }
}
