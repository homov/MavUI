using System.Collections;
using System.Globalization;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Drawing style of a <see cref="MavSparkline"/>: line or bars.</summary>
public enum MavSparklineKind
{
    Line,
    Bar,
    Donut,
}

/// <summary>
/// Material Design 3 mini chart / sparkline supporting Line (with area fill), Bar, and Donut modes.
/// </summary>
public class MavSparkline : Control
{
    private readonly List<MavArcCache> _donutArcs = [];

    public static readonly StyledProperty<MavSparklineKind> KindProperty =
        AvaloniaProperty.Register<MavSparkline, MavSparklineKind>(nameof(Kind), MavSparklineKind.Line);

    public static readonly StyledProperty<IReadOnlyList<double>?> ValuesProperty =
        AvaloniaProperty.Register<MavSparkline, IReadOnlyList<double>?>(nameof(Values));

    public static readonly StyledProperty<string?> ValuesTextProperty =
        AvaloniaProperty.Register<MavSparkline, string?>(nameof(ValuesText));

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavSparkline, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<Color?> AccentColorProperty =
        AvaloniaProperty.Register<MavSparkline, Color?>(nameof(AccentColor));

    public static readonly StyledProperty<string?> CenterLabelProperty =
        AvaloniaProperty.Register<MavSparkline, string?>(nameof(CenterLabel));

