using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
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
/// Mutual-exclusion container for <see cref="MavRadioButton"/> items.
/// </summary>
public class MavRadioGroup : StackPanel
{
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MavRadioGroup, int>(nameof(SelectedIndex), -1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<MavRadioGroup, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<System.Collections.IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavRadioGroup, System.Collections.IEnumerable?>(nameof(ItemsSource));

    private readonly List<MavRadioButton> _buttons = [];
    private readonly MavItemEventWiring<MavRadioButton, EventHandler<bool>> _wiredButtons = new(
        static (button, handler) => button.CheckedChanged += handler,
        static (button, handler) => button.CheckedChanged -= handler);
    private readonly MavCollectionObserver _observer;
    private int _selectedIndex = -1;
    private bool _updating;

    public event EventHandler<int>? SelectedIndexChanged;

    static MavRadioGroup()
    {
        SelectedIndexProperty.Changed.AddClassHandler<MavRadioGroup>((g, e) =>
            g.OnSelectedIndexPropertyChanged(e.GetNewValue<int>()));
        SelectedItemProperty.Changed.AddClassHandler<MavRadioGroup>((g, e) =>
            g.OnSelectedItemPropertyChanged(e.GetNewValue<object?>()));
        ItemsSourceProperty.Changed.AddClassHandler<MavRadioGroup>((g, e) =>
            g.OnItemsSourceChanged(e.GetNewValue<System.Collections.IEnumerable?>()));
    }

    /// <summary>Creates a vertical radio group.</summary>
    public MavRadioGroup() : this(Orientation.Vertical)
    {
    }

    /// <summary>Creates a radio group with the given <paramref name="orientation"/>.</summary>
    public MavRadioGroup(Orientation orientation = Orientation.Vertical)
    {
        Orientation = orientation;
        Spacing = 4;
        _observer = new MavCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));
        Children.CollectionChanged += (_, e) =>
        {
            _wiredButtons.HandleCollectionChanged(e);
            SyncChildrenToRadioButtons();
        };
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetValue(SelectedIndexProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public System.Collections.IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private void OnItemsSourceChanged(System.Collections.IEnumerable? src)
    {
        _observer.Attach(src);
        RebuildFromItemsSource(src);
    }

    private void RebuildFromItemsSource(System.Collections.IEnumerable? src)
    {
        if (src is null) return;
        Children.Clear();
        foreach (var item in src)
        {
            if (item is MavRadioButton rb)
                Children.Add(rb);
            else if (item is not null)
                AddOption(item.ToString() ?? string.Empty, item);
        }
    }

    private void OnSelectedIndexPropertyChanged(int value)
    {
        if (_selectedIndex == value) return;
        _selectedIndex = value;
        _updating = true;
        for (int i = 0; i < _buttons.Count; i++)
            _buttons[i].IsChecked = i == value;
        SetValue(SelectedItemProperty, SelectedValue);
        _updating = false;
        SelectedIndexChanged?.Invoke(this, _selectedIndex);
    }

    private void OnSelectedItemPropertyChanged(object? item)
    {
        if (_updating) return;
        if (item is null)
        {
            SelectedIndex = -1;
            return;
        }
        for (int i = 0; i < _buttons.Count; i++)
        {
            var val = _buttons[i].Value ?? _buttons[i].Label;
            if (Equals(val, item) || (item is string s && string.Equals(_buttons[i].Label, s, StringComparison.OrdinalIgnoreCase)))
            {
                SelectedIndex = i;
                return;
            }
        }
    }

    public object? SelectedValue =>
        _selectedIndex >= 0 && _selectedIndex < _buttons.Count
            ? (_buttons[_selectedIndex].Value ?? _buttons[_selectedIndex].Label)
            : null;

    /// <summary>Adds a radio button with the given label and value and returns it.</summary>
    public MavRadioButton AddOption(string label, object? value = null, bool selected = false)
    {
        var rb = new MavRadioButton(label, selected, value);
        AddRadio(rb);
        return rb;
    }

    /// <summary>Adds an existing radio button to the group.</summary>
    public void AddRadio(MavRadioButton button)
    {
        if (!Children.Contains(button))
            Children.Add(button);
        else
            SyncChildrenToRadioButtons();
    }

    private void SyncChildrenToRadioButtons()
    {
        _buttons.Clear();
        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i] is not MavRadioButton rb) continue;
            var idx = _buttons.Count;
            _buttons.Add(rb);
            _wiredButtons.Add(rb, (s, on) =>
            {
                if (_updating || !on || s is not MavRadioButton senderBtn) return;
                var index = _buttons.IndexOf(senderBtn);
                if (index >= 0) SelectedIndex = index;
            });
            if (rb.IsChecked)
            {
                _selectedIndex = idx;
                SetValue(SelectedIndexProperty, idx);
            }
        }
    }
}
