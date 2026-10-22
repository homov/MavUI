using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Expressive button group (Standard or Connected with shared inner corners).
/// </summary>
public class MavButtonGroup : StackPanel, IEnumerable<Control>
{
    public static readonly StyledProperty<MavButtonGroupKind> KindProperty =
        AvaloniaProperty.Register<MavButtonGroup, MavButtonGroupKind>(nameof(Kind), MavButtonGroupKind.Standard);

    public static readonly StyledProperty<bool> MultiSelectProperty =
        AvaloniaProperty.Register<MavButtonGroup, bool>(nameof(MultiSelect));

    private readonly MavItemEventWiring<MavButton, EventHandler<bool>> _wiredButtons = new(
        static (button, handler) => button.SelectedChanged += handler,
        static (button, handler) => button.SelectedChanged -= handler);
    private bool _syncingSelection;

    public event EventHandler<int>? SelectionChanged;

    static MavButtonGroup()
    {
        KindProperty.Changed.AddClassHandler<MavButtonGroup>((g, e) =>
        {
            g.Spacing = e.GetNewValue<MavButtonGroupKind>() == MavButtonGroupKind.Connected ? 2 : 8;
            g.UpdateChildRadii();
        });
    }

    /// <summary>Creates a standard button group.</summary>
    public MavButtonGroup() : this(MavButtonGroupKind.Standard)
    {
    }

    /// <summary>Creates a button group with the given layout <paramref name="kind"/>.</summary>
    public MavButtonGroup(MavButtonGroupKind kind = MavButtonGroupKind.Standard)
    {
        Orientation = Orientation.Horizontal;
        Kind = kind;
        Spacing = kind == MavButtonGroupKind.Connected ? 2 : 8;
        HorizontalAlignment = HorizontalAlignment.Left;
        Children.CollectionChanged += (_, e) =>
        {
            _wiredButtons.HandleCollectionChanged(e);
            WireChildButtons();
            UpdateChildRadii();
        };
    }

    public MavButtonGroupKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public MavButtonGroupKind Variant
    {
        get => Kind;
        set => Kind = value;
    }

    public bool MultiSelect
    {
        get => GetValue(MultiSelectProperty);
        set => SetValue(MultiSelectProperty, value);
    }

    private void WireChildButtons()
    {
        foreach (var btn in Children.OfType<MavButton>())
        {
            _wiredButtons.Add(btn, (s, isSelected) =>
            {
                if (_syncingSelection || s is not MavButton clickedBtn) return;
                var buttons = Children.OfType<MavButton>().ToList();
                var idx = buttons.IndexOf(clickedBtn);
                if (!MultiSelect && isSelected)
                {
                    _syncingSelection = true;
                    for (int i = 0; i < buttons.Count; i++)
                    {
                        if (i != idx && buttons[i].IsToggle)
                            buttons[i].IsSelected = false;
                    }
                    _syncingSelection = false;
                }
                if (idx >= 0 && (MultiSelect || isSelected))
                    SelectionChanged?.Invoke(this, idx);
            });
        }
    }

    /// <summary>Adds a button to the group and returns the group for chaining.</summary>
    public MavButtonGroup AddButton(MavButton button)
    {
        Children.Add(button);
        UpdateChildRadii();
        return this;
    }

    public void Add(MavButton button) => AddButton(button);

    public IEnumerator<Control> GetEnumerator() => Children.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void UpdateChildRadii()
    {
        var buttons = Children.OfType<MavButton>().ToList();
        for (int i = 0; i < buttons.Count; i++)
        {
            if (Kind != MavButtonGroupKind.Connected || buttons.Count <= 1)
            {
                buttons[i].SetGroupCornerRadii(null);
                continue;
            }

            const double outer = 20;
            const double inner = 6;
            if (i == 0)
                buttons[i].SetGroupCornerRadii(new CornerRadius(outer, inner, inner, outer));
            else if (i == buttons.Count - 1)
                buttons[i].SetGroupCornerRadii(new CornerRadius(inner, outer, outer, inner));
            else
                buttons[i].SetGroupCornerRadii(new CornerRadius(inner));
        }
    }
}
