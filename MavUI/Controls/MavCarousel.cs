using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

public sealed class MavCarouselItem : Control
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MavCarouselItem, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<MavCarouselItem, string?>(nameof(Subtitle));

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MavCarouselItem, string?>(nameof(Icon));

    public static readonly StyledProperty<Color?> AccentColorProperty =
        AvaloniaProperty.Register<MavCarouselItem, Color?>(nameof(AccentColor));

    internal event EventHandler? ItemChanged;

    static MavCarouselItem()
    {
        TitleProperty.Changed.AddClassHandler<MavCarouselItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        SubtitleProperty.Changed.AddClassHandler<MavCarouselItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MavCarouselItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        AccentColorProperty.Changed.AddClassHandler<MavCarouselItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public Color? AccentColor
    {
        get => GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    public MavCarouselItem()
    {
    }

    public MavCarouselItem(
        string title,
        string? subtitle = null,
        string? icon = null,
        Color? accentColor = null)
    {
        Title = title;
        Subtitle = subtitle;
        Icon = icon;
        AccentColor = accentColor;
    }

    protected override Size MeasureOverride(Size availableSize) => default;
}

public enum MavCarouselLayout
{
    Hero,
    CenterAligned,
    MultiBrowse,
    Uncontained,
}

/// <summary>
/// Material Design 3 Hero Multi-Browse Carousel with dynamic card widths, rounded clip masking,
/// pointer drag &amp; tap navigation, and page indicator dots.
/// </summary>
public class MavCarousel : Control, IMavDraggableControl, IEnumerable<MavCarouselItem>
{
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MavCarousel, int>(nameof(SelectedIndex), 0, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<MavCarousel, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> ShowIndicatorsProperty =
        AvaloniaProperty.Register<MavCarousel, bool>(nameof(ShowIndicators), true);

    public static readonly StyledProperty<MavCarouselLayout> LayoutProperty =
        AvaloniaProperty.Register<MavCarousel, MavCarouselLayout>(nameof(Layout), MavCarouselLayout.Hero);

    public static readonly StyledProperty<double> ItemHeightProperty =
        AvaloniaProperty.Register<MavCarousel, double>(nameof(ItemHeight), 210.0);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavCarousel, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<MavCarousel, IDataTemplate?>(nameof(ItemTemplate));

    public static readonly StyledProperty<ICommand?> ItemClickCommandProperty =
        AvaloniaProperty.Register<MavCarousel, ICommand?>(nameof(ItemClickCommand));

    private readonly AvaloniaList<MavCarouselItem> _items = new();
    private readonly List<object> _rawItems = [];
    private readonly MavItemEventWiring<MavCarouselItem, EventHandler> _wiredItems = new(
        static (item, handler) => item.ItemChanged += handler,
        static (item, handler) => item.ItemChanged -= handler);
    private readonly MavCollectionObserver _observer;
    private double _animatedIndex;
    private double _dragStartX;
    private double _dragStartIndex;
    private bool _dragging;
    private bool _moved;
    private bool _syncingSelection;

    public event EventHandler<int>? SelectedIndexChanged;
    public event EventHandler<int>? ItemClicked;

    static MavCarousel()
    {
        SelectedIndexProperty.Changed.AddClassHandler<MavCarousel>((c, e) => c.OnSelectedIndexChanged(e.GetNewValue<int>()));
        SelectedItemProperty.Changed.AddClassHandler<MavCarousel>((c, e) => c.OnSelectedItemChanged(e.GetNewValue<object?>()));
        ShowIndicatorsProperty.Changed.AddClassHandler<MavCarousel>((c, _) => c.InvalidateVisual());
        LayoutProperty.Changed.AddClassHandler<MavCarousel>((c, _) => c.InvalidateVisual());
        ItemsSourceProperty.Changed.AddClassHandler<MavCarousel>((c, e) => c.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        ItemTemplateProperty.Changed.AddClassHandler<MavCarousel>((c, _) => c.RebuildFromItemsSource(c.ItemsSource));
    }

    public MavCarousel() : this(210)
    {
    }

    public MavCarousel(double height = 210)
    {
        Height = height;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Center;
        Cursor = new Cursor(StandardCursorType.Hand);
        _observer = new MavCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));
        _items.CollectionChanged += (_, e) =>
        {
            _wiredItems.HandleCollectionChanged(e);
            foreach (var item in _items)
            {
                MavVisualTreeHelpers.AttachLogicalChild(this, item);
                _wiredItems.Add(item, (_, _) => InvalidateVisual());
            }
            InvalidateVisual();
        };

        MavTheme.Observe(this, (self, _) => self.InvalidateVisual());
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set
        {
            var clamped = _items.Count == 0 ? 0 : Math.Clamp(value, 0, _items.Count - 1);
            SetValue(SelectedIndexProperty, clamped);
        }
    }

    public int CurrentIndex
    {
        get => SelectedIndex;
        set => SelectedIndex = value;
    }

    public bool ShowIndicators
    {
        get => GetValue(ShowIndicatorsProperty);
        set => SetValue(ShowIndicatorsProperty, value);
    }

    public MavCarouselLayout Layout
    {
        get => GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    public double ItemHeight
    {
        get => GetValue(ItemHeightProperty);
        set
        {
            SetValue(ItemHeightProperty, value);
            Height = value;
        }
    }

    [Content]
    public AvaloniaList<MavCarouselItem> Items => _items;

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public ICommand? ItemClickCommand
    {
        get => GetValue(ItemClickCommandProperty);
        set => SetValue(ItemClickCommandProperty, value);
    }

    private void OnItemsSourceChanged(IEnumerable? src)
    {
        _observer.Attach(src);
        RebuildFromItemsSource(src);
    }

    private void RebuildFromItemsSource(IEnumerable? src)
    {
        if (src is null) return;
        _items.Clear();
        _rawItems.Clear();
        foreach (var item in src)
        {
            if (item is null) continue;
            _rawItems.Add(item);
            if (item is MavCarouselItem ci)
            {
                _items.Add(ci);
            }
            else if (ItemTemplate?.Build(item) is MavCarouselItem builtCi)
            {
                builtCi.DataContext = item;
                _items.Add(builtCi);
            }
            else
            {
                _items.Add(new MavCarouselItem(item.ToString() ?? string.Empty));
            }
        }
    }

    public MavCarousel AddItem(string title, string? subtitle = null, string? icon = null, Color? accent = null)
    {
        _items.Add(new MavCarouselItem(title, subtitle, icon, accent));
        return this;
    }

    public MavCarousel AddItem(MavCarouselItem item)
    {
        _items.Add(item);
        return this;
    }

    public MavCarousel AddCard(string title, string? subtitle = null, string? icon = null, Color? accent = null) =>
        AddItem(title, subtitle, icon, accent);

    public void Add(string title, string? subtitle = null, string? icon = null, Color? accent = null) =>
        AddItem(title, subtitle, icon, accent);

    public void Add(MavCarouselItem item) => AddItem(item);

    public IEnumerator<MavCarouselItem> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Next()
    {
        if (_items.Count == 0) return;
        SelectedIndex = (SelectedIndex + 1) % _items.Count;
    }

    public void Previous()
    {
        if (_items.Count == 0) return;
        SelectedIndex = (SelectedIndex - 1 + _items.Count) % _items.Count;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var w = double.IsFinite(availableSize.Width) && availableSize.Width > 0 ? availableSize.Width : 320;
        var h = !double.IsNaN(Height) && Height > 0 ? Height : 210;
        return new Size(w, h);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_items.Count == 0) return;
        if (MavGestureZones.ShouldDeferToSystem(this, e)) return;
        _dragging = true;
        _moved = false;
        _dragStartX = e.GetPosition(this).X;
        _dragStartIndex = _animatedIndex;
        MavMotion.Cancel(this, "car_idx");
        e.Pointer.Capture(this);
        e.PreventGestureRecognition();
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_dragging || _items.Count == 0) return;
        e.PreventGestureRecognition();
        var dx = e.GetPosition(this).X - _dragStartX;
        if (Math.Abs(dx) > 6) _moved = true;

        var stepW = Math.Max(80.0, Bounds.Width * 0.45);
        _animatedIndex = Math.Clamp(_dragStartIndex - dx / stepW, 0, Math.Max(0, _items.Count - 1));
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_dragging) return;
        _dragging = false;
        e.Pointer.Capture(null);

        if (!_moved)
        {
            var hitIdx = HitTestCard(e.GetPosition(this).X);
            if (hitIdx >= 0)
            {
                if (hitIdx == SelectedIndex)
                {
                    ItemClicked?.Invoke(this, hitIdx);
                    if (ItemClickCommand is { } cCmd && cCmd.CanExecute(hitIdx))
                        cCmd.Execute(hitIdx);
                }
                else
                {
                    SelectedIndex = hitIdx;
                }
                return;
            }
        }

        var target = (int)Math.Round(Math.Clamp(_animatedIndex, 0, Math.Max(0, _items.Count - 1)));
        if (target == SelectedIndex)
            AnimateToIndex(target);
        else
            SelectedIndex = target;
        e.Handled = true;
    }

    private void OnSelectedIndexChanged(int idx)
    {
        if (!_syncingSelection)
        {
            _syncingSelection = true;
            object? item = null;
            if (idx >= 0 && idx < _rawItems.Count)
                item = _rawItems[idx];
            else if (idx >= 0 && idx < _items.Count)
                item = _items[idx];
            SetValue(SelectedItemProperty, item);
            _syncingSelection = false;
        }
        AnimateToIndex(idx);
        SelectedIndexChanged?.Invoke(this, idx);
    }

    private void OnSelectedItemChanged(object? item)
    {
        if (_syncingSelection || item is null) return;
        _syncingSelection = true;
        int idx = _rawItems.IndexOf(item);
        if (idx < 0 && item is MavCarouselItem ci)
            idx = _items.IndexOf(ci);
        if (idx >= 0)
            SelectedIndex = idx;
        _syncingSelection = false;
    }

    private void AnimateToIndex(int idx)
    {
        MavMotion.Animate(this, "car_idx", _animatedIndex, idx, MavMotion.Medium2, MavMotion.CubicOut, v =>
        {
            _animatedIndex = v;
            InvalidateVisual();
        });
    }

    private int HitTestCard(double x)
    {
        var layouts = ComputeCardLayouts(Bounds.Width);
        for (int i = 0; i < layouts.Count; i++)
        {
            if (x >= layouts[i].X && x <= layouts[i].X + layouts[i].W)
                return i;
        }
        return -1;
    }

    internal List<(double X, double W, double Focus)> ComputeCardLayouts(double totalW) =>
        ComputeCardLayouts(totalW, _animatedIndex);

    internal List<(double X, double W, double Focus)> ComputeCardLayouts(double totalW, double animatedIndex)
    {
        return Layout switch
        {
            MavCarouselLayout.CenterAligned => ComputeCenterAlignedLayouts(totalW, animatedIndex),
            MavCarouselLayout.MultiBrowse => ComputeMultiBrowseLayouts(totalW, animatedIndex),
            MavCarouselLayout.Uncontained => ComputeUncontainedLayouts(totalW, animatedIndex),
            _ => ComputeHeroLayouts(totalW, animatedIndex),
        };
    }

    private static double FocusOf(int index, double animatedIndex) =>
        Math.Clamp(1.0 - Math.Abs(index - animatedIndex) * 0.65, 0.0, 1.0);

    private static int FocusIndex(int count, double animatedIndex) =>
        count == 0 ? 0 : Math.Clamp((int)Math.Round(animatedIndex), 0, count - 1);

    /// <summary>
    /// Hero layout: the focused card is wide and aligned to the leading edge;
    /// following cards are weighted peeks.
    /// </summary>
    private List<(double X, double W, double Focus)> ComputeHeroLayouts(double totalW, double animatedIndex)
    {
        var result = new List<(double, double, double)>(_items.Count);
        if (_items.Count == 0 || totalW <= 0) return result;

        const double gap = 8.0;
        var weights = new double[_items.Count];
        var focuses = new double[_items.Count];
        double weightSum = 0;

        for (int i = 0; i < _items.Count; i++)
        {
            var focus = FocusOf(i, animatedIndex);
            focuses[i] = focus;
            var w = 0.35 + focus * 1.45;
            weights[i] = w;
            weightSum += w;
        }

        var availW = Math.Max(40, totalW - gap * (_items.Count - 1));
        var widths = new double[_items.Count];
        for (int i = 0; i < _items.Count; i++)
            widths[i] = availW * (weights[i] / weightSum);

        var focusIndex = FocusIndex(_items.Count, animatedIndex);
        double focusStart = 0;
        for (int i = 0; i < focusIndex; i++)
            focusStart += widths[i] + gap;

        double curX = -focusStart;
        for (int i = 0; i < _items.Count; i++)
        {
            result.Add((curX, widths[i], focuses[i]));
            curX += widths[i] + gap;
        }

        return result;
    }

    /// <summary>
    /// Center-aligned layout: the focused card is centred in the viewport with equal peeks
    /// on both sides; the whole row is shifted so that the focused card stays in the middle.
    /// </summary>
    private List<(double X, double W, double Focus)> ComputeCenterAlignedLayouts(double totalW, double animatedIndex)
    {
        var result = new List<(double, double, double)>(_items.Count);
        if (_items.Count == 0 || totalW <= 0) return result;

        const double gap = 8.0;
        var focusW = Math.Clamp(totalW * 0.56, 120, 480);
        var sideW = Math.Clamp(totalW * 0.16, 48, 160);
        var widths = new double[_items.Count];
        var focuses = new double[_items.Count];
        var focusIndex = FocusIndex(_items.Count, animatedIndex);

        for (int i = 0; i < _items.Count; i++)
        {
            focuses[i] = FocusOf(i, animatedIndex);
            widths[i] = sideW + (focusW - sideW) * focuses[i];
        }

        double focusStart = 0;
        for (int i = 0; i < focusIndex; i++)
            focusStart += widths[i] + gap;
        var shift = totalW / 2 - (focusStart + widths[focusIndex] / 2);

        double curX = shift;
        for (int i = 0; i < _items.Count; i++)
        {
            result.Add((curX, widths[i], focuses[i]));
            curX += widths[i] + gap;
        }

        return result;
    }

    /// <summary>
    /// Multi-browse layout: several equally sized small cards. The row only scrolls
    /// when the focused card would otherwise leave the viewport.
    /// </summary>
    private List<(double X, double W, double Focus)> ComputeMultiBrowseLayouts(double totalW, double animatedIndex)
    {
        var result = new List<(double, double, double)>(_items.Count);
        if (_items.Count == 0 || totalW <= 0) return result;

        const double gap = 8.0;
        var visible = Math.Min(_items.Count, 3);
        var baseW = Math.Max(72, (totalW - gap * (visible - 1)) / visible);
        var widths = new double[_items.Count];
        var focuses = new double[_items.Count];
        double contentW = gap * (_items.Count - 1);

        for (int i = 0; i < _items.Count; i++)
        {
            focuses[i] = FocusOf(i, animatedIndex);
            widths[i] = baseW;
            contentW += widths[i];
        }

        var focusIndex = FocusIndex(_items.Count, animatedIndex);
        double focusStart = 0;
        for (int i = 0; i < focusIndex; i++)
            focusStart += widths[i] + gap;

        var shift = 0.0;
        if (focusStart + widths[focusIndex] > totalW || focusStart < 0)
        {
            var desired = totalW / 2 - (focusStart + widths[focusIndex] / 2);
            shift = Math.Clamp(desired, Math.Min(0, totalW - contentW), 0);
        }

        double curX = shift;
        for (int i = 0; i < _items.Count; i++)
        {
            result.Add((curX, widths[i], focuses[i]));
            curX += widths[i] + gap;
        }

        return result;
    }

    /// <summary>
    /// Uncontained layout: wide cards that bleed past the viewport edges; the focused
    /// card is centred and neighbours are partially visible.
    /// </summary>
    private List<(double X, double W, double Focus)> ComputeUncontainedLayouts(double totalW, double animatedIndex)
    {
        var result = new List<(double, double, double)>(_items.Count);
        if (_items.Count == 0 || totalW <= 0) return result;

        const double gap = 8.0;
        var cw = Math.Clamp(totalW * 0.46, 160, 480);
        var shift = totalW / 2 - (cw / 2 + animatedIndex * (cw + gap));

        for (int i = 0; i < _items.Count; i++)
            result.Add((shift + i * (cw + gap), cw, FocusOf(i, animatedIndex)));

        return result;
    }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0 || _items.Count == 0) return;

        context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));

        // Neighbouring cards are drawn past the edges while the carousel moves; keep them inside the control.
        using var clip = context.PushClip(new Rect(0, 0, w, h));

        var t = MavTheme.Current;
        var c = t.Color;
        var showDots = ShowIndicators;
        var cardH = showDots ? Math.Max(60, h - 22) : h;
        var layouts = ComputeCardLayouts(w);

        Color[] palette =
        [
            c.PrimaryContainer,
            c.SecondaryContainer,
            c.TertiaryContainer,
            MavColorScheme.Blend(c.PrimaryContainer, c.TertiaryContainer, 0.5),
            MavColorScheme.Blend(c.SecondaryContainer, c.PrimaryContainer, 0.5),
        ];

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var (cx, cw, focus) = layouts[i];
            if (cw < 12) continue;

            var rect = new Rect(cx, 0, cw, cardH);
            var radius = Math.Min(28.0, cw / 2.0);
            var rrect = new RoundedRect(rect, radius);

            var baseColor = item.AccentColor ?? palette[i % palette.Length];
            var fgColor = item.AccentColor is not null ? Colors.White : c.OnPrimaryContainer;

            using (context.PushClip(rrect))
            {
                context.DrawRectangle(MavTheme.BrushOf(baseColor), null, rrect);

                // Subtle decorative circle in top-right of card
                context.DrawEllipse(
                    MavTheme.BrushOf(fgColor.WithAlpha(0.10f)),
                    null,
                    new Point(cx + cw - 16, 28),
                    Math.Min(56, cw * 0.45),
                    Math.Min(56, cw * 0.45));

                if (!string.IsNullOrEmpty(item.Icon))
                {
                    var iconSz = 22.0 + focus * 10.0;
                    var iconFt = MavTextMetrics.GetFormattedText(
                        item.Icon!,
                        iconSz,
                        MavFonts.ResolveIconTypeface(item.Icon),
                        fgColor.WithAlpha((float)(0.75 + 0.25 * focus)));
                    context.DrawText(iconFt, new Point(cx + 16, 16));
                }

                if (cw >= 64)
                {
                    var titleAlpha = (float)Math.Clamp((cw - 56) / 60.0, 0.2, 1.0);
                    var titleFt = MavTextMetrics.GetFormattedText(
                        item.Title,
                        15 + focus * 3,
                        MavFonts.RobotoSemiBoldTypeface,
                        fgColor.WithAlpha(titleAlpha),
                        Math.Max(20, cw - 28),
                        1,
                        TextTrimming.CharacterEllipsis);

                    var subY = cardH - 18.0;
                    if (!string.IsNullOrEmpty(item.Subtitle) && cw >= 110 && focus > 0.35)
                    {
                        var subFt = MavTextMetrics.GetFormattedText(
                            item.Subtitle!,
                            12,
                            MavFonts.RobotoRegularTypeface,
                            fgColor.WithAlpha((float)(0.80 * focus)),
                            Math.Max(20, cw - 28),
                            1,
                            TextTrimming.CharacterEllipsis);
                        context.DrawText(subFt, new Point(cx + 14, cardH - subFt.Height - 14));
                        subY = cardH - subFt.Height - 16;
                    }

                    context.DrawText(titleFt, new Point(cx + 14, subY - titleFt.Height));
                }
            }
        }

        // Indicator dots
        if (!showDots) return;
        var dotY = h - 6.0;
        const double dotSpacing = 6.0;
        double dotsW = 0;
        for (int i = 0; i < _items.Count; i++)
        {
            var f = layouts[i].Focus;
            dotsW += 6.0 + f * 12.0;
        }
        dotsW += dotSpacing * (_items.Count - 1);

        var dotX = (w - dotsW) / 2.0;
        for (int i = 0; i < _items.Count; i++)
        {
            var f = layouts[i].Focus;
            var dw = 6.0 + f * 12.0;
            var dCol = MavColorScheme.Blend(c.OutlineVariant, c.Primary, f);
            context.DrawRectangle(
                MavTheme.BrushOf(dCol),
                null,
                new RoundedRect(new Rect(dotX, dotY - 3, dw, 6), 3));
            dotX += dw + dotSpacing;
        }
    }
}
