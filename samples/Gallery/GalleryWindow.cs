using Mav.Controls;

namespace Mav.Gallery;

public sealed class GalleryWindow : MavWindow
{
    public GalleryWindow()
    {
        Title = "Mav Control Gallery";
        Width = 1320;
        Height = 900;
        MinWidth = 360;
        MinHeight = 480;
        Content = GalleryShell.Create();
    }
}
