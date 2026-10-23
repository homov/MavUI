using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;

namespace Mav.Controls;

/// <summary>
/// Theme-free items host: builds one control per item from <see cref="ItemTemplate"/> and lays them
/// out in a line or in wrapping rows. The library ships no Avalonia theme, so a plain
/// <see cref="ItemsControl"/> is a templated control without a template: it creates no
/// <c>ItemsPresenter</c> and nothing is ever drawn. Use this instead (in AXAML or in code), exactly as
/// <see cref="MavWindow"/> replaces <see cref="Window"/> and <see cref="MavScrollViewer"/> replaces
/// <see cref="ScrollViewer"/>.
/// </summary>
/// <example>
/// <code>
/// var items = new MavItems { ItemsSource = roles, ItemTemplate = new FuncDataTemplate&lt;MavColorRole&gt;((role, _) => new MavLabel { Text = role.ToString() }) };
/// </code>
/// </example>
public class MavItems : Panel
{
    /// <summary>The items to show. Changing the collection in place (INotifyCollectionChanged) rebuilds the children.</summary>
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavItems, IEnumerable?>(nameof(ItemsSource));

    /// <summary>Builds one control per item. Without it an item that is a <see cref="Control"/> is used as is,
    /// and anything else becomes a <see cref="TextBlock"/> with its <c>ToString()</c>.</summary>
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<MavItems, IDataTemplate?>(nameof(ItemTemplate));

    /// <summary>Gap between items in a row and between rows.</summary>
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<MavItems, double>(nameof(Spacing));

    /// <summary>Wraps items into rows at the available width instead of one endless line.</summary>
    public static readonly StyledProperty<bool> WrapProperty =
        AvaloniaProperty.Register<MavItems, bool>(nameof(Wrap));

    /// <summary>
    /// <see cref="Orientation.Horizontal"/> (default) puts the items side by side;
    /// <see cref="Orientation.Vertical"/> gives every item its own full-width row.
    /// </summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<MavItems, Orientation>(nameof(Orientation));

    private INotifyCollectionChanged? _observed;
    private readonly List<Control> _built = [];

    static MavItems()
    {
        ItemsSourceProperty.Changed.AddClassHandler<MavItems>((items, _) => items.OnItemsSourceChanged());
        ItemTemplateProperty.Changed.AddClassHandler<MavItems>((items, _) => items.Rebuild());
        SpacingProperty.Changed.AddClassHandler<MavItems>((items, _) => items.InvalidateMeasure());
        WrapProperty.Changed.AddClassHandler<MavItems>((items, _) => items.InvalidateMeasure());
        OrientationProperty.Changed.AddClassHandler<MavItems>((items, _) => items.InvalidateMeasure());
    }

    /// <inheritdoc cref="ItemsSourceProperty" />
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <inheritdoc cref="ItemTemplateProperty" />
    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <inheritdoc cref="SpacingProperty" />
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    /// <inheritdoc cref="WrapProperty" />
    public bool Wrap
    {
        get => GetValue(WrapProperty);
        set => SetValue(WrapProperty, value);
    }

    /// <inheritdoc cref="OrientationProperty" />
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>Rebuilds the children from <see cref="ItemsSource"/>.</summary>
    public void Refresh() => Rebuild();

    private void OnItemsSourceChanged()
    {
        if (_observed is not null)
            _observed.CollectionChanged -= OnCollectionChanged;

        _observed = ItemsSource as INotifyCollectionChanged;
        if (_observed is not null)
            _observed.CollectionChanged += OnCollectionChanged;

        Rebuild();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        // Only children this host created are dropped: children added by hand (no ItemsSource) stay.
        foreach (var child in _built)
            Children.Remove(child);
        _built.Clear();

        if (ItemsSource is null)
        {
            InvalidateMeasure();
            return;
        }

        foreach (var item in ItemsSource)
        {
            var control = Build(item);
            if (control is null)
                continue;
            _built.Add(control);
            Children.Add(control);
        }

        InvalidateMeasure();
    }

