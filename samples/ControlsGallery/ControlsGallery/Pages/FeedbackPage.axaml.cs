using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Mav;
using Mav.Controls;
using Mav.Theming;

namespace Mav.ControlsGallery.Pages;

public partial class FeedbackPage : UserControl
{
    public FeedbackPage()
    {
        InitializeComponent();

        DialogsHost.Children.Add(BuildModalSurfaces());
        MessagesHost.Children.Add(BuildMessages());
        ProgressHost.Children.Add(BuildProgress());
        PlaceholdersHost.Children.Add(BuildSkeletonAndRefresh());
        DataHost.Children.Add(BuildDataWidgets());
    }

    private static Control Row(params Control[] children)
    {
        var panel = new WrapPanel();
        foreach (var child in children)
        {
            child.Margin = new Thickness(0, 0, 8, 8);
            panel.Children.Add(child);
        }
        return panel;
    }

    private static Control Column(params Control[] children)
    {
        var stack = new StackPanel { Spacing = 8 };
        foreach (var child in children)
            stack.Children.Add(child);
        return stack;
    }

    private static Control BuildModalSurfaces()
    {
        var dialog = new MavDialog
        {
            Icon = MavIcons.Delete,
            Title = MavLocalization.Get("Feedback.Dialog.Title", "Delete project?"),
            Message = MavLocalization.Get("Feedback.Dialog.Message", "This action cannot be undone."),
            ConfirmText = MavLocalization.Get("Feedback.Dialog.Delete", "Delete"),
            DismissText = MavLocalization.Get("Feedback.Dialog.Cancel", "Cancel"),
            NeutralText = MavLocalization.Get("Feedback.Dialog.Archive", "Archive"),
        };

        var sheet = new MavBottomSheet
        {
            Title = MavLocalization.Get("Feedback.Sheet.Title", "Filters"),
            Body = new MavLabel { Text = MavLocalization.Get("Feedback.Sheet.Body", "Sheet content: filters, choices, actions.") },
            ShowDragHandle = true,
        };

        var side = new MavSideSheet
        {
            Title = MavLocalization.Get("Feedback.Side.Title", "Details"),
            Body = new MavLabel { Text = MavLocalization.Get("Feedback.Side.Body", "Side sheet content on tablet and desktop.") },
            Placement = MavSideSheetPlacement.End,
        };

        var dialogButton = new MavButton(
            MavLocalization.Get("Feedback.Open.Dialog", "Open dialog"), MavButtonVariant.Filled, MavIcons.Chat);
        dialogButton.Clicked += (_, _) => dialog.Show(dialogButton);

        var sheetButton = new MavButton(
            MavLocalization.Get("Feedback.Open.Sheet", "Open bottom sheet"), MavButtonVariant.Tonal, MavIcons.BottomPanelOpen);
        sheetButton.Clicked += (_, _) => sheet.Show(sheetButton);

        var sideButton = new MavButton(
            MavLocalization.Get("Feedback.Open.Side", "Open side sheet"), MavButtonVariant.Outlined, MavIcons.SideNavigation);
        sideButton.Clicked += (_, _) => side.Show(sideButton);

        return Row(dialogButton, sheetButton, sideButton);
    }

