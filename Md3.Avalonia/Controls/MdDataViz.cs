using System.Collections;
using System.Globalization;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Overlapping stack of <see cref="MdAvatar"/> items with automatic <c>+N</c> overflow badge.
/// </summary>
public class MdAvatarGroup : MdContentView, IEnumerable<MdAvatar>
{
    public static readonly StyledProperty<int> MaxDisplayedProperty =
        AvaloniaProperty.Register<MdAvatarGroup, int>(nameof(MaxDisplayed), 4);

    public static readonly StyledProperty<double> AvatarSizeProperty =
        AvaloniaProperty.Register<MdAvatarGroup, double>(nameof(AvatarSize), 38.0);

    public static readonly StyledProperty<double> OverlapRatioProperty =
        AvaloniaProperty.Register<MdAvatarGroup, double>(nameof(OverlapRatio), 0.28);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MdAvatarGroup, IEnumerable?>(nameof(ItemsSource));

    private readonly AvaloniaList<MdAvatar> _avatars = new();
    private readonly MdCollectionObserver _observer;
    private readonly Canvas _canvas;

    static MdAvatarGroup()
    {
        MaxDisplayedProperty.Changed.AddClassHandler<MdAvatarGroup>((g, _) => g.Rebuild());
        AvatarSizeProperty.Changed.AddClassHandler<MdAvatarGroup>((g, _) => g.Rebuild());
        OverlapRatioProperty.Changed.AddClassHandler<MdAvatarGroup>((g, _) => g.Rebuild());
        ItemsSourceProperty.Changed.AddClassHandler<MdAvatarGroup>((g, e) => g.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
    }

    public MdAvatarGroup() : this(4, 38.0)
    {
    }

    public MdAvatarGroup(int maxDisplayed = 4, double avatarSize = 38.0)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;
        _observer = new MdCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));
        _avatars.CollectionChanged += (_, _) =>
        {
            foreach (var av in _avatars)
                MdVisualTreeHelpers.AttachLogicalChild(this, av);
            Rebuild();
        };
        _canvas = new Canvas();
        Content = _canvas;
        MaxDisplayed = maxDisplayed;
        AvatarSize = avatarSize;
        MdTheme.Observe(this, (self, _) => self.Rebuild());
    }

    public int MaxDisplayed
    {
        get => GetValue(MaxDisplayedProperty);
        set => SetValue(MaxDisplayedProperty, Math.Max(1, value));
    }

    public double AvatarSize
    {
        get => GetValue(AvatarSizeProperty);
        set => SetValue(AvatarSizeProperty, Math.Max(24, value));
    }

    public double OverlapRatio
    {
        get => GetValue(OverlapRatioProperty);
        set => SetValue(OverlapRatioProperty, Math.Clamp(value, 0.0, 0.65));
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private void OnItemsSourceChanged(IEnumerable? src)
    {
        _observer.Attach(src);
        RebuildFromItemsSource(src);
    }

    private void RebuildFromItemsSource(IEnumerable? src)
    {
        if (src is null) return;
        _avatars.Clear();
        foreach (var item in src)
        {
            if (item is MdAvatar av)
                _avatars.Add(av);
            else if (item is not null)
                AddAvatar(item.ToString() ?? string.Empty);
        }
    }

    [Content]
    public AvaloniaList<MdAvatar> Avatars => _avatars;

    public MdAvatarGroup AddAvatar(string initials, string? icon = null, Color? fillColor = null, Color? textColor = null)
    {
        var avatar = new MdAvatar(initials, icon, AvatarSize);
        if (fillColor is not null) avatar.CustomFillColor = fillColor;
        if (textColor is not null) avatar.CustomTextColor = textColor;
        _avatars.Add(avatar);
        return this;
    }

    public MdAvatarGroup AddAvatar(MdAvatar avatar)
    {
        _avatars.Add(avatar);
        return this;
    }

    public void Add(MdAvatar avatar) => AddAvatar(avatar);

    public void Add(string initials) => AddAvatar(initials);

    public IEnumerator<MdAvatar> GetEnumerator() => _avatars.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void Rebuild()
    {
        _canvas.Children.Clear();
        if (_avatars.Count == 0)
        {
            _canvas.Width = 0;
            _canvas.Height = AvatarSize;
            return;
        }

        var c = MdTheme.Current.Color;
        Color[] paletteBg =
        [
            c.PrimaryContainer,
            c.SecondaryContainer,
            c.TertiaryContainer,
            c.InversePrimary,
        ];
        Color[] paletteFg =
        [
            c.OnPrimaryContainer,
            c.OnSecondaryContainer,
            c.OnTertiaryContainer,
            c.OnSurface,
        ];

        var size = AvatarSize;
        var ringPad = 2.0;
        var outerSize = size + ringPad * 2;
        var step = size * (1.0 - OverlapRatio);
        var max = Math.Max(1, MaxDisplayed);
        var visibleCount = Math.Min(_avatars.Count, max);
        var overflow = _avatars.Count - visibleCount;

        for (int i = 0; i < visibleCount; i++)
        {
            var av = _avatars[i];
            MdVisualTreeHelpers.DetachFromParent(av);
            av.Size = size;
            if (av.CustomFillColor is null)
            {
                av.CustomFillColor = paletteBg[i % paletteBg.Length];
                av.CustomTextColor = paletteFg[i % paletteFg.Length];
            }

            var ring = new Border
            {
                Width = outerSize,
                Height = outerSize,
                CornerRadius = new CornerRadius(outerSize / 2.0),
                Background = MdTheme.BrushOf(c.Surface),
                Padding = new Thickness(ringPad),
                Child = av,
            };
            Canvas.SetLeft(ring, i * step);
            Canvas.SetTop(ring, 0);
            _canvas.Children.Add(ring);
        }

        int totalSlots = visibleCount;
        if (overflow > 0)
        {
            var badgeAvatar = new MdAvatar($"+{overflow}", size: size)
            {
                CustomFillColor = c.SurfaceContainerHighest,
                CustomTextColor = c.OnSurfaceVariant,
            };
            var ring = new Border
            {
                Width = outerSize,
                Height = outerSize,
                CornerRadius = new CornerRadius(outerSize / 2.0),
                Background = MdTheme.BrushOf(c.Surface),
                Padding = new Thickness(ringPad),
                Child = badgeAvatar,
            };
            Canvas.SetLeft(ring, visibleCount * step);
            Canvas.SetTop(ring, 0);
            _canvas.Children.Add(ring);
            totalSlots++;
        }

        _canvas.Width = totalSlots > 0 ? (totalSlots - 1) * step + outerSize : 0;
        _canvas.Height = outerSize;
    }
}

