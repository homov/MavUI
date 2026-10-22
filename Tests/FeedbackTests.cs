using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mav;
using Mav.Controls;
using Mav.Theming;
using Xunit;

namespace Mav.Tests;

public class FeedbackTests
{
    [AvaloniaFact]
    public void MavSnackbar_show_clears_pending_queue()
    {
        // No TopLevel is attached in this headless test, so Show only exercises the
        // global FIFO contract: Show() clears the queue, Enqueue() appends.
        MavSnackbar.Enqueue("one", duration: MavSnackbarDuration.Indefinite);
        MavSnackbar.Enqueue("two", duration: MavSnackbarDuration.Indefinite);
        Assert.True(MavSnackbar.QueueCount >= 1);

        MavSnackbar.Show("Hello", duration: MavSnackbarDuration.Short);
        Assert.Equal(0, MavSnackbar.QueueCount);
        MavSnackbar.Dismiss();
    }

    [AvaloniaFact]
    public void Indefinite_snackbar_has_a_close_button()
    {
        var anchor = new MavButton("Anchor");
        var page = new MavContentPage("Root") { Content = anchor };
        var navigation = new MavNavigationPage(page);
        var window = new Window { Width = 400, Height = 600, Content = navigation };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        MavSnackbar.Show("Uploading…", duration: MavSnackbarDuration.Indefinite, anchor: anchor);
        Dispatcher.UIThread.RunJobs();

        var close = navigation.GetVisualDescendants()
            .OfType<MavIconButton>()
            .FirstOrDefault(button => button.Icon == MavIcons.Close);

        Assert.NotNull(close);

        MavSnackbar.Dismiss();
        window.Close();
    }

    [AvaloniaFact]
    public void MavLinearProgress_indeterminate()
    {
        var bar = new MavLinearProgress { IsIndeterminate = true };
        Assert.True(bar.IsIndeterminate);
    }

    [AvaloniaFact]
    public void MavSkeleton_shape()
    {
        var sk = new MavSkeleton { Shape = MavSkeletonShape.Rounded };
        Assert.NotNull(sk);
    }

    [AvaloniaFact]
    public void MavInfoBar_severity()
    {
        var info = new MavInfoBar { Severity = MavInfoBarSeverity.Success, Title = "Done" };
        Assert.Equal(MavInfoBarSeverity.Success, info.Severity);
    }
}
