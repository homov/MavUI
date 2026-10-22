using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

public sealed class MdFabMenuItem : Control
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MdFabMenuItem, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MdFabMenuItem, string>(nameof(Icon), MdIcons.Add);

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MdFabMenuItem, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MdFabMenuItem, object?>(nameof(CommandParameter));

    internal event EventHandler? ItemChanged;

    static MdFabMenuItem()
    {
        LabelProperty.Changed.AddClassHandler<MdFabMenuItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MdFabMenuItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public Action? OnClick { get; set; }
    public event EventHandler? Clicked;

    internal void RaiseClicked() => Clicked?.Invoke(this, EventArgs.Empty);

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public MdFabMenuItem()
    {
    }

    public MdFabMenuItem(string label, string icon, Action? onClick = null)
    {
        Label = label;
        Icon = icon;
        OnClick = onClick;
    }

    protected override Size MeasureOverride(Size availableSize) => default;
}

/// <summary>
/// Material Design 3 Expressive FAB Speed-Dial Menu that expands labeled action pills upward from a primary FAB.
/// </summary>
public class MdFabMenu : MdContentView, IEnumerable<MdFabMenuItem>
{
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MdFabMenu, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> ClosedIconProperty =
        AvaloniaProperty.Register<MdFabMenu, string>(nameof(ClosedIcon), MdIcons.Add);

    public static readonly StyledProperty<string> OpenIconProperty =
        AvaloniaProperty.Register<MdFabMenu, string>(nameof(OpenIcon), MdIcons.Close);

    public static readonly StyledProperty<MdFabColor> ColorSchemeProperty =
        AvaloniaProperty.Register<MdFabMenu, MdFabColor>(nameof(ColorScheme), MdFabColor.Primary);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MdFabMenu, IEnumerable?>(nameof(ItemsSource));

    private readonly AvaloniaList<MdFabMenuItem> _items = new();
    private readonly HashSet<MdFabMenuItem> _wiredItems = new();
    private readonly MdCollectionObserver _observer;
    private readonly StackPanel _itemsHost;
    private readonly MdInkSurface _trigger;
    private readonly MdIcon _triggerIcon;
    private string _closedIcon = MdIcons.Add;
    private string _openIcon = MdIcons.Close;
    private bool _isOpen;

    public event EventHandler<int>? ItemClicked;
    public event EventHandler<bool>? OpenChanged;

    static MdFabMenu()
    {
        IsOpenProperty.Changed.AddClassHandler<MdFabMenu>((m, e) =>
        {
            var val = e.GetNewValue<bool>();
            if (m._isOpen == val) return;
            m._isOpen = val;
            m.AnimateOpen(val);
            m.OpenChanged?.Invoke(m, val);
        });
        ClosedIconProperty.Changed.AddClassHandler<MdFabMenu>((m, e) =>
        {
            m._closedIcon = e.GetNewValue<string>() ?? MdIcons.Add;
            if (!m._isOpen) m._triggerIcon.Glyph = m._closedIcon;
        });
        OpenIconProperty.Changed.AddClassHandler<MdFabMenu>((m, e) =>
        {
            m._openIcon = e.GetNewValue<string>() ?? MdIcons.Close;
            if (m._isOpen) m._triggerIcon.Glyph = m._openIcon;
        });
        ItemsSourceProperty.Changed.AddClassHandler<MdFabMenu>((m, e) =>
            m.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
    }

    public MdFabMenu() : this(MdIcons.Add)
    {
    }

    public MdFabMenu(string icon = MdIcons.Add)
    {
        _closedIcon = icon;
        ClosedIcon = icon;
        HorizontalAlignment = HorizontalAlignment.Right;
        VerticalAlignment = VerticalAlignment.Bottom;
        _observer = new MdCollectionObserver(() => RebuildFromItemsSource(ItemsSource));
        _items.CollectionChanged += (_, _) =>
        {
            foreach (var item in _items)
            {
                MdVisualTreeHelpers.AttachLogicalChild(this, item);
                if (_wiredItems.Add(item))
                    item.ItemChanged += (_, _) => RebuildItems();
            }
            RebuildItems();
        };

        _itemsHost = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right,
            IsVisible = false,
            Margin = new Thickness(0, 0, 0, 12),
        };

        _triggerIcon = new MdIcon(_closedIcon, 24) { UseThemeDefaultColor = false };
        _trigger = new MdInkSurface
        {
            Width = 56,
            Height = 56,
            CornerRadius = 16,
            Elevation = 3,
            HorizontalAlignment = HorizontalAlignment.Right,
            Content = _triggerIcon,
        };
        _trigger.Clicked += (_, _) => Toggle();

        var root = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        root.Children.Add(_itemsHost);
        root.Children.Add(_trigger);

        Content = root;
        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public string ClosedIcon
    {
        get => GetValue(ClosedIconProperty);
        set => SetValue(ClosedIconProperty, value);
    }

    public string Icon
    {
        get => ClosedIcon;
        set => ClosedIcon = value;
    }

    public string OpenIcon
    {
        get => GetValue(OpenIconProperty);
        set => SetValue(OpenIconProperty, value);
    }

    public MdFabColor ColorScheme
    {
        get => GetValue(ColorSchemeProperty);
        set => SetValue(ColorSchemeProperty, value);
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
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
            if (item is MdFabMenuItem fmi)
                _items.Add(fmi);
            else if (item is not null)
                _items.Add(new MdFabMenuItem(item.ToString() ?? string.Empty, MdIcons.Add));
        }
    }

