using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Mav.Theming;

/// <summary>
/// Material Design 3 Shared-Axis X page transition for <see cref="NavigationPage"/>.
/// Uses wall-clock <see cref="MavMotion"/> timing so transitions never stall on Android
/// and always restore presenter transforms and visibility upon completion.
/// </summary>
public sealed class MavPageSlide : PageSlide
{
    /// <summary>Creates a horizontal page slide transition.</summary>
    public MavPageSlide() : base(TimeSpan.FromMilliseconds(MavMotion.Short4), SlideAxis.Horizontal)
    {
    }

    /// <summary>Runs the slide transition between <paramref name="from"/> and <paramref name="to"/>.</summary>
    public override async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        var parent = (from ?? to)?.GetVisualParent();
        var width = parent is not null && parent.Bounds.Width > 0 ? parent.Bounds.Width : 360.0;
        var slideOffset = Math.Min(96.0, width * 0.22);
        var dir = forward ? 1.0 : -1.0;

        var fromTransform = new TranslateTransform(0, 0);
        var toTransform = new TranslateTransform(dir * slideOffset, 0);

        if (from is not null)
        {
            from.RenderTransform = fromTransform;
            from.Opacity = 1.0;
        }

        if (to is not null)
        {
            to.IsVisible = true;
            to.RenderTransform = toTransform;
            to.Opacity = 0.0;
        }

        var anchor = to ?? from;
        if (anchor is null)
            return;

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var reg = cancellationToken.Register(() =>
        {
            MavMotion.Cancel(anchor, "md_page_slide");
            tcs.TrySetResult(false);
        });

        try
        {
            MavMotion.Animate(anchor, "md_page_slide", 0.0, 1.0, MavMotion.Short4, MavMotion.CubicOut, p =>
            {
                toTransform.X = dir * slideOffset * (1.0 - p);
                if (to is not null)
                    to.Opacity = Math.Clamp(p * 1.35, 0.0, 1.0);

                fromTransform.X = -dir * (slideOffset * 0.45) * p;
                if (from is not null)
                    from.Opacity = Math.Clamp(1.0 - p * 1.4, 0.0, 1.0);
            }, () => tcs.TrySetResult(true));

            await tcs.Task;
        }
        finally
        {
            if (to is not null)
            {
                to.RenderTransform = null;
                to.Opacity = 1.0;
                to.IsVisible = true;
            }

            if (from is not null)
            {
                from.RenderTransform = null;
                from.Opacity = 1.0;
                if (!cancellationToken.IsCancellationRequested)
                    from.IsVisible = false;
            }
        }
    }
}
