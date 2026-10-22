using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Accordion group of <see cref="MavExpansionPanel"/> items with optional single-expand behavior.
/// </summary>
public class MavExpansionPanelList : StackPanel, System.Collections.Generic.IEnumerable<Control>
{
    public static readonly StyledProperty<bool> SingleExpandProperty =
        AvaloniaProperty.Register<MavExpansionPanelList, bool>(nameof(SingleExpand));

    private readonly List<MavExpansionPanel> _panels = [];
    private bool _updating;

    /// <summary>Creates a list of panels that can be expanded together.</summary>
    public MavExpansionPanelList() : this(false)
    {
    }

    /// <summary>Creates a list of panels; with <paramref name="singleExpand"/> only one panel can be open.</summary>
    public MavExpansionPanelList(bool singleExpand = false)
    {
        Orientation = Orientation.Vertical;
        Spacing = 8;
        SingleExpand = singleExpand;
        Children.CollectionChanged += OnChildrenCollectionChanged;
    }

    public bool SingleExpand
    {
        get => GetValue(SingleExpandProperty);
        set => SetValue(SingleExpandProperty, value);
    }

    public bool AllowMultipleExpanded
    {
        get => !SingleExpand;
        set => SingleExpand = !value;
    }

    /// <summary>Adds a panel and returns the list for chaining.</summary>
    public MavExpansionPanelList AddPanel(MavExpansionPanel panel)
    {
        Children.Add(panel);
        return this;
    }

    public void Add(Control panel) => Children.Add(panel);

    public System.Collections.Generic.IEnumerator<Control> GetEnumerator() => Children.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    private void OnChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is null) return;
        foreach (var item in e.NewItems)
        {
            if (item is MavExpansionPanel panel && !_panels.Contains(panel))
            {
                _panels.Add(panel);
                panel.ExpandedChanged += OnPanelExpandedChanged;
            }
        }
    }

    private void OnPanelExpandedChanged(object? sender, bool expanded)
    {
        if (_updating || !SingleExpand || !expanded || sender is not MavExpansionPanel active) return;
        _updating = true;
        foreach (var p in _panels)
        {
            if (!ReferenceEquals(p, active))
                p.IsExpanded = false;
        }
        _updating = false;
    }
}
