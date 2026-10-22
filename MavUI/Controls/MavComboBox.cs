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
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 editable autocomplete ComboBox with live filtering dropdown,
/// keyboard navigation (<c>Up</c>/<c>Down</c>/<c>Enter</c>/<c>Escape</c>), and <c>ObservableCollection&lt;T&gt;</c> support.
/// </summary>
public class MavComboBox : MavContentView, IEnumerable<string>
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MavComboBox, string>(nameof(Label), "Choose");

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavComboBox, string>(nameof(Text), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<MavComboBox, string?>(nameof(Placeholder));

    public static readonly StyledProperty<string?> SupportingTextProperty =
        AvaloniaProperty.Register<MavComboBox, string?>(nameof(SupportingText));

    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<MavComboBox, string?>(nameof(ErrorText));

    public static readonly StyledProperty<MavTextFieldVariant> VariantProperty =
        AvaloniaProperty.Register<MavComboBox, MavTextFieldVariant>(nameof(Variant), MavTextFieldVariant.Outlined);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavComboBox, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<bool> IsClearableProperty =
        AvaloniaProperty.Register<MavComboBox, bool>(nameof(IsClearable));

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<MavComboBox, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MavComboBox, int>(nameof(SelectedIndex), -1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> DisplayMemberPathProperty =
        AvaloniaProperty.Register<MavComboBox, string?>(nameof(DisplayMemberPath));

    private readonly AvaloniaList<string> _items = new();
    private readonly List<object> _rawItems = [];
    private readonly MavCollectionObserver _collectionObserver;
    private readonly MavTextField _field;
    private IDisposable? _dropdownHandle;
    private StackPanel? _dropdownStack;
    private MavSurface? _dropdownCard;
    private List<string> _currentMatches = [];
    private int _highlightedIndex = -1;
    private bool _suppressPopup;
    private bool _syncingSelection;
    private Func<object, string>? _itemFormatter;

    public event EventHandler<string?>? SelectedItemChanged;
    public event EventHandler<string>? TextChanged;

    static MavComboBox()
    {
        LabelProperty.Changed.AddClassHandler<MavComboBox>((c, e) =>
        {
            c._field.Label = e.GetNewValue<string>() ?? string.Empty;
            MavAutomation.SetName(c, e.GetNewValue<string>());
        });
        TextProperty.Changed.AddClassHandler<MavComboBox>((c, e) =>
        {
            var val = e.GetNewValue<string>() ?? string.Empty;
            if (c._field.Text != val)
                c._field.Text = val;
            c.UpdateTrailingIcon();
        });
        PlaceholderProperty.Changed.AddClassHandler<MavComboBox>((c, e) => c._field.Placeholder = e.GetNewValue<string?>());
        SupportingTextProperty.Changed.AddClassHandler<MavComboBox>((c, e) => c._field.SupportingText = e.GetNewValue<string?>());
        ErrorTextProperty.Changed.AddClassHandler<MavComboBox>((c, e) => c._field.ErrorText = e.GetNewValue<string?>());
        VariantProperty.Changed.AddClassHandler<MavComboBox>((c, e) => c._field.Variant = e.GetNewValue<MavTextFieldVariant>());
        ItemsSourceProperty.Changed.AddClassHandler<MavComboBox>((c, e) =>
            c.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        IsClearableProperty.Changed.AddClassHandler<MavComboBox>((c, _) => c.UpdateTrailingIcon());
        SelectedItemProperty.Changed.AddClassHandler<MavComboBox>((c, e) => c.OnSelectedItemPropertyChanged(e.GetNewValue<object?>()));
        SelectedIndexProperty.Changed.AddClassHandler<MavComboBox>((c, e) =>
        {
            var idx = e.GetNewValue<int>();
            if (idx >= 0 && idx < c._items.Count)
                c.CommitSelection(c._items[idx]);
        });
        DisplayMemberPathProperty.Changed.AddClassHandler<MavComboBox>((c, _) =>
        {
            if (c.ItemsSource is not null)
                c.RebuildFromItemsSource(c.ItemsSource);
        });
    }

    /// <summary>Creates an empty combo box with the default label.</summary>
    public MavComboBox() : this("Choose", null)
    {
    }

    /// <summary>Creates a combo box with the given label and options.</summary>
    public MavComboBox(
        string label = "Choose",
        IEnumerable<string>? items = null,
        MavTextFieldVariant variant = MavTextFieldVariant.Outlined,
        string? leadingIcon = null)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _collectionObserver = new MavCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));

        _field = new MavTextField(label, variant, leadingIcon: leadingIcon)
        {
            TrailingIcon = MavIcons.ArrowDropDown,
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

        MavAutomation.SetControlType(this, AutomationControlType.ComboBox);
        MavAutomation.SetName(this, Label);
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

    public MavTextFieldVariant Variant
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

    /// <summary>Adds an option and returns the combo box for chaining.</summary>
    public MavComboBox AddOption(string item)
    {
        _items.Add(item);
        _rawItems.Add(item);
        return this;
    }

    /// <summary>Adds several options and returns the combo box for chaining.</summary>
    public MavComboBox AddOptions(IEnumerable<string> items)
    {
        foreach (var item in items)
            AddOption(item);
        return this;
    }

    /// <summary>
    /// Adds an option to the combo box.
    /// </summary>
    public void Add(string item) => AddOption(item);

    /// <summary>
    /// Clears the selected item.
    /// </summary>
    public void Clear() => SelectedItem = null;

    /// <summary>
    /// Returns an enumerator over the options.
    /// </summary>
    public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private string FormatItem(object item) =>
        MavVisualTreeHelpers.FormatItemText(item, DisplayMemberPath, _itemFormatter);

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
            _field.TrailingIcon = MavIcons.Cancel;
        else
            _field.TrailingIcon = _dropdownHandle is not null ? MavIcons.ArrowDropUp : MavIcons.ArrowDropDown;
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

    /// <summary>Opens the dropdown list, optionally filtered by <paramref name="filter"/>.</summary>
    public void OpenDropdown(string? filter = null)
    {
        if (!IsEnabled || _items.Count == 0) return;
        RefreshDropdown(filter ?? Text);
    }

    /// <summary>Closes the dropdown list.</summary>
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
            var overlayRoot = MavOverlay.EnsureOverlayRoot(this);
            if (overlayRoot is null) return;

            var t = MavTheme.Current;

            _dropdownStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 6),
            };

            var scroll = new MavScrollViewer
            {
                MaxHeight = 240,
                Content = _dropdownStack,
            };

            var card = new MavSurface
            {
                Radius = 12,
                Elevation = 3,
                FillColor = t.Color.SurfaceContainer,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Content = scroll,
            };

            _dropdownCard = card;
            _dropdownHandle = MavOverlay.Show(
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

        var overlayRoot = MavOverlay.EnsureOverlayRoot(this);
        if (overlayRoot is null) return;

        var bounds = MavOverlay.BoundsInOverlay(this, overlayRoot);
        var overlaySize = MavOverlay.GetOverlaySize(this);
        var insets = MavOverlay.SafeAreaOf(this);

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

        if (_dropdownCard.Content is MavScrollViewer scroll)
            scroll.MaxHeight = Math.Max(44, Math.Min(240, overlaySize.Height - insets.Bottom - top - 8));
    }

    private void RebuildDropdownRows()
    {
        if (_dropdownStack is null) return;
        _dropdownStack.Children.Clear();
        var theme = MavTheme.Current;

        for (var i = 0; i < _currentMatches.Count; i++)
        {
            var itemText = _currentMatches[i];
            var isSel = i == _highlightedIndex || string.Equals(itemText, Text, StringComparison.OrdinalIgnoreCase);
            var lbl = new TextBlock
            {
                Text = itemText,
                VerticalAlignment = VerticalAlignment.Center,
            };
            lbl.Apply(MavTypeRole.BodyLarge, theme.Typography);
            lbl.Foreground = MavTheme.BrushOf(isSel ? theme.Color.OnSecondaryContainer : theme.Color.OnSurface);

            var row = new MavInkSurface
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