    private Control BuildMessages()
    {
        var snack = new MavButton(MavLocalization.Get("Feedback.Snack.Show", "Show snackbar"), MavButtonVariant.Filled, MavIcons.Notifications);
        snack.Clicked += (_, _) => MavSnackbar.Show(
            MavLocalization.Get("Feedback.Snack.Deleted", "Item deleted"),
            MavLocalization.Get("Feedback.Snack.Undo", "Undo"),
            () => { },
            duration: MavSnackbarDuration.Long,
            anchor: this);

        var queue = new MavButton(MavLocalization.Get("Feedback.Snack.Queue", "Queue snackbars"), MavButtonVariant.Tonal, MavIcons.PlaylistAdd);
        queue.Clicked += (_, _) =>
        {
            MavSnackbar.Enqueue(MavLocalization.Get("Feedback.Snack.Uploading", "Uploading photo…"), duration: MavSnackbarDuration.Short, anchor: this);
            MavSnackbar.Enqueue(MavLocalization.Get("Feedback.Snack.Complete", "Upload complete"),
                MavLocalization.Get("Feedback.Snack.Undo", "Undo"), () => { }, duration: MavSnackbarDuration.Long, anchor: this);
        };

        var dismiss = new MavButton(MavLocalization.Get("Feedback.Snack.Dismiss", "Dismiss snackbar"), MavButtonVariant.Text, MavIcons.Close);
        dismiss.Clicked += (_, _) => MavSnackbar.Dismiss();

        var indefinite = new MavButton(MavLocalization.Get("Feedback.Snack.Indefinite", "Indefinite snackbar"), MavButtonVariant.Outlined, MavIcons.HourglassBottom);
        indefinite.Clicked += (_, _) => MavSnackbar.Show(
            MavLocalization.Get("Feedback.Snack.IndefiniteText", "Indefinite snackbar — dismiss with ×"),
            duration: MavSnackbarDuration.Indefinite, anchor: this);

        var banner = new MavBanner
        {
            Message = MavLocalization.Get("Feedback.Banner.Message", "A new version of Mav is available."),
            PrimaryActionText = MavLocalization.Get("Feedback.Banner.Update", "Update"),
            SecondaryActionText = MavLocalization.Get("Feedback.Banner.Later", "Later"),
        };

        var infoBars = Column(
            new MavInfoBar { Severity = MavInfoBarSeverity.Info, Title = "Info", Message = MavLocalization.Get("Feedback.Info.Text", "Neutral information.") },
            new MavInfoBar { Severity = MavInfoBarSeverity.Success, Title = "Success", Message = MavLocalization.Get("Feedback.Success.Text", "Everything is up to date.") },
            new MavInfoBar { Severity = MavInfoBarSeverity.Warning, Title = "Warning", Message = MavLocalization.Get("Feedback.Warning.Text", "Storage is almost full.") },
            new MavInfoBar { Severity = MavInfoBarSeverity.Error, Title = "Error", Message = MavLocalization.Get("Feedback.Error.Text", "Could not reach the server."), IsClosable = true });

        var empty = new MavEmptyState
        {
            Title = MavLocalization.Get("Feedback.Empty.Title", "No projects yet"),
            Description = MavLocalization.Get("Feedback.Empty.Description", "Create a project or import one from a template."),
            Icon = MavIcons.FolderOpen,
            ActionText = MavLocalization.Get("Feedback.Empty.Action", "Create project"),
        };

        return Column(
            Row(snack, queue, dismiss, indefinite),
            banner,
            infoBars,
            new MavCard(MavCardVariant.Outlined, empty));
    }

    private static Control BuildProgress()
    {
        var linear = new MavLinearProgress { Value = 0.62 };
        var indeterminate = new MavLinearProgress { IsIndeterminate = true };
        var wavy = new MavWavyProgressIndicator { Value = 0.45 };
        var circular = new MavCircularProgress(48, 4) { Value = 0.7 };
        var spinner = new MavCircularProgress(28, 3) { IsIndeterminate = true };
        var segmented = new MavSegmentedProgressBar { Value = 0.6, SegmentCount = 6 };
        var loading = new MavLoadingIndicator { IsContained = true };

        return Column(
            linear,
            indeterminate,
            wavy,
            Row(circular, spinner, loading),
            segmented);
    }

    private static Control BuildSkeletonAndRefresh()
    {
        var skeleton = new StackPanel { Spacing = 10 };
        skeleton.Children.Add(MavSkeleton.Text(220));
        skeleton.Children.Add(MavSkeleton.Text(160));
        skeleton.Children.Add(MavSkeleton.Circle(48));
        skeleton.Children.Add(MavSkeleton.ListRow());

        var refreshBody = new StackPanel { Spacing = 8 };
        for (var i = 1; i <= 5; i++)
        {
            refreshBody.Children.Add(new MavListItem(
                MavLocalization.Format("Feedback.Refresh.Row", "Refreshable row {0}", i),
                MavLocalization.Get("Feedback.Refresh.Hint", "Pull down to refresh"),
                MavIcons.Refresh));
        }

        var refresh = new MavRefreshIndicator
        {
            Height = 220,
            Body = new MavScrollViewer { Content = refreshBody },
            RefreshHandler = async () => await Task.Delay(900),
        };

        var row = new AdaptiveWrap { MinItemWidth = 300 };
        row.Children.Add(new MavCard(MavCardVariant.Outlined, skeleton) { Margin = new Thickness(0, 0, 8, 8) });
        row.Children.Add(new MavCard(MavCardVariant.Outlined, refresh) { Margin = new Thickness(0, 0, 8, 8) });
        return row;
    }

    private static Control BuildDataWidgets()
    {
        var gauge = new MavRadialGauge
        {
            Value = 68,
            Minimum = 0,
            Maximum = 100,
            Unit = "%",
            IsInteractive = true,
            Width = 220,
            Height = 220,
        };

        var line = new MavSparkline { Kind = MavSparklineKind.Line, ValuesText = "3,7,4,9,12,8,15,11,17" };
        var bar = new MavSparkline { Kind = MavSparklineKind.Bar, ValuesText = "5,9,3,12,7,10" };
        var donut = new MavSparkline { Kind = MavSparklineKind.Donut, ValuesText = "70", CenterLabel = "70%" };

        var row = new AdaptiveWrap { MinItemWidth = 300 };
        row.Children.Add(new MavCard(MavCardVariant.Outlined, gauge) { Margin = new Thickness(0, 0, 8, 8) });
        row.Children.Add(new MavCard(MavCardVariant.Outlined, Column(line, bar, donut)) { Margin = new Thickness(0, 0, 8, 8) });
        return row;
    }
}
