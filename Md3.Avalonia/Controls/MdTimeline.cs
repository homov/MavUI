using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

public enum MdTimelineStatus
{
    Completed,
    Active,
    Pending,
    Warning,
    Error,
}

public sealed class MdTimelineItem : Control
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MdTimelineItem, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<MdTimelineItem, string?>(nameof(Description));

    public static readonly StyledProperty<string?> TimestampProperty =
        AvaloniaProperty.Register<MdTimelineItem, string?>(nameof(Timestamp));

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MdTimelineItem, string?>(nameof(Icon));

    public static readonly StyledProperty<MdTimelineStatus> StatusProperty =
        AvaloniaProperty.Register<MdTimelineItem, MdTimelineStatus>(nameof(Status), MdTimelineStatus.Completed);

    public static readonly StyledProperty<Control?> CustomContentProperty =
        AvaloniaProperty.Register<MdTimelineItem, Control?>(nameof(CustomContent));

    internal event EventHandler? ItemChanged;

    static MdTimelineItem()
    {
        TitleProperty.Changed.AddClassHandler<MdTimelineItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        DescriptionProperty.Changed.AddClassHandler<MdTimelineItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        TimestampProperty.Changed.AddClassHandler<MdTimelineItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MdTimelineItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        StatusProperty.Changed.AddClassHandler<MdTimelineItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        CustomContentProperty.Changed.AddClassHandler<MdTimelineItem>((i, e) =>
        {
            MdVisualTreeHelpers.AttachLogicalChild(i, e.GetNewValue<Control?>());
            i.ItemChanged?.Invoke(i, EventArgs.Empty);
        });
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string? Subtitle
    {
        get => Description;
        set => Description = value;
    }

    public bool IsCompleted
    {
        get => Status == MdTimelineStatus.Completed;
        set { if (value) Status = MdTimelineStatus.Completed; }
    }

    public bool IsActive
    {
        get => Status == MdTimelineStatus.Active;
        set { if (value) Status = MdTimelineStatus.Active; }
    }

    public string? Timestamp
    {
        get => GetValue(TimestampProperty);
        set => SetValue(TimestampProperty, value);
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public MdTimelineStatus Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    [Content]
    public Control? CustomContent
    {
        get => GetValue(CustomContentProperty);
        set => SetValue(CustomContentProperty, value);
    }

    public MdTimelineItem()
    {
    }

    public MdTimelineItem(
        string title,
        string? description = null,
        string? timestamp = null,
        string? icon = null,
        MdTimelineStatus status = MdTimelineStatus.Completed,
        Control? customContent = null)
    {
        Title = title;
        Description = description;
        Timestamp = timestamp;
        Icon = icon;
        Status = status;
        CustomContent = customContent;
    }

    protected override Size MeasureOverride(Size availableSize) => default;
}

/// <summary>
/// Material Design 3 vertical activity / event Timeline control with status indicators,
/// timestamps, descriptions, custom content slots, and C# collection initializer support.
/// </summary>
public class MdTimeline : MdContentView, IEnumerable<MdTimelineItem>
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MdTimeline, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<MdTimeline, IDataTemplate?>(nameof(ItemTemplate));

    private readonly AvaloniaList<MdTimelineItem> _items = new();
    private readonly MdItemEventWiring<MdTimelineItem, EventHandler> _wiredItems = new(
        static (item, handler) => item.ItemChanged += handler,
        static (item, handler) => item.ItemChanged -= handler);
    private readonly MdCollectionObserver _observer;
    private readonly StackPanel _stack;

    static MdTimeline()
    {
        ItemsSourceProperty.Changed.AddClassHandler<MdTimeline>((tl, e) =>
            tl.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        ItemTemplateProperty.Changed.AddClassHandler<MdTimeline>((tl, _) =>
            tl.RebuildFromItemsSource(tl.ItemsSource));
    }

    public MdTimeline()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _observer = new MdCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));
        _items.CollectionChanged += (_, e) =>
        {
            _wiredItems.HandleCollectionChanged(e);
            foreach (var item in _items)
            {
                MdVisualTreeHelpers.AttachLogicalChild(this, item);
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

        MdTheme.Observe(this, (self, _) => self.Rebuild());
    }

    [Content]
    public AvaloniaList<MdTimelineItem> Items => _items;

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
            if (item is MdTimelineItem ti)
            {
                _items.Add(ti);
            }
            else if (ItemTemplate?.Build(item) is { } built)
            {
                built.DataContext = item;
                if (built is MdTimelineItem builtTi)
                    _items.Add(builtTi);
                else
                    _items.Add(new MdTimelineItem(item.ToString() ?? string.Empty, customContent: built));
            }
            else
            {
                _items.Add(new MdTimelineItem(item.ToString() ?? string.Empty));
            }
        }
    }

    public void Add(MdTimelineItem item) => _items.Add(item);

    public void Add(
        string title,
        string? description = null,
        string? timestamp = null,
        string? icon = null,
        MdTimelineStatus status = MdTimelineStatus.Completed,
        Control? customContent = null)
        => Add(new MdTimelineItem(title, description, timestamp, icon, status, customContent));

    public void Clear() => _items.Clear();

    public IEnumerator<MdTimelineItem> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void Rebuild()
    {
        foreach (var item in _items)
            MdVisualTreeHelpers.DetachFromParent(item.CustomContent);

        _stack.Children.Clear();
        if (_items.Count == 0) return;

        var t = MdTheme.Current;
        var c = t.Color;

        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var isLast = i == _items.Count - 1;
            _stack.Children.Add(BuildRow(item, isLast, t, c));
        }
    }

    private Control BuildRow(MdTimelineItem item, bool isLast, MdTheme t, MdColorScheme c)
    {
        var (nodeBg, nodeFg, nodeBorder, defaultGlyph, lineColor) = item.Status switch
        {
            MdTimelineStatus.Completed => (
                c.Primary,
                c.OnPrimary,
                c.Primary,
                MdIcons.Check,
                c.Primary.WithAlpha(0.6f)),
            MdTimelineStatus.Active => (
                c.PrimaryContainer,
                c.OnPrimaryContainer,
                c.Primary,
                MdIcons.Schedule,
                c.OutlineVariant),
            MdTimelineStatus.Warning => (
                MdColorScheme.Blend(c.SurfaceContainerHigh, Color.FromRgb(237, 108, 2), 0.25),
                t.IsDark ? Color.FromRgb(255, 204, 128) : Color.FromRgb(180, 75, 0),
                Color.FromRgb(237, 108, 2),
                MdIcons.WarningAmber,
                c.OutlineVariant),
            MdTimelineStatus.Error => (
                c.ErrorContainer,
                c.OnErrorContainer,
                c.Error,
                MdIcons.ErrorOutline,
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
            nodeContent = new MdIcon(glyph, 16)
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
                Background = MdTheme.BrushOf(nodeFg),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
        }

        var nodeCircle = new MdSurface
        {
            Width = 30,
            Height = 30,
            Radius = 15,
            FillColor = nodeBg,
            BorderStrokeThickness = item.Status == MdTimelineStatus.Active ? 2 : 1,
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
                Background = MdTheme.BrushOf(lineColor),
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
            Foreground = MdTheme.BrushOf(item.Status == MdTimelineStatus.Pending ? c.OnSurfaceVariant : c.OnSurface),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        titleLbl.Apply(MdTypeRole.TitleSmall, t.Typography);
        headerGrid.Add(titleLbl, 0, 0);

        if (!string.IsNullOrEmpty(item.Timestamp))
        {
            var timeLbl = new TextBlock
            {
                Text = item.Timestamp,
                Foreground = MdTheme.BrushOf(c.OnSurfaceVariant),
                VerticalAlignment = VerticalAlignment.Center,
            };
            timeLbl.Apply(MdTypeRole.LabelSmall, t.Typography);

            var timePill = new MdSurface
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
                Foreground = MdTheme.BrushOf(c.OnSurfaceVariant),
                TextWrapping = TextWrapping.Wrap,
            };
            descLbl.Apply(MdTypeRole.BodyMedium, t.Typography);
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
