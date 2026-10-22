using System.Collections;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 read-only Select / Exposed Dropdown Menu built on <see cref="MavTextField"/> and <see cref="MavMenu"/>.
/// Supports string options, arbitrary object <see cref="ItemsSource"/> (including <c>ObservableCollection&lt;T&gt;</c>),
/// <see cref="ItemFormatter"/>, and <see cref="IsClearable"/>.
/// </summary>
public class MavSelect : MavContentView, IEnumerable<string>
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MavSelect, string>(nameof(Label), "Select");

    public static readonly StyledProperty<string?> SupportingTextProperty =
        AvaloniaProperty.Register<MavSelect, string?>(nameof(SupportingText));

    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<MavSelect, string?>(nameof(ErrorText));

    public static readonly StyledProperty<MavTextFieldVariant> VariantProperty =
        AvaloniaProperty.Register<MavSelect, MavTextFieldVariant>(nameof(Variant), MavTextFieldVariant.Outlined);

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MavSelect, int>(nameof(SelectedIndex), -1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object?> SelectedObjectProperty =
        AvaloniaProperty.Register<MavSelect, object?>(nameof(SelectedObject), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavSelect, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<bool> IsClearableProperty =
        AvaloniaProperty.Register<MavSelect, bool>(nameof(IsClearable));

    public static readonly StyledProperty<string?> DisplayMemberPathProperty =
        AvaloniaProperty.Register<MavSelect, string?>(nameof(DisplayMemberPath));

    private readonly AvaloniaList<string> _options = new();
    private readonly List<object> _rawItems = [];
    private readonly MavCollectionObserver _collectionObserver;
    private readonly MavTextField _field;
    private readonly MavMenu _menu;
    private Func<object, string>? _itemFormatter;
    private bool _syncingSelection;

    public event EventHandler<int>? SelectedIndexChanged;
    public event EventHandler<object?>? SelectedObjectChanged;

    static MavSelect()
    {
        LabelProperty.Changed.AddClassHandler<MavSelect>((s, e) =>
        {
            s._field.Label = e.GetNewValue<string>() ?? string.Empty;
            MavAutomation.SetName(s, e.GetNewValue<string>());
        });
        SupportingTextProperty.Changed.AddClassHandler<MavSelect>((s, e) => s._field.SupportingText = e.GetNewValue<string?>());
        ErrorTextProperty.Changed.AddClassHandler<MavSelect>((s, e) => s._field.ErrorText = e.GetNewValue<string?>());
        VariantProperty.Changed.AddClassHandler<MavSelect>((s, e) => s._field.Variant = e.GetNewValue<MavTextFieldVariant>());
        SelectedIndexProperty.Changed.AddClassHandler<MavSelect>((s, e) => s.OnIndexChanged(e.GetNewValue<int>()));
        SelectedObjectProperty.Changed.AddClassHandler<MavSelect>((s, e) => s.OnSelectedObjectChanged(e.GetNewValue<object?>()));
        ItemsSourceProperty.Changed.AddClassHandler<MavSelect>((s, e) => s.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        IsClearableProperty.Changed.AddClassHandler<MavSelect>((s, _) => s.UpdateTrailingIcon());
        DisplayMemberPathProperty.Changed.AddClassHandler<MavSelect>((s, _) =>
        {
            if (s.ItemsSource is not null)
                s.RebuildFromItemsSource(s.ItemsSource);
        });
    }

    /// <summary>Creates a select field with the localized default label.</summary>
    public MavSelect() : this(MavLocalization.Select, null)
    {
    }

    /// <summary>Creates a select field with the given label, options and selection.</summary>
    public MavSelect(
        string? label = null,
        IEnumerable<string>? options = null,
        MavTextFieldVariant variant = MavTextFieldVariant.Outlined,
        string? leadingIcon = null)
    {
        label ??= MavLocalization.Select;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Cursor = new Cursor(StandardCursorType.Hand);
        _collectionObserver = new MavCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));

        _field = new MavTextField(label, variant, leadingIcon: leadingIcon)
        {
            IsReadOnly = true,
            TrailingIcon = MavIcons.ArrowDropDown,
        };
        Label = label;
        Variant = variant;
        _field.InnerEntry.IsHitTestVisible = false;
        _field.TrailingIconClicked += (_, _) =>
        {
            if (IsClearable && SelectedIndex >= 0)
                Clear();
            else
                OpenMenu();
        };

        var tapBox = new Grid();
        tapBox.Children.Add(_field);

        var overlayTap = new Border
        {
            Background = global::Avalonia.Media.Brushes.Transparent,
            Margin = new Thickness(0, 0, 44, 0),
        };
        overlayTap.PointerReleased += (_, e) =>
        {
            e.Handled = true;
            OpenMenu();
        };
        tapBox.Children.Add(overlayTap);

        _menu = new MavMenu();
        _menu.Closed += (_, _) => UpdateTrailingIcon();

        Content = tapBox;

        if (options is not null)
        {
            foreach (var opt in options)
                AddOption(opt);
        }

        MavAutomation.SetControlType(this, AutomationControlType.ComboBox);
        MavAutomation.SetName(this, Label);
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public MavTextFieldVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
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

    public string? DisplayMemberPath
    {
        get => GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public object? SelectedObject
    {
        get => GetValue(SelectedObjectProperty);
        set => SetValue(SelectedObjectProperty, value);
    }

    public string? SelectedItem
    {
        get => SelectedIndex >= 0 && SelectedIndex < _options.Count ? _options[SelectedIndex] : null;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                SelectedIndex = -1;
                return;
            }
            SelectedIndex = FindOptionIndex(value);
        }
    }

    private int FindOptionIndex(string value)
    {
        for (int i = 0; i < _options.Count; i++)
        {
            if (string.Equals(_options[i], value, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    [Content]
    public AvaloniaList<string> Options => _options;

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Adds an option by its text and returns the field for chaining.</summary>
    public MavSelect AddOption(string option)
    {
        _options.Add(option);
        _rawItems.Add(option);
        return this;
    }

    /// <summary>Adds an option object, shown by its string form, and returns the field for chaining.</summary>
    public MavSelect AddOption(object item)
    {
        _rawItems.Add(item);
        _options.Add(FormatItem(item));
        return this;
    }

    /// <summary>
    /// Adds an option to the select control.
    /// </summary>
    public void Add(string option) => AddOption(option);

    /// <summary>
    /// Clears the selection.
    /// </summary>
    public void Clear() => SelectedIndex = -1;

    /// <summary>
    /// Returns an enumerator over the options.
    /// </summary>
    public IEnumerator<string> GetEnumerator() => _options.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private string FormatItem(object item) =>
        MavVisualTreeHelpers.FormatItemText(item, DisplayMemberPath, _itemFormatter);

    private void OnItemsSourceChanged(IEnumerable? items)
    {
        _collectionObserver.Attach(items);
        RebuildFromItemsSource(items);
    }

    private void RebuildFromItemsSource(IEnumerable? items)
    {
        _options.Clear();
        _rawItems.Clear();
        if (items is not null)
        {
            foreach (var item in items)
            {
                if (item is not null)
                {
                    _rawItems.Add(item);
                    _options.Add(FormatItem(item));
                }
            }
        }
        OnIndexChanged(SelectedIndex);
    }

    /// <summary>Opens the list of options.</summary>
    public void OpenMenu()
    {
        if (!IsEnabled || _options.Count == 0) return;

        _menu.Clear();
        for (int i = 0; i < _options.Count; i++)
        {
            var idx = i;
            var text = _options[i];
            _menu.AddItem(
                text,
                onSelected: () => SelectedIndex = idx,
                isSelected: idx == SelectedIndex);
        }

        _field.TrailingIcon = MavIcons.ArrowDropUp;
        _menu.Show(this);
    }

    private void UpdateTrailingIcon()
    {
        _field.TrailingIcon = (IsClearable && SelectedIndex >= 0)
            ? MavIcons.Cancel
            : MavIcons.ArrowDropDown;
    }

    private void OnIndexChanged(int idx)
    {
        if (_syncingSelection) return;
        _syncingSelection = true;
        try
        {
            _field.Text = idx >= 0 && idx < _options.Count ? _options[idx] : string.Empty;
            var obj = idx >= 0 && idx < _rawItems.Count ? _rawItems[idx] : null;
            SetValue(SelectedObjectProperty, obj);
            UpdateTrailingIcon();
            SelectedIndexChanged?.Invoke(this, idx);
            SelectedObjectChanged?.Invoke(this, obj);
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private void OnSelectedObjectChanged(object? obj)
    {
        if (_syncingSelection) return;
        if (obj is null)
        {
            SelectedIndex = -1;
            return;
        }
        var idx = _rawItems.FindIndex(x => Equals(x, obj));
        if (idx < 0 && obj is string s)
            idx = FindOptionIndex(s);
        if (idx >= 0)
            SelectedIndex = idx;
    }
}
