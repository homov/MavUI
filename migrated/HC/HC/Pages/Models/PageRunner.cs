using Mav.Controls;

namespace Hc.Pages.Models;

/// <summary>
/// Runs one server call from a page. The button (if any) shows busy while the call runs. A failure is shown in
/// the banner, and the page does not crash. Leaving the page during the call is not an error.
/// </summary>
public static class PageRunner
{
    public static async Task RunAsync(MavButton? button, MavBanner banner, Func<Task> action)
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
}
