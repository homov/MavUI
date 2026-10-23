using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Mav;
using Mav.Controls;
using Mav.Theming;
using static Mav.ControlsGalleryMarkup.Pages.PageParts;

namespace Mav.ControlsGalleryMarkup.Pages;

/// <summary>Dialogs, sheets, snackbars, banners, progress, placeholders and data widgets. Mirrors the AXAML Feedback page.</summary>
public static class FeedbackPage
{
    public static Control Build() =>
        Page(
            Section(
                "Feedback.Dialogs.Title", "Dialogs and sheets",
                "Feedback.Dialogs.Body", "Modal surfaces contain their own actions and close on scrim tap or Escape/Back.",
                Card(
                    "MavDialog, MavBottomSheet and MavSideSheet",
                    "Feedback.Dialogs.CardBody", "All three are overlay handles: create once, then Show(anchor). The dialog is adaptive on phones.",
                    "new MavDialog { ... }.Show(anchor) · MavBottomSheet.Show(anchor) · MavSideSheet.Placement",
                    Row(
                        WithDialog(
                            new MavButton(L("Feedback.Open.Dialog", "Open dialog"), MavButtonVariant.Filled, MavIcons.Chat),
                            new MavDialog()
                                .Icon(MavIcons.Delete)
                                .Title(L("Feedback.Dialog.Title", "Delete project?"))
                                .Message(L("Feedback.Dialog.Message", "This action cannot be undone."))
                                .ConfirmText(L("Feedback.Dialog.Delete", "Delete"))
                                .DismissText(L("Feedback.Dialog.Cancel", "Cancel"))
                                .NeutralText(L("Feedback.Dialog.Archive", "Archive"))),
                        WithBottomSheet(
                            new MavButton(L("Feedback.Open.Sheet", "Open bottom sheet"), MavButtonVariant.Tonal, MavIcons.BottomPanelOpen),
                            new MavBottomSheet()
                                .Title(L("Feedback.Sheet.Title", "Filters"))
                                .ShowDragHandle(true)
                                .Content(new MavLabel(L("Feedback.Sheet.Body", "Sheet content: filters, choices, actions.")))),
                        WithSideSheet(
                            new MavButton(L("Feedback.Open.Side", "Open side sheet"), MavButtonVariant.Outlined, MavIcons.SideNavigation),
                            new MavSideSheet()
                                .Title(L("Feedback.Side.Title", "Details"))
                                .Placement(MavSideSheetPlacement.End)
                                .Content(new MavLabel(L("Feedback.Side.Body", "Side sheet content on tablet and desktop.")))))))),
            Section(
                "Feedback.Messages.Title", "Messages and status",
                "Feedback.Messages.Body", "Snackbars, banners, info bars and an empty state.",
                Card(
                    "MavSnackbar, MavBanner, MavInfoBar and MavEmptyState",
                    "Feedback.Messages.CardBody", "Global snackbar (FIFO), inline banners, four info-bar severities and an empty-state placeholder.",
                    "MavSnackbar.Show/Enqueue · MavBanner.PrimaryActionText · MavInfoBar.Severity · MavEmptyState.Icon",
                    Stack(8,
                        Row(Snackbars()),
                        new MavBanner(L("Feedback.Banner.Message", "A new version of Mav is available."), MavIcons.Info,
                            L("Feedback.Banner.Update", "Update"), L("Feedback.Banner.Later", "Later")),
                        Stack(8,
                            new MavInfoBar(L("Feedback.Info.Title", "Info"), L("Feedback.Info.Text", "Neutral information."), MavInfoBarSeverity.Info),
                            new MavInfoBar(L("Feedback.Success.Title", "Success"), L("Feedback.Success.Text", "Everything is up to date."), MavInfoBarSeverity.Success),
                            new MavInfoBar(L("Feedback.Warning.Title", "Warning"), L("Feedback.Warning.Text", "Storage is almost full."), MavInfoBarSeverity.Warning),
                            new MavInfoBar(L("Feedback.Error.Title", "Error"), L("Feedback.Error.Text", "Could not reach the server."), MavInfoBarSeverity.Error, true)),
                        Outlined(new MavEmptyState(
                            L("Feedback.Empty.Title", "No projects yet"),
                            L("Feedback.Empty.Description", "Create a project or import one from a template."),
                            MavIcons.FolderOpen,
                            L("Feedback.Empty.Action", "Create project")))))),
            Section(
                "Feedback.Progress.Title", "Progress",
                "Feedback.Progress.Body", "Linear, circular, segmented, wavy and contained loading indicators.",
                Card(
                    "MavLinearProgress, MavCircularProgress, MavSegmentedProgressBar, MavWavyProgressIndicator and MavLoadingIndicator",
                    "Feedback.Progress.CardBody", "Determinate and indeterminate progress for inline and contained loading states.",
                    "MavLinearProgress.Value/IsIndeterminate · MavSegmentedProgressBar.SegmentCount · MavLoadingIndicator.IsContained",
                    Stack(8,
                        new MavLinearProgress(0.62),
                        new MavLinearProgress(0.5, true),
                        new MavWavyProgressIndicator(0.45),
                        Row(
                            new MavCircularProgress(48, false, 0.7).StrokeWidth(4),
                            new MavCircularProgress(28, true).StrokeWidth(3),
                            new MavLoadingIndicator(48, true)),
                        new MavSegmentedProgressBar(6, 0.6)))),
            Section(
                "Feedback.Placeholders.Title", "Placeholders and refresh",
                "Feedback.Placeholders.Body", "Skeletons and pull-to-refresh.",
                Card(
                    "MavSkeleton and MavRefreshIndicator",
                    "Feedback.Skeleton.CardBody", "Skeletons cover text, circles and list rows; the refresh indicator hosts any scrollable body.",
                    "MavSkeleton.Text/Circle/ListRow · MavRefreshIndicator.Body/RefreshHandler",
                    Adaptive(300,
                        Outlined(Stack(10,
                                new MavSkeleton(220, 14),
                                new MavSkeleton(160, 14),
                                new MavSkeleton(48, 48, 24),
                                SkeletonRow()))
                            .Margin(new Thickness(0, 0, 8, 8)),
                        Outlined(RefreshDemo())
                            .Margin(new Thickness(0, 0, 8, 8))))),
            Section(
                "Feedback.Data.Title", "Data widgets",
                "Feedback.Data.Body", "Radial gauge and sparklines.",
                Card(
                    "MavRadialGauge and MavSparkline",
                    "Feedback.Data.CardBody", "Interactive radial gauge and lightweight line/bar/donut charts.",
                    "MavRadialGauge.Value/IsInteractive · MavSparkline.Kind/ValuesText",
                    Adaptive(300,
                        Outlined(new MavRadialGauge()
                                .Value(68)
                                .Minimum(0)
                                .Maximum(100)
                                .Unit("%")
                                .IsInteractive(true)
                                .Width(220)
                                .Height(220))
                            .Margin(new Thickness(0, 0, 8, 8)),
                        Outlined(Stack(8,
                                new MavSparkline(new double[] { 3, 7, 4, 9, 12, 8, 15, 11, 17 }, MavSparklineKind.Line),
                                new MavSparkline(new double[] { 5, 9, 3, 12, 7, 10 }, MavSparklineKind.Bar),
                                new MavSparkline(new double[] { 70 }, MavSparklineKind.Donut, "70%")))
                            .Margin(new Thickness(0, 0, 8, 8)))));

