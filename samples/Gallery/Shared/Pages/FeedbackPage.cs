using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Theming;

namespace Md3.Gallery;

internal static class FeedbackPage
{
    public static Control Build()
    {
        return GalleryUi.Page(
            GalleryUi.Section("Dialogs and sheets", "Modal surfaces contain their own actions and close on scrim tap or Escape/Back.", ModalSurfaces()),
            GalleryUi.Section("Messages and status", "Snackbar, banners, info bars and an empty state.", Messages()),
            GalleryUi.Section("Progress", "Linear, circular, segmented, wavy and contained loading indicators.", Progress()),
            GalleryUi.Section("Placeholders and refresh", "Skeletons and pull-to-refresh.", SkeletonAndRefresh()),
            GalleryUi.Section("Data widgets", "Radial gauge and sparklines.", DataWidgets()));
    }

    private static Control ModalSurfaces()
    {
        var dialog = new MdDialog
        {
            Icon = MdIcons.Delete,
            Title = "Delete project?",
            Message = "This action cannot be undone.",
            ConfirmText = "Delete",
            DismissText = "Cancel",
            NeutralText = "Archive",
        };

        var sheet = new MdBottomSheet
        {
            Title = "Filters",
            Body = new MdLabel { Text = "Sheet content: filters, choices, actions." },
            ShowDragHandle = true,
        };

        var side = new MdSideSheet
        {
            Title = "Details",
            Body = new MdLabel { Text = "Side sheet content on tablet and desktop." },
            Placement = MdSideSheetPlacement.End,
        };

        var dialogButton = new MdButton("Open dialog", MdButtonVariant.Filled, MdIcons.Chat);
        dialogButton.Clicked += (_, _) => dialog.Show(dialogButton);

        var sheetButton = new MdButton("Open bottom sheet", MdButtonVariant.Tonal, MdIcons.BottomPanelOpen);
        sheetButton.Clicked += (_, _) => sheet.Show(sheetButton);

        var sideButton = new MdButton("Open side sheet", MdButtonVariant.Outlined, MdIcons.SideNavigation);
        sideButton.Clicked += (_, _) => side.Show(sideButton);

        return GalleryUi.Showcase(
            "MdDialog, MdBottomSheet and MdSideSheet",
            "All three are overlay handles: create once, then Show(anchor). The dialog is adaptive on phones.",
            GalleryUi.Row(dialogButton, sheetButton, sideButton),
            "new MdDialog { ... }.Show(anchor) · MdBottomSheet.Show(anchor) · MdSideSheet.Placement");
    }

    private static Control Messages()
    {
        var snack = new MdButton("Show snackbar", MdButtonVariant.Filled, MdIcons.Notifications);
        snack.Clicked += (_, _) => MdSnackbar.Show("Item deleted", "Undo", () => { }, duration: MdSnackbarDuration.Long);

        var queue = new MdButton("Queue snackbars", MdButtonVariant.Tonal, MdIcons.PlaylistAdd);
        queue.Clicked += (_, _) =>
        {
            MdSnackbar.Enqueue("Uploading photo…", duration: MdSnackbarDuration.Short);
            MdSnackbar.Enqueue("Upload complete", "Undo", () => { }, duration: MdSnackbarDuration.Long);
        };

        var dismiss = new MdButton("Dismiss snackbar", MdButtonVariant.Text, MdIcons.Close);
        dismiss.Clicked += (_, _) => MdSnackbar.Dismiss();

        var indefinite = new MdButton("Indefinite snackbar", MdButtonVariant.Outlined, MdIcons.HourglassBottom);
        indefinite.Clicked += (_, _) => MdSnackbar.Show(
            "Indefinite snackbar — dismiss with ×",
            duration: MdSnackbarDuration.Indefinite);

        var banner = new MdBanner
        {
            Message = "A new version of Md3.Avalonia is available.",
            PrimaryActionText = "Update",
            SecondaryActionText = "Later",
        };

        var infoBars = GalleryUi.Column(
            new MdInfoBar { Severity = MdInfoBarSeverity.Info, Title = "Info", Message = "Neutral information." },
            new MdInfoBar { Severity = MdInfoBarSeverity.Success, Title = "Success", Message = "Everything is up to date." },
            new MdInfoBar { Severity = MdInfoBarSeverity.Warning, Title = "Warning", Message = "Storage is almost full." },
            new MdInfoBar { Severity = MdInfoBarSeverity.Error, Title = "Error", Message = "Could not reach the server.", IsClosable = true });

        var empty = new MdEmptyState
        {
            Title = "No projects yet",
            Description = "Create a project or import one from a template.",
            Icon = MdIcons.FolderOpen,
            ActionText = "Create project",
        };

        return GalleryUi.Showcase(
            "MdSnackbar, MdBanner, MdInfoBar and MdEmptyState",
            "Global snackbar (FIFO), inline banners, four info-bar severities and an empty-state placeholder.",
            GalleryUi.Column(
                GalleryUi.Row(snack, queue, dismiss, indefinite),
                banner,
                infoBars,
                new MdCard(MdCardVariant.Outlined, empty)),
            "MdSnackbar.Show/Enqueue · MdBanner.PrimaryActionText · MdInfoBar.Severity · MdEmptyState.Icon");
    }

