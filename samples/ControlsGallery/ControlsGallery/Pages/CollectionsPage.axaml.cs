using Avalonia.Controls;
using Mav.Controls;

namespace Mav.ControlsGallery.Pages;

public partial class CollectionsPage : UserControl
{
    /// <summary>Swipe command; the label to update arrives as the command parameter.</summary>
    public static readonly GalleryCommand ArchiveSwipe =
        new(p => ((MavLabel)p!).Text = "Leading action triggered: Archive");

    /// <summary>Swipe command; the label to update arrives as the command parameter.</summary>
    public static readonly GalleryCommand DeleteSwipe =
        new(p => ((MavLabel)p!).Text = "Trailing action triggered: Delete");

    public CollectionsPage()
    {
        InitializeComponent();
    }
}
