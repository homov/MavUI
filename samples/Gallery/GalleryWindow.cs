using Md3.Avalonia.Controls;

namespace Md3.Gallery;

public sealed class GalleryWindow : MdWindow
{
    public GalleryWindow()
    {
        Title = "Md3 Control Gallery";
        Width = 1320;
        Height = 900;
        MinWidth = 360;
        MinHeight = 480;
        Content = GalleryShell.Create();
    }
}