    private Control? Build(object? item)
    {
        if (ItemTemplate is { } template)
        {
            var built = template.Build(item);
            if (built is null)
                return null;
            built.DataContext = item;
            return built;
        }

        return item switch
        {
            Control control => control,
            null => null,
            _ => new TextBlock { Text = item.ToString() },
        };
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var spacing = Math.Max(0, Spacing);
        if (Children.Count == 0)
            return default;

        if (Orientation == Orientation.Vertical)
        {
            var width = 0.0;
            var height = 0.0;
            foreach (var child in Children)
            {
                child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                width = Math.Max(width, child.DesiredSize.Width);
                height += child.DesiredSize.Height;
            }

            height += spacing * (Children.Count - 1);
            return new Size(double.IsInfinity(availableSize.Width) ? width : Math.Min(availableSize.Width, width), height);
        }

        if (!Wrap)
        {
            var width = 0.0;
            var height = 0.0;
            foreach (var child in Children)
            {
                child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                width += child.DesiredSize.Width;
                height = Math.Max(height, child.DesiredSize.Height);
            }

            width += spacing * (Children.Count - 1);
            return new Size(width, height);
        }

        var rows = Rows(availableSize.Width, spacing, measure: true);
        var totalHeight = rows.Sum(row => row.Height) + spacing * Math.Max(0, rows.Count - 1);
        var totalWidth = rows.Count == 0 ? 0 : rows.Max(row => row.Width);
        return new Size(
            double.IsInfinity(availableSize.Width) ? totalWidth : Math.Min(availableSize.Width, totalWidth),
            totalHeight);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var spacing = Math.Max(0, Spacing);
        if (Orientation == Orientation.Vertical)
        {
            var y = 0.0;
            foreach (var child in Children)
            {
                child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height));
                y += child.DesiredSize.Height + spacing;
            }

            return finalSize;
        }

        if (!Wrap)
        {
            var x = 0.0;
            var height = 0.0;
            foreach (var child in Children)
                height = Math.Max(height, child.DesiredSize.Height);

            foreach (var child in Children)
            {
                var width = child.DesiredSize.Width;
                child.Arrange(new Rect(x, 0, width, height));
                x += width + spacing;
            }

            return new Size(x, height);
        }

        var rows = Rows(finalSize.Width, spacing, measure: false);
        var y = 0.0;
        foreach (var row in rows)
        {
            var x = 0.0;
            foreach (var child in row.Items)
            {
                child.Arrange(new Rect(x, y, child.DesiredSize.Width, row.Height));
                x += child.DesiredSize.Width + spacing;
            }

            y += row.Height + spacing;
        }

        return finalSize;
    }

    /// <summary>
    /// Groups children into rows. With <paramref name="measure"/> the children are measured against the
    /// row width first (first pass); otherwise their desired sizes are already known (arrange pass).
    /// </summary>
    private List<Row> Rows(double availableWidth, double spacing, bool measure)
    {
        var rows = new List<Row>();
        var row = new Row();
        foreach (var child in Children)
        {
            if (measure)
                child.Measure(new Size(
                    double.IsInfinity(availableWidth) ? double.PositiveInfinity : Math.Max(0, availableWidth),
                    double.PositiveInfinity));

            var width = child.DesiredSize.Width;
            var extra = row.Items.Count == 0 ? width : width + spacing;
            if (row.Items.Count > 0 && !double.IsInfinity(availableWidth) && row.Width + extra > availableWidth + 0.5)
            {
                rows.Add(row);
                row = new Row();
                extra = width;
            }

            row.Items.Add(child);
            row.Width += extra;
            row.Height = Math.Max(row.Height, child.DesiredSize.Height);
        }

        if (row.Items.Count > 0)
            rows.Add(row);
        return rows;
    }

    private sealed class Row
    {
        public List<Control> Items { get; } = [];

        public double Width { get; set; }

        public double Height { get; set; }
    }
}
