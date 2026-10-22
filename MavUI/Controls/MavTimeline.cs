using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 vertical activity / event Timeline control with status indicators,
/// timestamps, descriptions, custom content slots, and C# collection initializer support.
/// </summary>
public class MavTimeline : MavContentView, IEnumerable<MavTimelineItem>
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavTimeline, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<MavTimeline, IDataTemplate?>(nameof(ItemTemplate));

    private readonly AvaloniaList<MavTimelineItem> _items = new();
    private readonly MavItemEventWiring<MavTimelineItem, EventHandler> _wiredItems = new(
        static (item, handler) => item.ItemChanged += handler,
        static (item, handler) => item.ItemChanged -= handler);
    private readonly MavCollectionObserver _observer;
    private readonly StackPanel _stack;

    static MavTimeline()
    {
        ItemsSourceProperty.Changed.AddClassHandler<MavTimeline>((tl, e) =>
            tl.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        ItemTemplateProperty.Changed.AddClassHandler<MavTimeline>((tl, _) =>
            tl.RebuildFromItemsSource(tl.ItemsSource));
    }

    /// <summary>Creates an empty timeline.</summary>
    public MavTimeline()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _observer = new MavCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));
        _items.CollectionChanged += (_, e) =>
        {
            _wiredItems.HandleCollectionChanged(e);
            foreach (var item in _items)
            {
                MavVisualTreeHelpers.AttachLogicalChild(this, item);
                _wiredItems.Add(item, (_, _) => Rebuild());
            }
            Rebuild();
        };
        _stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0,
        };
        Content = _stack;

        MavTheme.Observe(this, (self, _) => self.Rebuild());
    }

    [Content]
    public AvaloniaList<MavTimelineItem> Items => _items;

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

    private void OnItemsSourceChanged(IEnumerable? src)
    {
        _observer.Attach(src);
        RebuildFromItemsSource(src);
    }

    private void RebuildFromItemsSource(IEnumerable? src)
    {
        if (src is null) return;
        _items.Clear();
        foreach (var item in src)
        {
            if (item is null) continue;
            if (item is MavTimelineItem ti)
            {
                _items.Add(ti);
            }
            else if (ItemTemplate?.Build(item) is { } built)
            {
                built.DataContext = item;
                if (built is MavTimelineItem builtTi)
                    _items.Add(builtTi);
                else
                    _items.Add(new MavTimelineItem(item.ToString() ?? string.Empty, customContent: built));
            }
            else
            {
                _items.Add(new MavTimelineItem(item.ToString() ?? string.Empty));
            }
        }
    }

    /// <summary>
    /// Adds an item to the timeline.
    /// </summary>
    public void Add(MavTimelineItem item) => _items.Add(item);

    /// <summary>Adds an item to the timeline.</summary>
    public void Add(
        string title,
        string? description = null,
        string? timestamp = null,
        string? icon = null,
        MavTimelineStatus status = MavTimelineStatus.Completed,
        Control? customContent = null)
        => Add(new MavTimelineItem(title, description, timestamp, icon, status, customContent));

    /// <summary>
    /// Removes all items from the timeline.
    /// </summary>
    public void Clear() => _items.Clear();

    /// <summary>
    /// Returns an enumerator over the timeline items.
    /// </summary>
    public IEnumerator<MavTimelineItem> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void Rebuild()
    {
        foreach (var item in _items)
            MavVisualTreeHelpers.DetachFromParent(item.CustomContent);

        _stack.Children.Clear();
        if (_items.Count == 0) return;

        var t = MavTheme.Current;
        var c = t.Color;

        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var isLast = i == _items.Count - 1;
            _stack.Children.Add(BuildRow(item, isLast, t, c));
        }
    }

    private Control BuildRow(MavTimelineItem item, bool isLast, MavTheme t, MavColorScheme c)
    {
        var (nodeBg, nodeFg, nodeBorder, defaultGlyph, lineColor) = item.Status switch
        {
            MavTimelineStatus.Completed => (
                c.Primary,
                c.OnPrimary,
                c.Primary,
                MavIcons.Check,
                c.Primary.WithAlpha(0.6f)),
            MavTimelineStatus.Active => (
                c.PrimaryContainer,
                c.OnPrimaryContainer,
                c.Primary,
                MavIcons.Schedule,
                c.OutlineVariant),
            MavTimelineStatus.Warning => (
                MavColorScheme.Blend(c.SurfaceContainerHigh, Color.FromRgb(237, 108, 2), 0.25),
                t.IsDark ? Color.FromRgb(255, 204, 128) : Color.FromRgb(180, 75, 0),
                Color.FromRgb(237, 108, 2),
                MavIcons.WarningAmber,
                c.OutlineVariant),
            MavTimelineStatus.Error => (
                c.ErrorContainer,
                c.OnErrorContainer,
                c.Error,
                MavIcons.ErrorOutline,
                c.Error.WithAlpha(0.5f)),
            _ => (
                c.SurfaceContainerHighest,
                c.OnSurfaceVariant,
                c.OutlineVariant,
                null,
                c.OutlineVariant.WithAlpha(0.6f)),
        };

        var glyph = item.Icon ?? defaultGlyph;
        Control nodeContent;
        if (!string.IsNullOrEmpty(glyph))
        {
            nodeContent = new MavIcon(glyph, 16)
            {
                UseThemeDefaultColor = false,
                Color = nodeFg,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
        }
        else
        {
            nodeContent = new Border
            {
                Width = 8,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Background = MavTheme.BrushOf(nodeFg),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
        }

        var nodeCircle = new MavSurface
        {
            Width = 30,
            Height = 30,
            Radius = 15,
            FillColor = nodeBg,
            BorderStrokeThickness = item.Status == MavTimelineStatus.Active ? 2 : 1,
            BorderStrokeColor = nodeBorder,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Content = nodeContent,
        };

        var railGrid = new Grid
        {
            Width = 36,
            RowDefinitions = new RowDefinitions("Auto,*"),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        railGrid.Add(nodeCircle, 0, 0);

        if (!isLast)
        {
            var connector = new Border
            {
                Width = 2,
                MinHeight = 28,
                Background = MavTheme.BrushOf(lineColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(0, 4, 0, 4),
            };
            railGrid.Add(connector, 0, 1);
        }

        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            VerticalAlignment = VerticalAlignment.Center,
        };

        var titleLbl = new TextBlock
        {
            Text = item.Title,
            Foreground = MavTheme.BrushOf(item.Status == MavTimelineStatus.Pending ? c.OnSurfaceVariant : c.OnSurface),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        titleLbl.Apply(MavTypeRole.TitleSmall, t.Typography);
        headerGrid.Add(titleLbl, 0, 0);

        if (!string.IsNullOrEmpty(item.Timestamp))
        {
            var timeLbl = new TextBlock
            {
                Text = item.Timestamp,
                Foreground = MavTheme.BrushOf(c.OnSurfaceVariant),
                VerticalAlignment = VerticalAlignment.Center,
            };
            timeLbl.Apply(MavTypeRole.LabelSmall, t.Typography);

            var timePill = new MavSurface
            {
                Radius = 8,
                Padding = new Thickness(8, 2),
                FillColor = c.SurfaceContainerHigh,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Content = timeLbl,
            };
            headerGrid.Add(timePill, 1, 0);
        }

        var contentCol = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 4,
            Margin = new Thickness(12, 2, 0, isLast ? 4 : 20),
        };
        contentCol.Children.Add(headerGrid);

        if (!string.IsNullOrEmpty(item.Description))
        {
            var descLbl = new TextBlock
            {
                Text = item.Description,
                Foreground = MavTheme.BrushOf(c.OnSurfaceVariant),
                TextWrapping = TextWrapping.Wrap,
            };
            descLbl.Apply(MavTypeRole.BodyMedium, t.Typography);
            contentCol.Children.Add(descLbl);
        }

        if (item.CustomContent is not null)
        {
            item.CustomContent.Margin = new Thickness(0, 6, 0, 0);
            contentCol.Children.Add(item.CustomContent);
        }

        var rowGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
        };
        rowGrid.Add(railGrid, 0, 0);
        rowGrid.Add(contentCol, 1, 0);

        return rowGrid;
    }
}
