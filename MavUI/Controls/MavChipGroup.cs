using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Chip Group container supporting wrapped or single-line scrollable layout,
/// single-selection or multi-selection filter mode, and dynamic chip management.
/// </summary>
public class MavChipGroup : MavContentView, System.Collections.Generic.IEnumerable<MavChip>
{
    public static readonly StyledProperty<bool> SingleSelectionProperty =
        AvaloniaProperty.Register<MavChipGroup, bool>(nameof(SingleSelection));

    public static readonly StyledProperty<bool> SelectionRequiredProperty =
        AvaloniaProperty.Register<MavChipGroup, bool>(nameof(SelectionRequired));

    public static readonly StyledProperty<bool> SingleLineProperty =
        AvaloniaProperty.Register<MavChipGroup, bool>(nameof(SingleLine));

    public static readonly StyledProperty<MavChipKind> DefaultChipKindProperty =
        AvaloniaProperty.Register<MavChipGroup, MavChipKind>(nameof(DefaultChipKind), MavChipKind.Filter);

    public bool Wrap
    {
        get => !SingleLine;
        set => SingleLine = !value;
    }

    public MavChipKind ChipVariant
    {
        get => DefaultChipKind;
        set => DefaultChipKind = value;
    }

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MavChipGroup, int>(nameof(SelectedIndex), -1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<MavChipGroup, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<System.Collections.IList?> SelectedItemsSourceProperty =
        AvaloniaProperty.Register<MavChipGroup, System.Collections.IList?>(nameof(SelectedItemsSource));

    public static readonly StyledProperty<System.Collections.IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavChipGroup, System.Collections.IEnumerable?>(nameof(ItemsSource));

    private readonly AvaloniaList<MavChip> _chips = new();
    private readonly List<object> _rawItems = [];
    private readonly MavItemEventWiring<MavChip, EventHandler> _wiredChips = new(
        static (chip, handler) => chip.Clicked += handler,
        static (chip, handler) => chip.Clicked -= handler);
    private readonly WrapPanel _wrapPanel;
    private readonly StackPanel _linePanel;
    private readonly ScrollViewer _lineScroller;
    private readonly MavCollectionObserver _observer;
    private readonly double _spacing;
    private bool _updating;
    private bool _suppressChipsCollection;

    public event EventHandler? SelectionChanged;

    static MavChipGroup()
    {
        SingleLineProperty.Changed.AddClassHandler<MavChipGroup>((g, _) => g.RebuildHost());
        SelectedIndexProperty.Changed.AddClassHandler<MavChipGroup>((g, e) => g.OnSelectedIndexChanged(e.GetNewValue<int>()));
        SelectedItemProperty.Changed.AddClassHandler<MavChipGroup>((g, e) => g.OnSelectedItemChanged(e.GetNewValue<object?>()));
        ItemsSourceProperty.Changed.AddClassHandler<MavChipGroup>((g, e) => g.OnItemsSourceChanged(e.GetNewValue<System.Collections.IEnumerable?>()));
    }

    /// <summary>Creates a multi-select chip group with spacing 8.</summary>
    public MavChipGroup() : this(false, false, 8)
    {
    }

    /// <summary>Creates a chip group with the given selection mode, line mode and spacing.</summary>
    public MavChipGroup(bool singleSelection = false, bool singleLine = false, double spacing = 8)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _spacing = spacing;
        _observer = new MavCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));

        _chips.CollectionChanged += (_, e) =>
        {
            _wiredChips.HandleCollectionChanged(e);
            if (_suppressChipsCollection) return;
            foreach (var chip in _chips)
                EnsureChipWired(chip);
            RebuildHost();
        };

        _wrapPanel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
        };

        _linePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = spacing,
        };

        _lineScroller = new MavScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Child = _linePanel,
        };

        SingleSelection = singleSelection;
        SingleLine = singleLine;
        RebuildHost();
    }

    public bool SingleSelection
    {
        get => GetValue(SingleSelectionProperty);
        set => SetValue(SingleSelectionProperty, value);
    }

    public bool SelectionRequired
    {
        get => GetValue(SelectionRequiredProperty);
        set => SetValue(SelectionRequiredProperty, value);
    }

    public bool SingleLine
    {
        get => GetValue(SingleLineProperty);
        set => SetValue(SingleLineProperty, value);
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public System.Collections.IList? SelectedItemsSource
    {
        get => GetValue(SelectedItemsSourceProperty);
        set => SetValue(SelectedItemsSourceProperty, value);
    }

    public System.Collections.IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public MavChipKind DefaultChipKind
    {
        get => GetValue(DefaultChipKindProperty);
        set => SetValue(DefaultChipKindProperty, value);
    }

    [Content]
    public AvaloniaList<MavChip> Chips => _chips;

    public IReadOnlyList<int> SelectedIndices =>
        _chips
            .Select((c, i) => (c.IsSelected, i))
            .Where(x => x.IsSelected)
            .Select(x => x.i)
            .ToList();

    public IReadOnlyList<string> SelectedTexts =>
        _chips.Where(c => c.IsSelected).Select(c => c.Text).ToList();

    /// <summary>Adds a chip with a label, icon and selection state and returns the group for chaining.</summary>
    public MavChipGroup AddChip(
        string text,
        MavChipKind kind = MavChipKind.Filter,
        string? icon = null,
        bool selected = false,
        bool elevated = false)
    {
        var chip = new MavChip(text, kind, icon, selected, elevated);
        return AddChip(chip);
    }

    /// <summary>Adds an existing chip and returns the group for chaining.</summary>
    public MavChipGroup AddChip(MavChip chip)
    {
        var idx = _chips.Count;
        EnsureChipWired(chip);
        _suppressChipsCollection = true;
        _chips.Add(chip);
        _suppressChipsCollection = false;
        if (chip.IsSelected && (SingleSelection || SelectedIndex < 0))
            SetValue(SelectedIndexProperty, idx);
        AttachChipToPanel(chip);
        return this;
    }

    private void EnsureChipWired(MavChip chip)
    {
        chip.AutoToggleOnClick = false;
        _wiredChips.Add(chip, (_, _) =>
        {
            var idx = _chips.IndexOf(chip);
            if (idx >= 0) OnChipClicked(idx);
        });
    }

    /// <summary>Adds a chip without returning the group.</summary>
    public void Add(
        string text,
        MavChipKind kind = MavChipKind.Filter,
        string? icon = null,
        bool selected = false,
        bool elevated = false) => AddChip(text, kind, icon, selected, elevated);

    /// <summary>
    /// Adds a chip to the group.
    /// </summary>
    public void Add(MavChip chip) => AddChip(chip);

    /// <summary>
    /// Returns an enumerator over the chips in the group.
    /// </summary>
    public IEnumerator<MavChip> GetEnumerator() => _chips.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    private void RebuildHost()
    {
        _wrapPanel.Children.Clear();
        _linePanel.Children.Clear();

        foreach (var chip in _chips)
            AttachChipToPanel(chip);

        Child = SingleLine ? _lineScroller : _wrapPanel;
    }

    private void OnItemsSourceChanged(System.Collections.IEnumerable? src)
    {
        _observer.Attach(src);
        RebuildFromItemsSource(src);
    }

    private void RebuildFromItemsSource(System.Collections.IEnumerable? src)
    {
        _chips.Clear();
        _rawItems.Clear();
        _wrapPanel.Children.Clear();
        _linePanel.Children.Clear();
        if (src is null) return;
        foreach (var item in src)
        {
            if (item is null) continue;
            _rawItems.Add(item);
            if (item is MavChip chip)
                AddChip(chip);
            else
                AddChip(item.ToString() ?? string.Empty, DefaultChipKind);
        }
        if (SelectedIndex >= 0 && SelectedIndex < _chips.Count)
            OnSelectedIndexChanged(SelectedIndex);
    }

    private void AttachChipToPanel(MavChip chip)
    {
        if (SingleLine)
        {
            chip.Margin = new Thickness(0);
            _linePanel.Children.Add(chip);
        }
        else
        {
            chip.Margin = new Thickness(0, 0, _spacing, _spacing);
            _wrapPanel.Children.Add(chip);
        }
    }

    private void SyncSelectedObjects()
    {
        var firstIdx = SelectedIndices.FirstOrDefault(-1);
        object? item = null;
        if (firstIdx >= 0 && firstIdx < _rawItems.Count)
            item = _rawItems[firstIdx];
        else if (firstIdx >= 0 && firstIdx < _chips.Count)
            item = _chips[firstIdx].Text;
        SetValue(SelectedItemProperty, item);

        if (SelectedItemsSource is { IsReadOnly: false } list)
        {
            list.Clear();
            foreach (var idx in SelectedIndices)
            {
                if (idx >= 0 && idx < _rawItems.Count)
                    list.Add(_rawItems[idx]);
                else if (idx >= 0 && idx < _chips.Count)
                    list.Add(_chips[idx].Text);
            }
        }
    }

    private void OnChipClicked(int index)
    {
        if (index < 0 || index >= _chips.Count) return;
        var chip = _chips[index];

        if (SingleSelection)
        {
            if (chip.IsSelected && !SelectionRequired)
            {
                SelectedIndex = -1;
            }
            else
            {
                SelectedIndex = index;
            }
        }
        else
        {
            if (chip.IsSelected && SelectionRequired && SelectedIndices.Count <= 1)
                return;
            chip.IsSelected = !chip.IsSelected;
            _updating = true;
            SetValue(SelectedIndexProperty, SelectedIndices.FirstOrDefault(-1));
            SyncSelectedObjects();
            _updating = false;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnSelectedIndexChanged(int idx)
    {
        if (_updating) return;
        _updating = true;
        if (SingleSelection)
        {
            for (int i = 0; i < _chips.Count; i++)
                _chips[i].IsSelected = i == idx;
        }
        else if (idx >= 0 && idx < _chips.Count)
        {
            _chips[idx].IsSelected = true;
        }
        SyncSelectedObjects();
        _updating = false;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnSelectedItemChanged(object? item)
    {
        if (_updating) return;
        if (item is null)
        {
            SelectedIndex = -1;
            return;
        }
        int idx = _rawItems.IndexOf(item);
        if (idx < 0 && item is string s)
        {
            for (int i = 0; i < _chips.Count; i++)
            {
                if (string.Equals(_chips[i].Text, s, StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }
        }
        if (idx >= 0)
            SelectedIndex = idx;
    }
}
