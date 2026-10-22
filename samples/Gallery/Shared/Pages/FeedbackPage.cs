using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Mav;
using Mav.Controls;
using Mav.Theming;

namespace Mav.Gallery;

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
        var dialog = new MavDialog
        {
            Icon = MavIcons.Delete,
            Title = "Delete project?",
            Message = "This action cannot be undone.",
            ConfirmText = "Delete",
            DismissText = "Cancel",
            NeutralText = "Archive",
        };

        var sheet = new MavBottomSheet
        {
            Title = "Filters",
            Body = new MavLabel { Text = "Sheet content: filters, choices, actions." },
            ShowDragHandle = true,
        };

        var side = new MavSideSheet
        {
            Title = "Details",
            Body = new MavLabel { Text = "Side sheet content on tablet and desktop." },
            Placement = MavSideSheetPlacement.End,
        };

        var dialogButton = new MavButton("Open dialog", MavButtonVariant.Filled, MavIcons.Chat);
        dialogButton.Clicked += (_, _) => dialog.Show(dialogButton);

        var sheetButton = new MavButton("Open bottom sheet", MavButtonVariant.Tonal, MavIcons.BottomPanelOpen);
        sheetButton.Clicked += (_, _) => sheet.Show(sheetButton);

        var sideButton = new MavButton("Open side sheet", MavButtonVariant.Outlined, MavIcons.SideNavigation);
        sideButton.Clicked += (_, _) => side.Show(sideButton);

        return GalleryUi.Showcase(
            "MavDialog, MavBottomSheet and MavSideSheet",
            "All three are overlay handles: create once, then Show(anchor). The dialog is adaptive on phones.",
            GalleryUi.Row(dialogButton, sheetButton, sideButton),
            "new MavDialog { ... }.Show(anchor) · MavBottomSheet.Show(anchor) · MavSideSheet.Placement");
    }

    private static Control Messages()
    {
        var snack = new MavButton("Show snackbar", MavButtonVariant.Filled, MavIcons.Notifications);
        snack.Clicked += (_, _) => MavSnackbar.Show("Item deleted", "Undo", () => { }, duration: MavSnackbarDuration.Long);

        var queue = new MavButton("Queue snackbars", MavButtonVariant.Tonal, MavIcons.PlaylistAdd);
        queue.Clicked += (_, _) =>
        {
            MavSnackbar.Enqueue("Uploading photo…", duration: MavSnackbarDuration.Short);
            MavSnackbar.Enqueue("Upload complete", "Undo", () => { }, duration: MavSnackbarDuration.Long);
        };

        var dismiss = new MavButton("Dismiss snackbar", MavButtonVariant.Text, MavIcons.Close);
        dismiss.Clicked += (_, _) => MavSnackbar.Dismiss();

        var indefinite = new MavButton("Indefinite snackbar", MavButtonVariant.Outlined, MavIcons.HourglassBottom);
        indefinite.Clicked += (_, _) => MavSnackbar.Show(
            "Indefinite snackbar — dismiss with ×",
            duration: MavSnackbarDuration.Indefinite);

        var banner = new MavBanner
        {
            Message = "A new version of Mav is available.",
            PrimaryActionText = "Update",
            SecondaryActionText = "Later",
        };

        var infoBars = GalleryUi.Column(
            new MavInfoBar { Severity = MavInfoBarSeverity.Info, Title = "Info", Message = "Neutral information." },
            new MavInfoBar { Severity = MavInfoBarSeverity.Success, Title = "Success", Message = "Everything is up to date." },
            new MavInfoBar { Severity = MavInfoBarSeverity.Warning, Title = "Warning", Message = "Storage is almost full." },
            new MavInfoBar { Severity = MavInfoBarSeverity.Error, Title = "Error", Message = "Could not reach the server.", IsClosable = true });

        var empty = new MavEmptyState
        {
            Title = "No projects yet",
            Description = "Create a project or import one from a template.",
            Icon = MavIcons.FolderOpen,
            ActionText = "Create project",
        };

        return GalleryUi.Showcase(
            "MavSnackbar, MavBanner, MavInfoBar and MavEmptyState",
            "Global snackbar (FIFO), inline banners, four info-bar severities and an empty-state placeholder.",
            GalleryUi.Column(
                GalleryUi.Row(snack, queue, dismiss, indefinite),
                banner,
                infoBars,
                new MavCard(MavCardVariant.Outlined, empty)),
            "MavSnackbar.Show/Enqueue · MavBanner.PrimaryActionText · MavInfoBar.Severity · MavEmptyState.Icon");
    }

    private static Control Progress()
    {
        var linear = new MavLinearProgress { Value = 0.62 };
        var indeterminate = new MavLinearProgress { IsIndeterminate = true };
        var wavy = new MavWavyProgressIndicator { Value = 0.45 };
        var circular = new MavCircularProgress(48, 4) { Value = 0.7 };
        var spinner = new MavCircularProgress(28, 3) { IsIndeterminate = true };
        var segmented = new MavSegmentedProgressBar { Value = 0.6, SegmentCount = 6 };
        var loading = new MavLoadingIndicator { IsContained = true };

        return GalleryUi.Showcase(
            "MavLinearProgress, MavCircularProgress, MavSegmentedProgressBar, MavWavyProgressIndicator and MavLoadingIndicator",
            "Determinate and indeterminate progress for inline and contained loading states.",
            GalleryUi.Column(
                linear,
                indeterminate,
                wavy,
                GalleryUi.Row(circular, spinner, loading),
                segmented),
            "MavLinearProgress.Value/IsIndeterminate · MavSegmentedProgressBar.SegmentCount · MavLoadingIndicator.IsContained");
    }

    private static Control SkeletonAndRefresh()
    {
        var skeleton = new StackPanel { Spacing = 10 };
        skeleton.Children.Add(MavSkeleton.Text(220));
        skeleton.Children.Add(MavSkeleton.Text(160));
        skeleton.Children.Add(MavSkeleton.Circle(48));
        skeleton.Children.Add(MavSkeleton.ListRow());

        var refreshBody = new StackPanel { Spacing = 8 };
        for (var i = 1; i <= 5; i++)
            refreshBody.Children.Add(new MavListItem($"Refreshable row {i}", "Pull down to refresh", MavIcons.Refresh));

        var refresh = new MavRefreshIndicator
        {
            Height = 220,
            Body = new MavScrollViewer { Content = refreshBody },
            RefreshHandler = async () => await Task.Delay(900),
        };

        return GalleryUi.Showcase(
            "MavSkeleton and MavRefreshIndicator",
            "Skeletons cover text, circles and list rows; the refresh indicator hosts any scrollable body.",
            GalleryUi.Row(
                new MavCard(MavCardVariant.Outlined, skeleton) { Width = 320 },
                new MavCard(MavCardVariant.Outlined, refresh) { Width = 420 }),
            "MavSkeleton.Text/Circle/ListRow · MavRefreshIndicator.Body/RefreshHandler");
    }

    private static Control DataWidgets()
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

        return GalleryUi.Showcase(
            "MavRadialGauge and MavSparkline",
            "Interactive radial gauge and lightweight line/bar/donut charts.",
            GalleryUi.Row(
                new MavCard(MavCardVariant.Outlined, gauge) { Width = 260 },
                new MavCard(MavCardVariant.Outlined, GalleryUi.Column(line, bar, donut)) { Width = 360 }),
            "MavRadialGauge.Value/IsInteractive · MavSparkline.Kind/ValuesText");
    }
}
