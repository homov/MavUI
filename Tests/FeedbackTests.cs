using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Theming;
using Xunit;

namespace Md3.Avalonia.Tests;

public class FeedbackTests
{
    [AvaloniaFact]
    public void MdSnackbar_show_clears_pending_queue()
    {
        // No TopLevel is attached in this headless test, so Show only exercises the
        // global FIFO contract: Show() clears the queue, Enqueue() appends.
        MdSnackbar.Enqueue("one", duration: MdSnackbarDuration.Indefinite);
        MdSnackbar.Enqueue("two", duration: MdSnackbarDuration.Indefinite);
        Assert.True(MdSnackbar.QueueCount >= 1);

        MdSnackbar.Show("Hello", duration: MdSnackbarDuration.Short);
        Assert.Equal(0, MdSnackbar.QueueCount);
        MdSnackbar.Dismiss();
    }

    [AvaloniaFact]
    public void Indefinite_snackbar_has_a_close_button()
    {
        var anchor = new MdButton("Anchor");
        var page = new MdContentPage("Root") { Content = anchor };
        var navigation = new MdNavigationPage(page);
        var window = new Window { Width = 400, Height = 600, Content = navigation };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        MdSnackbar.Show("Uploading…", duration: MdSnackbarDuration.Indefinite, anchor: anchor);
        Dispatcher.UIThread.RunJobs();

        var close = navigation.GetVisualDescendants()
            .OfType<MdIconButton>()
            .FirstOrDefault(button => button.Icon == MdIcons.Close);

        Assert.NotNull(close);

        MdSnackbar.Dismiss();
        window.Close();
    }

    [AvaloniaFact]
    public void MdLinearProgress_indeterminate()
    {
        var bar = new MdLinearProgress { IsIndeterminate = true };
        Assert.True(bar.IsIndeterminate);
    }

    [AvaloniaFact]
    public void MdSkeleton_shape()
    {
        var sk = new MdSkeleton { Shape = MdSkeletonShape.Rounded };
        Assert.NotNull(sk);
    }

    [AvaloniaFact]
    public void MdInfoBar_severity()
    {
        var info = new MdInfoBar { Severity = MdInfoBarSeverity.Success, Title = "Done" };
        Assert.Equal(MdInfoBarSeverity.Success, info.Severity);
    }
}