public enum MdSparklineKind
{
    Line,
    Bar,
    Donut,
}

/// <summary>
/// Material Design 3 mini chart / sparkline supporting Line (with area fill), Bar, and Donut modes.
/// </summary>
public class MdSparkline : Control
{
    private readonly List<MdArcCache> _donutArcs = [];

    public static readonly StyledProperty<MdSparklineKind> KindProperty =
        AvaloniaProperty.Register<MdSparkline, MdSparklineKind>(nameof(Kind), MdSparklineKind.Line);

    public static readonly StyledProperty<IReadOnlyList<double>?> ValuesProperty =
        AvaloniaProperty.Register<MdSparkline, IReadOnlyList<double>?>(nameof(Values));

    public static readonly StyledProperty<string?> ValuesTextProperty =
        AvaloniaProperty.Register<MdSparkline, string?>(nameof(ValuesText));

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MdSparkline, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<Color?> AccentColorProperty =
        AvaloniaProperty.Register<MdSparkline, Color?>(nameof(AccentColor));

    public static readonly StyledProperty<string?> CenterLabelProperty =
        AvaloniaProperty.Register<MdSparkline, string?>(nameof(CenterLabel));

    static MdSparkline()
    {
        AffectsRender<MdSparkline>(KindProperty, ValuesProperty, AccentColorProperty, CenterLabelProperty);
        KindProperty.Changed.AddClassHandler<MdSparkline>((s, e) =>
        {
            var k = e.GetNewValue<MdSparklineKind>();
            s.MinWidth = k == MdSparklineKind.Donut ? 76 : 120;
            s.MinHeight = k == MdSparklineKind.Donut ? 76 : 48;
        });
        ValuesTextProperty.Changed.AddClassHandler<MdSparkline>((s, e) =>
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
        ItemsSourceProperty.Changed.AddClassHandler<MdSparkline>((s, e) =>
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

    public MdSparkline() : this(null, MdSparklineKind.Line, null)
    {
    }

    public MdSparkline(
        IReadOnlyList<double>? values = null,
        MdSparklineKind kind = MdSparklineKind.Line,
        string? centerLabel = null)
    {
        Kind = kind;
        Values = values ?? [18, 32, 24, 46, 38, 62, 54, 78];
        CenterLabel = centerLabel;
        MinWidth = kind == MdSparklineKind.Donut ? 76 : 120;
        MinHeight = kind == MdSparklineKind.Donut ? 76 : 48;
        MdTheme.Observe(this, (self, _) => self.InvalidateVisual());
    }

    public MdSparklineKind Kind
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

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        var vals = Values;
        if (w <= 4 || h <= 4 || vals is null || vals.Count == 0) return;

        var t = MdTheme.Current;
        var c = t.Color;
        var accent = AccentColor ?? c.Primary;

        switch (Kind)
        {
            case MdSparklineKind.Line:
                RenderLine(context, w, h, vals, accent);
                break;
            case MdSparklineKind.Bar:
                RenderBar(context, w, h, vals, accent, c);
                break;
            case MdSparklineKind.Donut:
                RenderDonut(context, w, h, vals, t);
                break;
        }
    }

    private static void RenderLine(DrawingContext ctx, double w, double h, IReadOnlyList<double> vals, Color accent)
    {
        if (vals.Count == 1)
        {
            ctx.DrawEllipse(MdTheme.BrushOf(accent), null, new Point(w / 2, h / 2), 4, 4);
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
        ctx.DrawGeometry(MdTheme.BrushOf(accent.WithAlpha(0.18f)), null, areaGeo);

        var lineGeo = new StreamGeometry();
        using (var gc = lineGeo.Open())
        {
            gc.BeginFigure(pts[0], false);
            for (int i = 1; i < pts.Length; i++)
                gc.LineTo(pts[i]);
            gc.EndFigure(false);
        }
        ctx.DrawGeometry(null, MdTheme.PenOf(accent, 2.5, PenLineCap.Round, PenLineJoin.Round), lineGeo);

        var last = pts[^1];
        ctx.DrawEllipse(MdTheme.BrushOf(accent), null, last, 4, 4);
    }

    private static void RenderBar(DrawingContext ctx, double w, double h, IReadOnlyList<double> vals, Color accent, MdColorScheme c)
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
                MdTheme.BrushOf(col),
                null,
                new RoundedRect(new Rect(bx, by, barW, bh), radius));
        }
    }

    private void RenderDonut(DrawingContext ctx, double w, double h, IReadOnlyList<double> vals, MdTheme t)
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
            var pen = MdTheme.PenOf(col, stroke, PenLineCap.Flat);
            while (_donutArcs.Count <= i)
                _donutArcs.Add(new MdArcCache());
            ctx.DrawGeometry(
                null,
                pen,
                _donutArcs[i].Get(new Point(cx, cy), radius, startDeg, Math.Max(1.0, sweep - 3.0)));
            startDeg += sweep;
        }

        if (!string.IsNullOrEmpty(CenterLabel))
        {
            var ft = MdTextMetrics.GetFormattedText(
                CenterLabel!,
                Math.Clamp(size * 0.20, 10, 16),
                MdFonts.RobotoSemiBoldTypeface,
                c.OnSurface);
            ctx.DrawText(ft, new Point(cx - ft.Width / 2.0, cy - ft.Height / 2.0));
        }
    }
}