    /// <summary>Snackbar buttons. Each one anchors its snackbar to itself, as the AXAML version did with CommandParameter.</summary>
    private static Control[] Snackbars()
    {
        var show = new MavButton(L("Feedback.Snack.Show", "Show snackbar"), MavButtonVariant.Filled, MavIcons.Notifications);
        show.OnClicked(() => MavSnackbar.Show(
            L("Feedback.Snack.Deleted", "Item deleted"),
            L("Feedback.Snack.Undo", "Undo"),
            () => { },
            duration: MavSnackbarDuration.Long,
            anchor: show));

        var queue = new MavButton(L("Feedback.Snack.Queue", "Queue snackbars"), MavButtonVariant.Tonal, MavIcons.PlaylistAdd);
        queue.OnClicked(() =>
        {
            MavSnackbar.Enqueue(L("Feedback.Snack.Uploading", "Uploading photo…"),
                duration: MavSnackbarDuration.Short, anchor: queue);
            MavSnackbar.Enqueue(L("Feedback.Snack.Complete", "Upload complete"),
                L("Feedback.Snack.Undo", "Undo"), () => { },
                duration: MavSnackbarDuration.Long, anchor: queue);
        });

        var dismiss = new MavButton(L("Feedback.Snack.Dismiss", "Dismiss snackbar"), MavButtonVariant.Text, MavIcons.Close);
        dismiss.OnClicked(() => MavSnackbar.Dismiss());

        var indefinite = new MavButton(L("Feedback.Snack.Indefinite", "Indefinite snackbar"), MavButtonVariant.Outlined, MavIcons.HourglassBottom);
        indefinite.OnClicked(() => MavSnackbar.Show(
            L("Feedback.Snack.IndefiniteText", "Indefinite snackbar — dismiss with ×"),
            duration: MavSnackbarDuration.Indefinite,
            anchor: indefinite));

        return [show, queue, dismiss, indefinite];
    }

    private static Control SkeletonRow() =>
        new Grid().Cols("Auto, *").Children(
            new MavSkeleton(40, 40, 20).Margin(new Thickness(0, 0, 12, 0)),
            Stack(8,
                    new MavSkeleton(180, 14),
                    new MavSkeleton(120, 14))
                .GridColumn(1)
                .VerticalAlignment(VerticalAlignment.Center));

    private static Control RefreshDemo()
    {
        var rows = Enumerable.Range(1, 5)
            .Select(i => (Control)new MavListItem(
                L($"Feedback.Refresh.Row{i}", $"Refreshable row {i}"),
                L("Feedback.Refresh.Hint", "Pull down to refresh"),
                MavIcons.Refresh))
            .ToArray();

        return new MavRefreshIndicator(new MavScrollViewer().Content(Stack(8, rows)))
            .Height(220)
            .Command(new MavAsyncCommand(() => Task.Delay(900)));
    }

    private static MavButton WithDialog(MavButton button, MavDialog dialog)
    {
        MavFlyout.SetDialog(button, dialog);
        return button;
    }

    private static MavButton WithBottomSheet(MavButton button, MavBottomSheet sheet)
    {
        MavFlyout.SetBottomSheet(button, sheet);
        return button;
    }

    private static MavButton WithSideSheet(MavButton button, MavSideSheet sheet)
    {
        MavFlyout.SetSideSheet(button, sheet);
        return button;
    }

    private static StackPanel Stack(double spacing, params Control[] items) =>
        new StackPanel().Spacing(spacing).Children(items);

    private static MavCard Outlined(Control content) =>
        new MavCard(MavCardVariant.Outlined, content);
}