    private static Control Progress()
    {
        var linear = new MdLinearProgress { Value = 0.62 };
        var indeterminate = new MdLinearProgress { IsIndeterminate = true };
        var wavy = new MdWavyProgressIndicator { Value = 0.45 };
        var circular = new MdCircularProgress(48, 4) { Value = 0.7 };
        var spinner = new MdCircularProgress(28, 3) { IsIndeterminate = true };
        var segmented = new MdSegmentedProgressBar { Value = 0.6, SegmentCount = 6 };
        var loading = new MdLoadingIndicator { IsContained = true };

        return GalleryUi.Showcase(
            "MdLinearProgress, MdCircularProgress, MdSegmentedProgressBar, MdWavyProgressIndicator and MdLoadingIndicator",
            "Determinate and indeterminate progress for inline and contained loading states.",
            GalleryUi.Column(
                linear,
                indeterminate,
                wavy,
                GalleryUi.Row(circular, spinner, loading),
                segmented),
            "MdLinearProgress.Value/IsIndeterminate · MdSegmentedProgressBar.SegmentCount · MdLoadingIndicator.IsContained");
    }

    private static Control SkeletonAndRefresh()
    {
        var skeleton = new StackPanel { Spacing = 10 };
        skeleton.Children.Add(MdSkeleton.Text(220));
        skeleton.Children.Add(MdSkeleton.Text(160));
        skeleton.Children.Add(MdSkeleton.Circle(48));
        skeleton.Children.Add(MdSkeleton.ListRow());

        var refreshBody = new StackPanel { Spacing = 8 };
        for (var i = 1; i <= 5; i++)
            refreshBody.Children.Add(new MdListItem($"Refreshable row {i}", "Pull down to refresh", MdIcons.Refresh));

        var refresh = new MdRefreshIndicator
        {
            Height = 220,
            Body = new MdScrollViewer { Content = refreshBody },
            RefreshHandler = async () => await Task.Delay(900),
        };

        return GalleryUi.Showcase(
            "MdSkeleton and MdRefreshIndicator",
            "Skeletons cover text, circles and list rows; the refresh indicator hosts any scrollable body.",
            GalleryUi.Row(
                new MdCard(MdCardVariant.Outlined, skeleton) { Width = 320 },
                new MdCard(MdCardVariant.Outlined, refresh) { Width = 420 }),
            "MdSkeleton.Text/Circle/ListRow · MdRefreshIndicator.Body/RefreshHandler");
    }

    private static Control DataWidgets()
    {
        var gauge = new MdRadialGauge
        {
            Value = 68,
            Minimum = 0,
            Maximum = 100,
            Unit = "%",
            IsInteractive = true,
            Width = 220,
            Height = 220,
        };

        var line = new MdSparkline { Kind = MdSparklineKind.Line, ValuesText = "3,7,4,9,12,8,15,11,17" };
        var bar = new MdSparkline { Kind = MdSparklineKind.Bar, ValuesText = "5,9,3,12,7,10" };
        var donut = new MdSparkline { Kind = MdSparklineKind.Donut, ValuesText = "70", CenterLabel = "70%" };

        return GalleryUi.Showcase(
            "MdRadialGauge and MdSparkline",
            "Interactive radial gauge and lightweight line/bar/donut charts.",
            GalleryUi.Row(
                new MdCard(MdCardVariant.Outlined, gauge) { Width = 260 },
                new MdCard(MdCardVariant.Outlined, GalleryUi.Column(line, bar, donut)) { Width = 360 }),
            "MdRadialGauge.Value/IsInteractive · MdSparkline.Kind/ValuesText");
    }
}
