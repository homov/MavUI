using Avalonia;
using Avalonia.Controls;
using Mav;
using Mav.Controls;

namespace Mav.ControlsGallery.Pages;

public partial class FeedbackPage : UserControl
{
    /// <summary>Shows a snackbar anchored to the button that sent the command (CommandParameter).</summary>
    public static readonly GalleryCommand ShowSnackbar = new(p => MavSnackbar.Show(
        MavLocalization.Get("Feedback.Snack.Deleted", "Item deleted"),
        MavLocalization.Get("Feedback.Snack.Undo", "Undo"),
        () => { },
        duration: MavSnackbarDuration.Long,
        anchor: (Visual)p!));

    /// <summary>Queues two snackbars anchored to the button that sent the command.</summary>
    public static readonly GalleryCommand QueueSnackbars = new(p =>
    {
        var anchor = (Visual)p!;
        MavSnackbar.Enqueue(MavLocalization.Get("Feedback.Snack.Uploading", "Uploading photo…"),
            duration: MavSnackbarDuration.Short, anchor: anchor);
        MavSnackbar.Enqueue(MavLocalization.Get("Feedback.Snack.Complete", "Upload complete"),
            MavLocalization.Get("Feedback.Snack.Undo", "Undo"), () => { },
            duration: MavSnackbarDuration.Long, anchor: anchor);
    });

    public static readonly GalleryCommand DismissSnackbar = new(_ => MavSnackbar.Dismiss());

    public static readonly GalleryCommand IndefiniteSnackbar = new(p => MavSnackbar.Show(
        MavLocalization.Get("Feedback.Snack.IndefiniteText", "Indefinite snackbar — dismiss with ×"),
        duration: MavSnackbarDuration.Indefinite,
        anchor: (Visual)p!));

    /// <summary>Refresh command; the demo refresh completes immediately (Command cannot await a Task).</summary>
    public static readonly GalleryCommand RefreshRows = new(_ => { });

    public FeedbackPage()
    {
        InitializeComponent();
    }
}
