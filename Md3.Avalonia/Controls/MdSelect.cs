using System.Collections;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Metadata;
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Material Design 3 read-only Select / Exposed Dropdown Menu built on <see cref="MdTextField"/> and <see cref="MdMenu"/>.
/// Supports string options, arbitrary object <see cref="ItemsSource"/> (including <c>ObservableCollection&lt;T&gt;</c>),
/// <see cref="ItemFormatter"/>, and <see cref="IsClearable"/>.
/// </summary>
public class MdSelect : MdContentView, IEnumerable<string>
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MdSelect, string>(nameof(Label), "Select");

    public static readonly StyledProperty<string?> SupportingTextProperty =
        AvaloniaProperty.Register<MdSelect, string?>(nameof(SupportingText));

    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<MdSelect, string?>(nameof(ErrorText));

    public static readonly StyledProperty<MdTextFieldVariant> VariantProperty =
        AvaloniaProperty.Register<MdSelect, MdTextFieldVariant>(nameof(Variant), MdTextFieldVariant.Outlined);

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MdSelect, int>(nameof(SelectedIndex), -1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object?> SelectedObjectProperty =
        AvaloniaProperty.Register<MdSelect, object?>(nameof(SelectedObject), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MdSelect, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<bool> IsClearableProperty =
        AvaloniaProperty.Register<MdSelect, bool>(nameof(IsClearable));

    public static readonly StyledProperty<string?> DisplayMemberPathProperty =
        AvaloniaProperty.Register<MdSelect, string?>(nameof(DisplayMemberPath));

    private readonly AvaloniaList<string> _options = new();
    private readonly List<object> _rawItems = [];
    private readonly MdCollectionObserver _collectionObserver;
    private readonly MdTextField _field;
    private readonly MdMenu _menu;
    private Func<object, string>? _itemFormatter;
    private bool _syncingSelection;

    public event EventHandler<int>? SelectedIndexChanged;
    public event EventHandler<object?>? SelectedObjectChanged;

    static MdSelect()
    {
        LabelProperty.Changed.AddClassHandler<MdSelect>((s, e) =>
        {
            s._field.Label = e.GetNewValue<string>() ?? string.Empty;
            MdAutomation.SetName(s, e.GetNewValue<string>());
        });
        SupportingTextProperty.Changed.AddClassHandler<MdSelect>((s, e) => s._field.SupportingText = e.GetNewValue<string?>());
        ErrorTextProperty.Changed.AddClassHandler<MdSelect>((s, e) => s._field.ErrorText = e.GetNewValue<string?>());
        VariantProperty.Changed.AddClassHandler<MdSelect>((s, e) => s._field.Variant = e.GetNewValue<MdTextFieldVariant>());
        SelectedIndexProperty.Changed.AddClassHandler<MdSelect>((s, e) => s.OnIndexChanged(e.GetNewValue<int>()));
        SelectedObjectProperty.Changed.AddClassHandler<MdSelect>((s, e) => s.OnSelectedObjectChanged(e.GetNewValue<object?>()));
        ItemsSourceProperty.Changed.AddClassHandler<MdSelect>((s, e) => s.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        IsClearableProperty.Changed.AddClassHandler<MdSelect>((s, _) => s.UpdateTrailingIcon());
        DisplayMemberPathProperty.Changed.AddClassHandler<MdSelect>((s, _) =>
        {
            if (s.ItemsSource is not null)
                s.RebuildFromItemsSource(s.ItemsSource);
        });
    }

    public MdSelect() : this(MdLocalization.Select, null)
    {
    }

    public MdSelect(
        string? label = null,
        IEnumerable<string>? options = null,
        MdTextFieldVariant variant = MdTextFieldVariant.Outlined,
        string? leadingIcon = null)
    {
        label ??= MdLocalization.Select;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Cursor = new Cursor(StandardCursorType.Hand);
        _collectionObserver = new MdCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));

        _field = new MdTextField(label, variant, leadingIcon: leadingIcon)
        {
            IsReadOnly = true,
            TrailingIcon = MdIcons.ArrowDropDown,
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

        _menu = new MdMenu();
        _menu.Closed += (_, _) => UpdateTrailingIcon();

        Content = tapBox;

        if (options is not null)
        {
            foreach (var opt in options)
                AddOption(opt);
        }

        MdAutomation.SetControlType(this, AutomationControlType.ComboBox);
        MdAutomation.SetName(this, Label);
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public MdTextFieldVariant Variant
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

    public MdSelect AddOption(string option)
    {
        _options.Add(option);
        _rawItems.Add(option);
        return this;
    }

    public MdSelect AddOption(object item)
    {
        _rawItems.Add(item);
        _options.Add(FormatItem(item));
        return this;
    }

    public void Add(string option) => AddOption(option);

    public void Clear() => SelectedIndex = -1;

    public IEnumerator<string> GetEnumerator() => _options.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private string FormatItem(object item) =>
        MdVisualTreeHelpers.FormatItemText(item, DisplayMemberPath, _itemFormatter);

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

        _field.TrailingIcon = MdIcons.ArrowDropUp;
        _menu.Show(this);
    }

    private void UpdateTrailingIcon()
    {
        _field.TrailingIcon = (IsClearable && SelectedIndex >= 0)
            ? MdIcons.Cancel
            : MdIcons.ArrowDropDown;
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