    static MavSparkline()
    {
        AffectsRender<MavSparkline>(KindProperty, ValuesProperty, AccentColorProperty, CenterLabelProperty);
        KindProperty.Changed.AddClassHandler<MavSparkline>((s, e) =>
        {
            var k = e.GetNewValue<MavSparklineKind>();
            s.MinWidth = k == MavSparklineKind.Donut ? 76 : 120;
            s.MinHeight = k == MavSparklineKind.Donut ? 76 : 48;
        });
        ValuesTextProperty.Changed.AddClassHandler<MavSparkline>((s, e) =>
        {
            var txt = e.GetNewValue<string?>();
            if (string.IsNullOrWhiteSpace(txt)) return;
            var list = new List<double>();
            foreach (var part in txt.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries))
            {
                if (double.TryParse(part.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    list.Add(d);
            }
            if (list.Count > 0)
                s.Values = list;
        });
        ItemsSourceProperty.Changed.AddClassHandler<MavSparkline>((s, e) =>
        {
            var src = e.GetNewValue<IEnumerable?>();
            if (src is null) return;
            var list = new List<double>();
            foreach (var item in src)
            {
                if (item is IConvertible conv)
                {
                    try { list.Add(conv.ToDouble(CultureInfo.InvariantCulture)); } catch { }
                }
            }
            if (list.Count > 0)
                s.Values = list;
        });
    }

    /// <summary>Creates an empty line sparkline.</summary>
    public MavSparkline() : this(null, MavSparklineKind.Line, null)
    {
    }

    /// <summary>Creates a sparkline with the given values, kind and color.</summary>
    public MavSparkline(
        IReadOnlyList<double>? values = null,
        MavSparklineKind kind = MavSparklineKind.Line,
        string? centerLabel = null)
    {
        Kind = kind;
        Values = values ?? [18, 32, 24, 46, 38, 62, 54, 78];
        CenterLabel = centerLabel;
        MinWidth = kind == MavSparklineKind.Donut ? 76 : 120;
        MinHeight = kind == MavSparklineKind.Donut ? 76 : 48;
        MavTheme.Observe(this, (self, _) => self.InvalidateVisual());
    }

    public MavSparklineKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public IReadOnlyList<double>? Values
    {
        get => GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public string? ValuesText
    {
        get => GetValue(ValuesTextProperty);
        set => SetValue(ValuesTextProperty, value);
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public Color? AccentColor
    {
        get => GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    public string? CenterLabel
    {
        get => GetValue(CenterLabelProperty);
        set => SetValue(CenterLabelProperty, value);
    }

    /// <summary>Draws the sparkline for the current values.</summary>
    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        var vals = Values;
        if (w <= 4 || h <= 4 || vals is null || vals.Count == 0) return;

        var t = MavTheme.Current;
        var c = t.Color;
        var accent = AccentColor ?? c.Primary;

        switch (Kind)
        {
            case MavSparklineKind.Line:
                RenderLine(context, w, h, vals, accent);
                break;
            case MavSparklineKind.Bar:
                RenderBar(context, w, h, vals, accent, c);
                break;
            case MavSparklineKind.Donut:
                RenderDonut(context, w, h, vals, t);
                break;
        }
    }

    private static void RenderLine(DrawingContext ctx, double w, double h, IReadOnlyList<double> vals, Color accent)
    {
        if (vals.Count == 1)
        {
            ctx.DrawEllipse(MavTheme.BrushOf(accent), null, new Point(w / 2, h / 2), 4, 4);
            return;
        }

        var min = vals.Min();
        var max = vals.Max();
        var span = Math.Max(0.0001, max - min);
        const double padX = 6;
        const double padY = 6;
        var plotW = Math.Max(4, w - padX * 2);
        var plotH = Math.Max(4, h - padY * 2);

        var pts = new Point[vals.Count];
        for (int i = 0; i < vals.Count; i++)
        {
            var x = padX + (i / (double)(vals.Count - 1)) * plotW;
            var norm = (vals[i] - min) / span;
            var y = padY + (1.0 - norm) * plotH;
            pts[i] = new Point(x, y);
        }

        var areaGeo = new StreamGeometry();
        using (var gc = areaGeo.Open())
        {
            gc.BeginFigure(new Point(pts[0].X, h - padY), true);
            foreach (var p in pts)
                gc.LineTo(p);
            gc.LineTo(new Point(pts[^1].X, h - padY));
            gc.EndFigure(true);
        }
        ctx.DrawGeometry(MavTheme.BrushOf(accent.WithAlpha(0.18f)), null, areaGeo);

        var lineGeo = new StreamGeometry();
        using (var gc = lineGeo.Open())
        {
            gc.BeginFigure(pts[0], false);
            for (int i = 1; i < pts.Length; i++)
                gc.LineTo(pts[i]);
            gc.EndFigure(false);
        }
        ctx.DrawGeometry(null, MavTheme.PenOf(accent, 2.5, PenLineCap.Round, PenLineJoin.Round), lineGeo);

        var last = pts[^1];
        ctx.DrawEllipse(MavTheme.BrushOf(accent), null, last, 4, 4);
    }

    private static void RenderBar(DrawingContext ctx, double w, double h, IReadOnlyList<double> vals, Color accent, MavColorScheme c)
    {
        var max = Math.Max(0.0001, vals.Max());
        const double gap = 4.0;
        var count = vals.Count;
        var barW = Math.Max(3.0, (w - gap * (count - 1)) / count);
        var maxIdx = 0;
        for (int i = 1; i < count; i++)
        {
            if (vals[i] >= vals[maxIdx]) maxIdx = i;
        }

        for (int i = 0; i < count; i++)
        {
            var norm = Math.Clamp(vals[i] / max, 0.08, 1.0);
            var bh = Math.Max(4.0, norm * (h - 4));
            var bx = i * (barW + gap);
            var by = h - bh;
            var col = i == maxIdx ? accent : accent.WithAlpha(0.42f);
            var radius = Math.Min(4.0, barW / 2.0);
            ctx.DrawRectangle(
                MavTheme.BrushOf(col),
                null,
                new RoundedRect(new Rect(bx, by, barW, bh), radius));
        }
    }

    private void RenderDonut(DrawingContext ctx, double w, double h, IReadOnlyList<double> vals, MavTheme t)
    {
        var c = t.Color;
        var sum = vals.Where(v => v > 0).Sum();
        if (sum <= 0) sum = 1;

        Color[] palette =
        [
            c.Primary,
            c.Tertiary,
            c.Secondary,
            c.PrimaryContainer,
            c.TertiaryContainer,
        ];

        var size = Math.Min(w, h);
        var stroke = Math.Clamp(size * 0.16, 6.0, 14.0);
        var radius = (size - stroke) / 2.0 - 2.0;
        var cx = w / 2.0;
        var cy = h / 2.0;

        double startDeg = -90.0;
        for (int i = 0; i < vals.Count; i++)
        {
            var v = Math.Max(0, vals[i]);
            if (v <= 0) continue;
            var sweep = (v / sum) * 360.0;
            var col = palette[i % palette.Length];
            var pen = MavTheme.PenOf(col, stroke, PenLineCap.Flat);
            while (_donutArcs.Count <= i)
                _donutArcs.Add(new MavArcCache());
            ctx.DrawGeometry(
                null,
                pen,
                _donutArcs[i].Get(new Point(cx, cy), radius, startDeg, Math.Max(1.0, sweep - 3.0)));
            startDeg += sweep;
        }

        if (!string.IsNullOrEmpty(CenterLabel))
        {
            var ft = MavTextMetrics.GetFormattedText(
                CenterLabel!,
                Math.Clamp(size * 0.20, 10, 16),
                MavFonts.RobotoSemiBoldTypeface,
                c.OnSurface);
            ctx.DrawText(ft, new Point(cx - ft.Width / 2.0, cy - ft.Height / 2.0));
        }
    }
}
