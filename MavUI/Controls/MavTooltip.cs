using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

public enum MavTooltipPlacement
{
    Top,
    Bottom,
}

/// <summary>
/// Material Design 3 Plain Tooltip (24dp minimum height, 4dp radius, InverseSurface container).
/// </summary>
public static class MavTooltip
{
    public static readonly AttachedProperty<string?> TextProperty =
        AvaloniaProperty.RegisterAttached<Control, string?>("Text", typeof(MavTooltip));

    public static readonly AttachedProperty<MavTooltipPlacement> PlacementProperty =
        AvaloniaProperty.RegisterAttached<Control, MavTooltipPlacement>("Placement", typeof(MavTooltip), MavTooltipPlacement.Top);

    private static readonly AttachedProperty<bool> IsHookedProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsHooked", typeof(MavTooltip));

    private static IDisposable? _handle;
    private static DispatcherTimer? _timer;

    static MavTooltip()
    {
        TextProperty.Changed.AddClassHandler<Control>((ctrl, _) => EnsureHooked(ctrl));
    }

    public static string? GetText(Control element) => element.GetValue(TextProperty);

    public static void SetText(Control element, string? value) => element.SetValue(TextProperty, value);

    public static MavTooltipPlacement GetPlacement(Control element) => element.GetValue(PlacementProperty);

    public static void SetPlacement(Control element, MavTooltipPlacement value) => element.SetValue(PlacementProperty, value);

    public static void Attach(
        Control target,
        string text,
        MavTooltipPlacement placement = MavTooltipPlacement.Top)
    {
        target.SetValue(PlacementProperty, placement);
        target.SetValue(TextProperty, text);
    }

    public static void SetText(
        Control target,
        string text,
        MavTooltipPlacement placement) =>
        Attach(target, text, placement);

    private static void EnsureHooked(Control target)
    {
        if (target.GetValue(IsHookedProperty)) return;

        // Keep the marker and handlers on the target itself. A static registry would
        // keep every tooltip owner alive for the lifetime of the process.
        target.SetValue(IsHookedProperty, true);
        target.PointerEntered += (_, _) =>
        {
            var txt = GetText(target);
            if (!string.IsNullOrEmpty(txt))
                Show(target, txt, GetPlacement(target), durationMs: 2200);
        };
        target.PointerExited += (_, _) => Dismiss();
    }

    public static void Show(
        Control anchor,
        string text,
        MavTooltipPlacement placement = MavTooltipPlacement.Top,
        int durationMs = 2000)
    {
        Dismiss();

        var overlayRoot = MavOverlay.EnsureOverlayRoot(anchor);
        if (overlayRoot is null) return;

        var t = MavTheme.Current;
        var c = t.Color;

        var lbl = new TextBlock
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
        };
        lbl.Apply(MavTypeRole.BodySmall, t.Typography);
        lbl.Foreground = MavTheme.BrushOf(c.InverseOnSurface);

        var w = Math.Max(40, MavTextMetrics.MeasureWidth(text, 12) + 16);
        var pill = new MavSurface
        {
            MinHeight = 24,
            Radius = 4,
            Padding = new Thickness(8, 4),
            FillColor = c.InverseSurface,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            IsHitTestVisible = false,
            Content = lbl,
        };

        void Place()
        {
            var currentBounds = MavOverlay.BoundsInOverlay(anchor, overlayRoot);
            var currentSize = MavOverlay.GetOverlaySize(anchor);
            var insets = MavOverlay.SafeAreaOf(anchor);

            var x = Math.Clamp(
                currentBounds.Center.X - w / 2.0,
                insets.Left + 8,
                Math.Max(insets.Left + 8, currentSize.Width - insets.Right - w - 8));
            var y = placement == MavTooltipPlacement.Top
                ? Math.Max(insets.Top + 8, currentBounds.Y - 32)
                : Math.Min(
                    currentSize.Height - insets.Bottom - 36,
                    currentBounds.Bottom + 6);

            pill.Margin = new Thickness(x, y, 0, 0);
        }

        Place();
        IDisposable? handle = null;
        handle = MavOverlay.Show(
            anchor,
            pill,
            passThroughScrim: true,
            onViewportChanged: Place,
            onClosed: () => OnOverlayClosed(handle));
        _handle = handle;
        MavMotion.FadeIn(pill, MavMotion.Short3);

        if (durationMs > 0)
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
            _timer.Tick += (_, _) => Dismiss();
            _timer.Start();
        }
    }

    public static void Dismiss()
    {
        StopTimer();

        // Null the field before disposing so the onClosed callback does not
        // mistake an explicit Dismiss for an external overlay close.
        var handle = _handle;
        _handle = null;
        handle?.Dispose();
    }

    private static void OnOverlayClosed(IDisposable? handle)
    {
        if (handle is null || !ReferenceEquals(_handle, handle))
            return;

        _handle = null;
        StopTimer();
    }

    private static void StopTimer()
    {
        if (_timer is null)
            return;

        _timer.Stop();
        _timer = null;
    }
}
