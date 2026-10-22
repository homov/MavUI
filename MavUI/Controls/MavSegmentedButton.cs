using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>One segment of a <see cref="MavSegmentedButton"/>.</summary>
public sealed class MavSegment : Control
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavSegment, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MavSegment, string?>(nameof(Icon));

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MavSegment, bool>(nameof(IsSelected), defaultBindingMode: BindingMode.TwoWay);

    internal event EventHandler? SegmentChanged;

    static MavSegment()
    {
        TextProperty.Changed.AddClassHandler<MavSegment>((s, _) => s.SegmentChanged?.Invoke(s, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MavSegment>((s, _) => s.SegmentChanged?.Invoke(s, EventArgs.Empty));
        IsSelectedProperty.Changed.AddClassHandler<MavSegment>((s, _) => s.SegmentChanged?.Invoke(s, EventArgs.Empty));
    }

    /// <summary>Creates an empty segment.</summary>
    public MavSegment()
    {
    }

    /// <summary>Creates a segment with the given text, optional icon and initial selection state.</summary>
    public MavSegment(string text, string? icon = null, bool selected = false)
    {
        Text = text;
        Icon = icon;
        IsSelected = selected;
    }

    [Content]
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Label
    {
        get => Text;
        set => Text = value;
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => default;
}

/// <summary>
/// Material Design 3 single-select or multi-select segmented button bar.
/// </summary>
public class MavSegmentedButton : MavContentView, IEnumerable<MavSegment>
{
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MavSegmentedButton, int>(nameof(SelectedIndex), -1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<MavSegmentedButton, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IList?> SelectedItemsSourceProperty =
        AvaloniaProperty.Register<MavSegmentedButton, IList?>(nameof(SelectedItemsSource));

    public static readonly StyledProperty<bool> MultiSelectProperty =
        AvaloniaProperty.Register<MavSegmentedButton, bool>(nameof(MultiSelect));

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavSegmentedButton, IEnumerable?>(nameof(ItemsSource));

    private readonly AvaloniaList<MavSegment> _segments = new();
    private readonly List<object> _rawItems = [];
    private readonly MavItemEventWiring<MavSegment, EventHandler> _wiredSegments = new(
        static (segment, handler) => segment.SegmentChanged += handler,
        static (segment, handler) => segment.SegmentChanged -= handler);
    private readonly MavCollectionObserver _observer;
    private readonly List<SegmentCell> _cells = [];
    private readonly SegmentedRowPanel _row;
    private bool _multiSelect;
    private bool _syncingSelection;

    public event EventHandler? SelectionChanged;
    public event EventHandler<int>? SelectedIndexChanged;

    static MavSegmentedButton()
    {
        SelectedIndexProperty.Changed.AddClassHandler<MavSegmentedButton>((b, e) =>
            b.OnSelectedIndexPropertyChanged(e.GetNewValue<int>()));
        SelectedItemProperty.Changed.AddClassHandler<MavSegmentedButton>((b, e) =>
            b.OnSelectedItemPropertyChanged(e.GetNewValue<object?>()));
        MultiSelectProperty.Changed.AddClassHandler<MavSegmentedButton>((b, e) =>
            b._multiSelect = e.GetNewValue<bool>());
        ItemsSourceProperty.Changed.AddClassHandler<MavSegmentedButton>((b, e) =>
            b.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
    }

    /// <summary>Creates a single-select segmented button.</summary>
    public MavSegmentedButton() : this(false)
    {
    }

    /// <summary>Creates a segmented button that allows one or several selected segments.</summary>
    public MavSegmentedButton(bool multiSelect = false)
    {
        _multiSelect = multiSelect;
        MultiSelect = multiSelect;
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;
        _observer = new MavCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));

        _segments.CollectionChanged += (_, e) =>
        {
            _wiredSegments.HandleCollectionChanged(e);
            foreach (var seg in _segments)
            {
                MavVisualTreeHelpers.AttachLogicalChild(this, seg);
                _wiredSegments.Add(seg, (_, _) =>
                {
                    if (!_syncingSelection) Rebuild();
                });
            }
            Rebuild();
        };

        _row = new SegmentedRowPanel(this)
        {
            MinHeight = 40,
        };

        Content = _row;
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public bool MultiSelect
    {
        get => GetValue(MultiSelectProperty);
        set => SetValue(MultiSelectProperty, value);
    }

    [Content]
    public AvaloniaList<MavSegment> Segments => _segments;

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public IList? SelectedItemsSource
    {
        get => GetValue(SelectedItemsSourceProperty);
        set => SetValue(SelectedItemsSourceProperty, value);
    }

    private void SyncSelectedItemsSource()
    {
        if (SelectedItemsSource is { IsReadOnly: false } list)
        {
            list.Clear();
            foreach (var idx in SelectedIndices)
            {
                if (idx >= 0 && idx < _rawItems.Count)
                    list.Add(_rawItems[idx]);
                else if (idx >= 0 && idx < _segments.Count)
                    list.Add(_segments[idx].Text);
            }
        }
    }

    private void OnItemsSourceChanged(IEnumerable? src)
    {
        _observer.Attach(src);
        RebuildFromItemsSource(src);
    }

    private void RebuildFromItemsSource(IEnumerable? src)
    {
        if (src is null) return;
        _segments.Clear();
        _rawItems.Clear();
        foreach (var item in src)
        {
            if (item is null) continue;
            _rawItems.Add(item);
            if (item is MavSegment seg)
                _segments.Add(seg);
            else
                _segments.Add(new MavSegment(item.ToString() ?? string.Empty));
        }
    }

    public int SelectedIndex
    {
        get
        {
            for (int i = 0; i < _segments.Count; i++)
                if (_segments[i].IsSelected) return i;
            return GetValue(SelectedIndexProperty);
        }
        set => SetValue(SelectedIndexProperty, value);
    }

    private void OnSelectedIndexPropertyChanged(int value)
    {
        if (_syncingSelection) return;
        _syncingSelection = true;
        for (int i = 0; i < _segments.Count; i++)
            _segments[i].IsSelected = i == value;
        object? item = null;
        if (value >= 0 && value < _rawItems.Count)
            item = _rawItems[value];
        else if (value >= 0 && value < _segments.Count)
            item = _segments[value].Text;
        SetValue(SelectedItemProperty, item);
        ApplyTheme(MavTheme.Current);
        _syncingSelection = false;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        SelectedIndexChanged?.Invoke(this, value);
    }

    private void OnSelectedItemPropertyChanged(object? item)
    {
        if (_syncingSelection) return;
        if (item is null)
        {
            SelectedIndex = -1;
            return;
        }
        int idx = _rawItems.IndexOf(item);
        if (idx < 0 && item is string s)
        {
            for (int i = 0; i < _segments.Count; i++)
            {
                if (string.Equals(_segments[i].Text, s, StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }
        }
        if (idx >= 0)
            SelectedIndex = idx;
    }

    public IReadOnlyList<int> SelectedIndices =>
        _segments.Select((s, i) => (s, i)).Where(x => x.s.IsSelected).Select(x => x.i).ToList();

    /// <summary>Adds a segment with the given text and returns the button for chaining.</summary>
    public MavSegmentedButton AddSegment(string text, string? icon = null, bool selected = false)
    {
        _segments.Add(new MavSegment(text, icon, selected));
        return this;
    }

    /// <summary>Adds an existing segment and returns the button for chaining.</summary>
    public MavSegmentedButton AddSegment(MavSegment segment)
    {
        _segments.Add(segment);
        return this;
    }

    public void Add(string text, string? icon = null, bool selected = false) => AddSegment(text, icon, selected);

    public void Add(MavSegment segment) => AddSegment(segment);

    public IEnumerator<MavSegment> GetEnumerator() => _segments.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void Rebuild()
    {
        _row.Children.Clear();
        _cells.Clear();

        for (int i = 0; i < _segments.Count; i++)
        {
            var idx = i;
            var seg = _segments[i];
            var cell = new SegmentCell(seg, () => OnSegmentClicked(idx));
            _row.Children.Add(cell);
            _cells.Add(cell);
        }

        ApplyTheme(MavTheme.Current);
    }

    private void OnSegmentClicked(int index)
    {
        if (index < 0 || index >= _segments.Count) return;
        if (!_segments[index].IsEnabled) return;

        if (_multiSelect)
        {
            _segments[index].IsSelected = !_segments[index].IsSelected;
            SyncSelectedItemsSource();
        }
        else
        {
            _syncingSelection = true;
            for (int i = 0; i < _segments.Count; i++)
                _segments[i].IsSelected = i == index;
            SetValue(SelectedIndexProperty, index);
            var item = index >= 0 && index < _rawItems.Count ? _rawItems[index] : (object)_segments[index].Text;
            SetValue(SelectedItemProperty, item);
            SyncSelectedItemsSource();
            _syncingSelection = false;
        }

        ApplyTheme(MavTheme.Current);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        SelectedIndexChanged?.Invoke(this, SelectedIndex);
    }

    private void ApplyTheme(MavTheme t)
    {
        var count = _cells.Count;
        var h = Math.Max(32, 40 + t.DensityOffset);
        _row.MinHeight = h;
        var r = h / 2.0;

        for (int i = 0; i < count; i++)
        {
            CornerRadius radii;
            if (count == 1)
                radii = new CornerRadius(r);
            else if (i == 0)
                radii = new CornerRadius(r, 0, 0, r);
            else if (i == count - 1)
                radii = new CornerRadius(0, r, r, 0);
            else
                radii = new CornerRadius(0);

            _cells[i].Update(t, radii, h);
        }

        _row.InvalidateMeasure();
    }

    private sealed class SegmentedRowPanel : Panel
    {
        private readonly MavSegmentedButton _owner;
        private double[] _naturalWidths = [];

        public SegmentedRowPanel(MavSegmentedButton owner)
        {
            _owner = owner;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var count = Children.Count;
            if (count == 0)
                return new Size(0, MinHeight);

            if (_naturalWidths.Length != count)
                _naturalWidths = new double[count];

            double maxNaturalWidth = 0;
            double maxHeight = MinHeight;
            var unconstrained = new Size(double.PositiveInfinity, availableSize.Height);

            for (int i = 0; i < count; i++)
            {
                var child = Children[i];
                if (child is SegmentCell sc)
                    sc.SetAvailableWidth(double.PositiveInfinity);

                child.Measure(unconstrained);
                var w = Math.Max(48.0, Math.Ceiling(child.DesiredSize.Width) + 4.0);
                if (i < _owner._segments.Count)
                {
                    var seg = _owner._segments[i];
                    // Reserve space for checkmark icon when an icon-less segment is unselected
                    // so selecting it never causes layout shift or text truncation.
                    if (!seg.IsSelected && string.IsNullOrEmpty(seg.Icon))
                        w += !string.IsNullOrEmpty(seg.Text) ? 26.0 : 18.0;
                }
                _naturalWidths[i] = w;
                if (w > maxNaturalWidth)
                    maxNaturalWidth = w;
                if (child.DesiredSize.Height > maxHeight)
                    maxHeight = child.DesiredSize.Height;
            }

            var overlaps = Math.Max(0, count - 1);
            var idealEqualTotal = count * maxNaturalWidth - overlaps;
            var targetTotalWidth = double.IsInfinity(availableSize.Width)
                ? idealEqualTotal
                : Math.Min(idealEqualTotal, Math.Max(0, availableSize.Width));

            var colWidths = AllocateColumnWidths(_naturalWidths, targetTotalWidth + overlaps);
            for (int i = 0; i < count; i++)
            {
                if (Children[i] is SegmentCell sc)
                    sc.SetAvailableWidth(colWidths[i]);
                Children[i].Measure(new Size(colWidths[i], maxHeight));
            }

            return new Size(targetTotalWidth, maxHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var count = Children.Count;
            if (count == 0)
                return finalSize;

            if (_naturalWidths.Length != count)
                MeasureOverride(finalSize);

            var overlaps = Math.Max(0, count - 1);
            var colWidths = AllocateColumnWidths(_naturalWidths, Math.Max(0, finalSize.Width) + overlaps);
            var h = Math.Max(MinHeight, finalSize.Height);

            double x = 0;
            for (int i = 0; i < count; i++)
            {
                var w = colWidths[i];
                if (Children[i] is SegmentCell sc)
                    sc.SetAvailableWidth(w);
                Children[i].Arrange(new Rect(x, 0, w, h));
                x += Math.Max(0, w - 1.0); // 1px border overlap between adjacent segments
            }

            return new Size(Math.Max(0, x + (count > 0 ? 1.0 : 0.0)), h);
        }

        private static double[] AllocateColumnWidths(double[] natural, double budget)
        {
            var n = natural.Length;
            var result = new double[n];
            if (n == 0)
                return result;

            double maxD = 0;
            double sumD = 0;
            for (int i = 0; i < n; i++)
            {
                if (natural[i] > maxD) maxD = natural[i];
                sumD += natural[i];
            }

            // 1. Plenty of space for equal-width segments
            if (budget >= n * maxD - 0.5)
            {
                var equal = budget / n;
                for (int i = 0; i < n; i++)
                    result[i] = equal;
                return result;
            }

            // 2. Enough space for all full labels, but not enough for N * maxD:
            //    Water-fill shorter segments upward while preserving longer segments' full natural width.
            if (budget >= sumD - 0.5)
            {
                var locked = new bool[n];
                var remaining = budget;
                var unlocked = n;

                while (unlocked > 0)
                {
                    var avg = remaining / unlocked;
                    var anyLocked = false;
                    for (int i = 0; i < n; i++)
                    {
                        if (!locked[i] && natural[i] > avg)
                        {
                            locked[i] = true;
                            result[i] = natural[i];
                            remaining -= natural[i];
                            unlocked--;
                            anyLocked = true;
                        }
                    }

                    if (!anyLocked)
                    {
                        var fill = unlocked > 0 ? remaining / unlocked : 0;
                        for (int i = 0; i < n; i++)
                        {
                            if (!locked[i])
                                result[i] = fill;
                        }
                        break;
                    }
                }

                return result;
            }

            // 3. Container is narrower than the sum of natural widths:
            //    Keep compact segments at their natural width and cap wider segments so they share remaining width and ellipsize cleanly.
            {
                var locked = new bool[n];
                var remaining = Math.Max(0, budget);
                var unlocked = n;

                while (unlocked > 0)
                {
                    var avg = remaining / unlocked;
                    var anyLocked = false;
                    for (int i = 0; i < n; i++)
                    {
                        if (!locked[i] && natural[i] < avg)
                        {
                            locked[i] = true;
                            result[i] = natural[i];
                            remaining -= natural[i];
                            unlocked--;
                            anyLocked = true;
                        }
                    }

                    if (!anyLocked)
                    {
                        var cap = unlocked > 0 ? Math.Max(0, remaining / unlocked) : 0;
                        for (int i = 0; i < n; i++)
                        {
                            if (!locked[i])
                                result[i] = cap;
                        }
                        break;
                    }
                }

                return result;
            }
        }
    }

    private sealed class SegmentCell : MavContentView
    {
        private readonly MavSegment _seg;
        private readonly MavInkSurface _surface;
        private readonly MavIcon _icon;
        private readonly TextBlock _label;

        public SegmentCell(MavSegment seg, Action onClick)
        {
            _seg = seg;
            ClipToBounds = true;

            _icon = new MavIcon { Size = 18, UseThemeDefaultColor = false, IsVisible = false };
            _label = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            row.Children.Add(_icon);
            row.Children.Add(_label);

            _surface = new MavInkSurface
            {
                ClipToBounds = true,
                Padding = new Thickness(16, 0),
                Content = row,
            };
            _surface.Clicked += (_, _) => onClick();

            Content = _surface;
        }

        public void SetAvailableWidth(double cellWidth)
        {
            if (double.IsInfinity(cellWidth))
            {
                _label.MaxWidth = double.PositiveInfinity;
                return;
            }

            var iconBudget = _icon.IsVisible ? (_label.IsVisible ? 26.0 : 18.0) : 0.0;
            _label.MaxWidth = Math.Max(0, cellWidth - 32.0 - iconBudget);
        }

        public void Update(MavTheme t, CornerRadius radii, double height)
        {
            var c = t.Color;
            _surface.MinHeight = height;
            _surface.CornerRadii = radii;
            _surface.IsEnabled = _seg.IsEnabled;

            var hasText = !string.IsNullOrEmpty(_seg.Text);
            _label.Text = _seg.Text;
            _label.IsVisible = hasText;
            _label.Apply(MavTypeRole.LabelLarge, t.Typography);

            var glyph = _seg.IsSelected ? MavIcons.Check : _seg.Icon;
            var hasIcon = !string.IsNullOrEmpty(glyph);
            _icon.Glyph = glyph;
            _icon.IsVisible = hasIcon;
            _icon.Margin = hasIcon && hasText ? new Thickness(0, 0, 8, 0) : new Thickness(0);

            var bg = _seg.IsSelected ? c.SecondaryContainer : Colors.Transparent;
            var fg = _seg.IsSelected ? c.OnSecondaryContainer : c.OnSurface;
            var stroke = c.Outline;

            if (!_seg.IsEnabled)
            {
                fg = c.OnSurface.WithAlpha(t.StateLayer.DisabledContent);
                stroke = c.OnSurface.WithAlpha(t.StateLayer.DisabledContainer);
            }

            _surface.FillColor = bg;
            _surface.StrokeColor = stroke;
            _surface.StrokeThickness = 1;
            _surface.RippleColor = fg;
            _label.Foreground = MavTheme.BrushOf(fg);
            _icon.Color = fg;
            ZIndex = _seg.IsSelected ? 1 : 0;

            _label.InvalidateMeasure();
            InvalidateMeasure();
        }
    }
}
