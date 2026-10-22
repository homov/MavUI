using System.Collections;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Material Design 3 editable autocomplete ComboBox with live filtering dropdown,
/// keyboard navigation (<c>Up</c>/<c>Down</c>/<c>Enter</c>/<c>Escape</c>), and <c>ObservableCollection&lt;T&gt;</c> support.
/// </summary>
public class MdComboBox : MdContentView, IEnumerable<string>
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MdComboBox, string>(nameof(Label), "Choose");

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MdComboBox, string>(nameof(Text), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<MdComboBox, string?>(nameof(Placeholder));

    public static readonly StyledProperty<string?> SupportingTextProperty =
        AvaloniaProperty.Register<MdComboBox, string?>(nameof(SupportingText));

    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<MdComboBox, string?>(nameof(ErrorText));

    public static readonly StyledProperty<MdTextFieldVariant> VariantProperty =
        AvaloniaProperty.Register<MdComboBox, MdTextFieldVariant>(nameof(Variant), MdTextFieldVariant.Outlined);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MdComboBox, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<bool> IsClearableProperty =
        AvaloniaProperty.Register<MdComboBox, bool>(nameof(IsClearable));

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<MdComboBox, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MdComboBox, int>(nameof(SelectedIndex), -1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> DisplayMemberPathProperty =
        AvaloniaProperty.Register<MdComboBox, string?>(nameof(DisplayMemberPath));

    private readonly AvaloniaList<string> _items = new();
    private readonly List<object> _rawItems = [];
    private readonly MdCollectionObserver _collectionObserver;
    private readonly MdTextField _field;
    private IDisposable? _dropdownHandle;
    private StackPanel? _dropdownStack;
    private MdSurface? _dropdownCard;
    private List<string> _currentMatches = [];
    private int _highlightedIndex = -1;
    private bool _suppressPopup;
    private bool _syncingSelection;
    private Func<object, string>? _itemFormatter;

    public event EventHandler<string?>? SelectedItemChanged;
    public event EventHandler<string>? TextChanged;

    static MdComboBox()
    {
        LabelProperty.Changed.AddClassHandler<MdComboBox>((c, e) =>
        {
            c._field.Label = e.GetNewValue<string>() ?? string.Empty;
            MdAutomation.SetName(c, e.GetNewValue<string>());
        });
        TextProperty.Changed.AddClassHandler<MdComboBox>((c, e) =>
        {
            var val = e.GetNewValue<string>() ?? string.Empty;
            if (c._field.Text != val)
                c._field.Text = val;
            c.UpdateTrailingIcon();
        });
        PlaceholderProperty.Changed.AddClassHandler<MdComboBox>((c, e) => c._field.Placeholder = e.GetNewValue<string?>());
        SupportingTextProperty.Changed.AddClassHandler<MdComboBox>((c, e) => c._field.SupportingText = e.GetNewValue<string?>());
        ErrorTextProperty.Changed.AddClassHandler<MdComboBox>((c, e) => c._field.ErrorText = e.GetNewValue<string?>());
        VariantProperty.Changed.AddClassHandler<MdComboBox>((c, e) => c._field.Variant = e.GetNewValue<MdTextFieldVariant>());
        ItemsSourceProperty.Changed.AddClassHandler<MdComboBox>((c, e) =>
            c.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        IsClearableProperty.Changed.AddClassHandler<MdComboBox>((c, _) => c.UpdateTrailingIcon());
        SelectedItemProperty.Changed.AddClassHandler<MdComboBox>((c, e) => c.OnSelectedItemPropertyChanged(e.GetNewValue<object?>()));
        SelectedIndexProperty.Changed.AddClassHandler<MdComboBox>((c, e) =>
        {
            var idx = e.GetNewValue<int>();
            if (idx >= 0 && idx < c._items.Count)
                c.CommitSelection(c._items[idx]);
        });
        DisplayMemberPathProperty.Changed.AddClassHandler<MdComboBox>((c, _) =>
        {
            if (c.ItemsSource is not null)
                c.RebuildFromItemsSource(c.ItemsSource);
        });
    }

    public MdComboBox() : this("Choose", null)
    {
    }

    public MdComboBox(
        string label = "Choose",
        IEnumerable<string>? items = null,
        MdTextFieldVariant variant = MdTextFieldVariant.Outlined,
        string? leadingIcon = null)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _collectionObserver = new MdCollectionObserver(() => RebuildFromItemsSource(ItemsSource));

        _field = new MdTextField(label, variant, leadingIcon: leadingIcon)
        {
            TrailingIcon = MdIcons.ArrowDropDown,
        };
        Label = label;
        Variant = variant;

        _field.TextChanged += (_, txt) =>
        {
            SetValue(TextProperty, txt);
            UpdateTrailingIcon();
            TextChanged?.Invoke(this, txt);
            if (!_suppressPopup && _field.InnerEntry.IsFocused)
                RefreshDropdown(txt);
        };

        _field.TrailingIconClicked += (_, _) =>
        {
            if (IsClearable && !string.IsNullOrEmpty(Text))
            {
                Clear();
                return;
            }
            if (_dropdownHandle is not null)
                CloseDropdown();
            else
                OpenDropdown(filter: string.Empty);
        };

        _field.InnerEntry.KeyDown += OnEntryKeyDown;

        Content = _field;

        if (items is not null)
            AddOptions(items);

        MdAutomation.SetControlType(this, AutomationControlType.ComboBox);
        MdAutomation.SetName(this, Label);
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value ?? string.Empty);
    }

    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public string? SupportingText
    {
        get => GetValue(SupportingTextProperty);
        set => SetValue(SupportingTextProperty, value);
    }

    public string? ErrorText
    {
        get => GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorText);

    public bool IsError => HasError;

    public bool IsClearable
    {
        get => GetValue(IsClearableProperty);
        set => SetValue(IsClearableProperty, value);
    }

    public Func<object, string>? ItemFormatter
    {
        get => _itemFormatter;
        set
        {
            _itemFormatter = value;
            if (ItemsSource is not null)
                RebuildFromItemsSource(ItemsSource);
        }
    }

    public MdTextFieldVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    [Content]
    public AvaloniaList<string> Items => _items;

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public string? DisplayMemberPath
    {
        get => GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    private void OnSelectedItemPropertyChanged(object? val)
    {
        if (_syncingSelection) return;
        _syncingSelection = true;
        _suppressPopup = true;
        var text = val is null ? string.Empty : FormatItem(val);
        Text = text;
        _suppressPopup = false;
        _syncingSelection = false;
        UpdateTrailingIcon();
        SelectedItemChanged?.Invoke(this, text);
    }

    public MdComboBox AddOption(string item)
    {
        _items.Add(item);
        _rawItems.Add(item);
        return this;
    }

    public MdComboBox AddOptions(IEnumerable<string> items)
    {
        foreach (var item in items)
            AddOption(item);
        return this;
    }

    public void Add(string item) => AddOption(item);

    public void Clear() => SelectedItem = null;

    public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private string FormatItem(object item) =>
        MdVisualTreeHelpers.FormatItemText(item, DisplayMemberPath, _itemFormatter);

    private void OnItemsSourceChanged(IEnumerable? src)
    {
        _collectionObserver.Attach(src);
        RebuildFromItemsSource(src);
    }

    private void RebuildFromItemsSource(IEnumerable? src)
    {
        _items.Clear();
        _rawItems.Clear();
        if (src is not null)
        {
            foreach (var item in src)
            {
                if (item is not null)
                {
                    _rawItems.Add(item);
                    _items.Add(FormatItem(item));
                }
            }
        }
        if (SelectedIndex >= 0 && SelectedIndex < _items.Count)
            CommitSelection(_items[SelectedIndex]);
    }

    private void UpdateTrailingIcon()
    {
        if (IsClearable && !string.IsNullOrEmpty(Text))
            _field.TrailingIcon = MdIcons.Cancel;
        else
            _field.TrailingIcon = _dropdownHandle is not null ? MdIcons.ArrowDropUp : MdIcons.ArrowDropDown;
    }

    private void OnEntryKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down)
        {
            if (_dropdownHandle is null)
            {
                OpenDropdown(filter: string.Empty);
            }
            else if (_currentMatches.Count > 0)
            {
                _highlightedIndex = (_highlightedIndex + 1) % _currentMatches.Count;
                RebuildDropdownRows();
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Up && _dropdownHandle is not null && _currentMatches.Count > 0)
        {
            _highlightedIndex = _highlightedIndex <= 0 ? _currentMatches.Count - 1 : _highlightedIndex - 1;
            RebuildDropdownRows();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && _dropdownHandle is not null && _highlightedIndex >= 0 && _highlightedIndex < _currentMatches.Count)
        {
            CommitSelection(_currentMatches[_highlightedIndex]);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _dropdownHandle is not null)
        {
            CloseDropdown();
            e.Handled = true;
        }
    }

    private void CommitSelection(string itemText)
    {
        _suppressPopup = true;
        Text = itemText;
        _suppressPopup = false;
        _syncingSelection = true;
        int idx = -1;
        for (int i = 0; i < _items.Count; i++)
        {
            if (string.Equals(_items[i], itemText, StringComparison.OrdinalIgnoreCase))
            {
                idx = i;
                break;
            }
        }
        var raw = idx >= 0 && idx < _rawItems.Count ? _rawItems[idx] : itemText;
        SetValue(SelectedItemProperty, raw);
        _syncingSelection = false;
        CloseDropdown();
        SelectedItemChanged?.Invoke(this, itemText);
    }

    public void OpenDropdown(string? filter = null)
    {
        if (!IsEnabled || _items.Count == 0) return;
        RefreshDropdown(filter ?? Text);
    }

    public void CloseDropdown()
    {
        if (_dropdownHandle is not null)
        {
            _dropdownHandle.Dispose();
            _dropdownHandle = null;
            _dropdownStack = null;
            _dropdownCard = null;
            _currentMatches.Clear();
            _highlightedIndex = -1;
            UpdateTrailingIcon();
        }
    }

    private void RefreshDropdown(string? query)
    {
        _currentMatches = string.IsNullOrWhiteSpace(query)
            ? _items.ToList()
            : _items.Where(x => x.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

        if (_currentMatches.Count == 0)
        {
            CloseDropdown();
            return;
        }

        _highlightedIndex = _currentMatches.FindIndex(x => string.Equals(x, Text, StringComparison.OrdinalIgnoreCase));
        if (_highlightedIndex < 0)
            _highlightedIndex = 0;

        if (_dropdownHandle is null)
        {
            var overlayRoot = MdOverlay.EnsureOverlayRoot(this);
            if (overlayRoot is null) return;

            var t = MdTheme.Current;

            _dropdownStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 6),
            };

            var scroll = new MdScrollViewer
            {
                MaxHeight = 240,
                Content = _dropdownStack,
            };

            var card = new MdSurface
            {
                Radius = 12,
                Elevation = 3,
                FillColor = t.Color.SurfaceContainer,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Content = scroll,
            };

            _dropdownCard = card;
            _dropdownHandle = MdOverlay.Show(
                this,
                card,
                Colors.Transparent,
                CloseDropdown,
                onViewportChanged: PlaceDropdown);
            UpdateTrailingIcon();
        }

        RebuildDropdownRows();
        PlaceDropdown();
    }

    /// <summary>
    /// Places the dropdown under (or above) the field and keeps it inside the overlay
    /// viewport and its safe area. Runs on open and after every resize/rotation.
    /// </summary>
    private void PlaceDropdown()
    {
        if (_dropdownCard is null || _dropdownStack is null) return;

        var overlayRoot = MdOverlay.EnsureOverlayRoot(this);
        if (overlayRoot is null) return;

        var bounds = MdOverlay.BoundsInOverlay(this, overlayRoot);
        var overlaySize = MdOverlay.GetOverlaySize(this);
        var insets = MdOverlay.SafeAreaOf(this);

        var maxWidth = Math.Max(120, overlaySize.Width - insets.Left - insets.Right - 16);
        var width = Math.Min(Math.Max(180, bounds.Width), maxWidth);
        var maxHeight = Math.Max(120, overlaySize.Height - insets.Top - insets.Bottom - 16);

        // Rows are rebuilt on every keystroke, so estimate the height from the match
        // count: this lets the card flip above the field before it is measured.
        var estHeight = Math.Min(240, Math.Max(44, _currentMatches.Count * 44 + 12));
        var spaceBelow = overlaySize.Height - insets.Bottom - bounds.Bottom - 4 - 8;
        var top = spaceBelow >= estHeight
            ? bounds.Bottom + 4
            : Math.Max(insets.Top + 8, bounds.Y - estHeight - 4);

        _dropdownCard.Width = width;
        _dropdownCard.MaxWidth = maxWidth;
        _dropdownCard.MaxHeight = maxHeight;
        _dropdownCard.Margin = new Thickness(
            Math.Clamp(
                bounds.X,
                insets.Left + 8,
                Math.Max(insets.Left + 8, overlaySize.Width - insets.Right - width - 8)),
            top,
            0,
            0);

        if (_dropdownCard.Content is MdScrollViewer scroll)
            scroll.MaxHeight = Math.Max(44, Math.Min(240, overlaySize.Height - insets.Bottom - top - 8));
    }

    private void RebuildDropdownRows()
    {
        if (_dropdownStack is null) return;
        _dropdownStack.Children.Clear();
        var theme = MdTheme.Current;

        for (var i = 0; i < _currentMatches.Count; i++)
        {
            var itemText = _currentMatches[i];
            var isSel = i == _highlightedIndex || string.Equals(itemText, Text, StringComparison.OrdinalIgnoreCase);
            var lbl = new TextBlock
            {
                Text = itemText,
                VerticalAlignment = VerticalAlignment.Center,
            };
            lbl.Apply(MdTypeRole.BodyLarge, theme.Typography);
            lbl.Foreground = MdTheme.BrushOf(isSel ? theme.Color.OnSecondaryContainer : theme.Color.OnSurface);

            var row = new MdInkSurface
            {
                MinHeight = 44,
                CornerRadius = 0,
                Padding = new Thickness(16, 0),
                FillColor = isSel ? theme.Color.SecondaryContainer : Colors.Transparent,
                RippleColor = theme.Color.OnSurface,
                Content = lbl,
            };
            row.Clicked += (_, _) => CommitSelection(itemText);
            _dropdownStack.Children.Add(row);
        }
    }
}