    [Content]
    public AvaloniaList<MdFabMenuItem> Items => _items;

    public MdFabMenu AddItem(string label, string icon, Action? onClick = null)
    {
        _items.Add(new MdFabMenuItem(label, icon, onClick));
        return this;
    }

    public void Add(MdFabMenuItem item) => _items.Add(item);

    public IEnumerator<MdFabMenuItem> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Toggle() => IsOpen = !IsOpen;
    public void Open() => IsOpen = true;
    public void Close() => IsOpen = false;

    private void RebuildItems()
    {
        _itemsHost.Children.Clear();
        for (int i = 0; i < _items.Count; i++)
        {
            var idx = i;
            var item = _items[i];
            var pill = BuildItemPill(item, idx);
            _itemsHost.Children.Add(pill);
        }
        ApplyTheme(MdTheme.Current);
    }

    private MdInkSurface BuildItemPill(MdFabMenuItem item, int index)
    {
        var icon = new MdIcon(item.Icon, 20) { UseThemeDefaultColor = false };
        var label = new TextBlock
        {
            Text = item.Label,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
        };

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.Children.Add(icon);
        row.Children.Add(label);

        var pill = new MdInkSurface
        {
            MinHeight = 44,
            CornerRadius = 22,
            Padding = new Thickness(16, 0, 20, 0),
            Elevation = 2,
            HorizontalAlignment = HorizontalAlignment.Right,
            Content = row,
        };
        pill.Clicked += (_, _) =>
        {
            IsOpen = false;
            item.OnClick?.Invoke();
            item.RaiseClicked();
            if (item.Command is { } cmd && cmd.CanExecute(item.CommandParameter))
                cmd.Execute(item.CommandParameter);
            ItemClicked?.Invoke(this, index);
        };
        return pill;
    }

    private void AnimateOpen(bool open)
    {
        _triggerIcon.Glyph = open ? _openIcon : _closedIcon;
        MdMotion.Animate(this, "fab_rot", _triggerIcon.GetRotation(), open ? 90 : 0, MdMotion.Short4, MdMotion.CubicOut, v =>
        {
            _triggerIcon.SetRotation(v);
        });
        ApplyTheme(MdTheme.Current);

        if (open)
        {
            _itemsHost.IsVisible = true;
            _itemsHost.Opacity = 0;
            _itemsHost.SetTranslationY(12);
            MdMotion.Animate(this, "fab_items", 0, 1, MdMotion.Medium1, MdMotion.CubicOut, v =>
            {
                _itemsHost.Opacity = v;
                _itemsHost.SetTranslationY(12 * (1 - v));
            });
        }
        else
        {
            MdMotion.Animate(this, "fab_items", 1, 0, MdMotion.Short3, MdMotion.CubicIn, v =>
            {
                _itemsHost.Opacity = v;
                _itemsHost.SetTranslationY(8 * (1 - v));
            }, () => _itemsHost.IsVisible = false);
        }
    }

    private void ApplyTheme(MdTheme t)
    {
        var c = t.Color;
        _trigger.FillColor = _isOpen ? c.Primary : c.PrimaryContainer;
        _trigger.RippleColor = _isOpen ? c.OnPrimary : c.OnPrimaryContainer;
        _trigger.CornerRadius = _isOpen ? 28 : 16;
        _triggerIcon.Color = _isOpen ? c.OnPrimary : c.OnPrimaryContainer;

        foreach (var child in _itemsHost.Children.OfType<MdInkSurface>())
        {
            child.FillColor = c.SecondaryContainer;
            child.RippleColor = c.OnSecondaryContainer;
            if (child.Content is StackPanel row)
            {
                foreach (var el in row.Children)
                {
                    if (el is MdIcon ic) ic.Color = c.OnSecondaryContainer;
                    else if (el is TextBlock lbl)
                    {
                        lbl.Apply(MdTypeRole.LabelLarge, t.Typography);
                        lbl.Foreground = MdTheme.BrushOf(c.OnSecondaryContainer);
                    }
                }
            }
        }
    }
}
