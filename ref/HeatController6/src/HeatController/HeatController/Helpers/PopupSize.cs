namespace HeatController;

public static class PopupSize
{
    public static Size GetSize()
    {
        var size = Device.Info.ScaledScreenSize;
        var m = Math.Min(size.Width, size.Height) - 40;
        return new Size(m, m);
    }
}
