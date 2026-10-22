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

public enum MavChipKind
{
    Assist,
    Filter,
    Input,
    Suggestion,
}

/// <summary>
/// Material Design 3 chip (Assist, Filter, Input, Suggestion) with optional leading icon,
/// avatar, selection check, close button, and elevated style.
/// </summary>
public class MavChip : MavContentView
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavChip, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MavChip, string?>(nameof(Icon));

    public static readonly StyledProperty<MavChipKind> KindProperty =
        AvaloniaProperty.Register<MavChip, MavChipKind>(nameof(Kind), MavChipKind.Assist);

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MavChip, bool>(nameof(IsSelected), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsElevatedProperty =
        AvaloniaProperty.Register<MavChip, bool>(nameof(IsElevated));

    public static readonly StyledProperty<bool> ShowCloseIconProperty =
        AvaloniaProperty.Register<MavChip, bool>(nameof(ShowCloseIcon));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavChip, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavChip, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<ICommand?> CloseCommandProperty =
        AvaloniaProperty.Register<MavChip, ICommand?>(nameof(CloseCommand));

    public static readonly StyledProperty<object?> CloseCommandParameterProperty =
        AvaloniaProperty.Register<MavChip, object?>(nameof(CloseCommandParameter));

    public static readonly StyledProperty<Control?> AvatarViewProperty =
        AvaloniaProperty.Register<MavChip, Control?>(nameof(AvatarView));

    private readonly MavInkSurface _surface;
    private readonly Border _avatarHost;
    private readonly MavIcon _leadingIcon;
    private readonly TextBlock _label;
    private readonly Border _closeHit;
    private readonly MavIcon _closeIcon;
    private Control? _avatarView;

    public event EventHandler? Clicked;
    public event EventHandler<bool>? SelectedChanged;
    public event EventHandler? CloseClicked;

    static MavChip()
    {
        TextProperty.Changed.AddClassHandler<MavChip>((c, _) =>
        {
            c.Sync();
            MavAutomation.SetName(c, c.Text);
        });
        IconProperty.Changed.AddClassHandler<MavChip>((c, _) => c.Sync());
        KindProperty.Changed.AddClassHandler<MavChip>((c, _) => c.Sync());
        IsSelectedProperty.Changed.AddClassHandler<MavChip>((c, e) =>
        {
            c.Sync();
            c.SelectedChanged?.Invoke(c, e.GetNewValue<bool>());
        });
        IsElevatedProperty.Changed.AddClassHandler<MavChip>((c, _) => c.ApplyTheme(MavTheme.Current));
        ShowCloseIconProperty.Changed.AddClassHandler<MavChip>((c, _) => c.Sync());
        CommandProperty.Changed.AddClassHandler<MavChip>((c, e) => c._surface.Command = e.GetNewValue<ICommand?>());
        CommandParameterProperty.Changed.AddClassHandler<MavChip>((c, e) => c._surface.CommandParameter = e.GetNewValue<object?>());
        AvatarViewProperty.Changed.AddClassHandler<MavChip>((c, e) =>
        {
            c._avatarView = e.GetNewValue<Control?>();
            MavVisualTreeHelpers.DetachFromParent(c._avatarView);
            c.Sync();
        });
    }

    public MavChip() : this(string.Empty)
    {
    }

    public MavChip(
        string text = "",
        MavChipKind kind = MavChipKind.Assist,
        string? icon = null,
        bool selected = false,
        bool elevated = false)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;

        _avatarHost = new Border
        {
            Width = 24,
            Height = 24,
            CornerRadius = new CornerRadius(12),
            ClipToBounds = true,
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = false,
        };
        _leadingIcon = new MavIcon { Size = 18, IsVisible = false, UseThemeDefaultColor = false };
        _label = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
        };
        _closeIcon = new MavIcon(MavIcons.Close, 18) { UseThemeDefaultColor = false };
        _closeHit = new Border
        {
            Width = 22,
            Height = 22,
            Background = Brushes.Transparent,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand),
            IsVisible = false,
            Child = _closeIcon,
        };
        _closeHit.PointerPressed += (_, e) => e.Handled = true;
        _closeHit.PointerReleased += (_, e) =>
        {
            e.Handled = true;
            CloseClicked?.Invoke(this, EventArgs.Empty);
            if (CloseCommand is { } cCmd && cCmd.CanExecute(CloseCommandParameter))
                cCmd.Execute(CloseCommandParameter);
        };

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.Children.Add(_avatarHost);
        row.Children.Add(_leadingIcon);
        row.Children.Add(_label);
        row.Children.Add(_closeHit);

        _surface = new MavInkSurface
        {
            MinHeight = 32,
            CornerRadius = 8,
            Padding = new Thickness(12, 0),
            Content = row,
        };
        _surface.Clicked += (_, _) =>
        {
            if (AutoToggleOnClick && Kind is MavChipKind.Filter or MavChipKind.Input)
                IsSelected = !IsSelected;
            Clicked?.Invoke(this, EventArgs.Empty);
        };

        Content = _surface;
        Text = text;
        Kind = kind;
        Icon = icon;
        IsSelected = selected;
        IsElevated = elevated;
        if (kind == MavChipKind.Input)
            ShowCloseIcon = true;

        Sync();
        MavAutomation.SetName(this, Text);
        MavAutomation.SetControlType(this, AutomationControlType.Button);
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    internal bool AutoToggleOnClick { get; set; } = true;

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? LeadingIcon
    {
        get => Icon;
        set => Icon = value;
    }

    public MavChipKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public MavChipKind Variant
    {
        get => Kind;
        set => Kind = value;
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsElevated
    {
        get => GetValue(IsElevatedProperty);
        set => SetValue(IsElevatedProperty, value);
    }

    public bool ShowCloseIcon
    {
        get => GetValue(ShowCloseIconProperty);
        set => SetValue(ShowCloseIconProperty, value);
    }

    public bool IsRemovable
    {
        get => ShowCloseIcon;
        set => ShowCloseIcon = value;
    }

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

    public ICommand? CloseCommand
    {
        get => GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public object? CloseCommandParameter
    {
        get => GetValue(CloseCommandParameterProperty);
        set => SetValue(CloseCommandParameterProperty, value);
    }

    public Control? AvatarView
    {
        get => GetValue(AvatarViewProperty);
        set => SetValue(AvatarViewProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEnabledProperty)
        {
            _surface.IsEnabled = IsEnabled;
            ApplyTheme(MavTheme.Current);
        }
    }

    private void Sync()
    {
        _label.Text = Text;

        var hasAvatar = _avatarView is not null && !(Kind == MavChipKind.Filter && IsSelected);
        if (hasAvatar)
        {
            if (!ReferenceEquals(_avatarHost.Child, _avatarView))
            {
                _avatarHost.Child = null;
                MavVisualTreeHelpers.DetachFromParent(_avatarView);
                _avatarHost.Child = _avatarView;
            }
        }
        else
        {
            _avatarHost.Child = null;
        }
        _avatarHost.IsVisible = hasAvatar;

        string? leadGlyph = Icon;
        if (Kind == MavChipKind.Filter && IsSelected)
            leadGlyph = MavIcons.Check;

        var hasLead = !hasAvatar && !string.IsNullOrEmpty(leadGlyph);
        _leadingIcon.Glyph = leadGlyph;
        _leadingIcon.IsVisible = hasLead;

        var hasClose = ShowCloseIcon || Kind == MavChipKind.Input;
        _closeHit.IsVisible = hasClose;

        var leftPad = hasAvatar ? 4.0 : (hasLead ? 8.0 : 12.0);
        var rightPad = hasClose ? 8.0 : 12.0;
        _surface.Padding = new Thickness(leftPad, 0, rightPad, 0);

        ApplyTheme(MavTheme.Current);
    }

    private void ApplyTheme(MavTheme t)
    {
        _label.Apply(MavTypeRole.LabelLarge, t.Typography);
        _surface.MinHeight = Math.Max(28, 32 + t.DensityOffset * 0.5);
        _surface.CornerRadius = t.Shape.Small;

        var c = t.Color;
        Color bg;
        Color fg;
        Color iconCol;
        Color stroke;
        double strokeW;
        double elev = IsElevated ? 1.0 : 0.0;

        if (!IsEnabled)
        {
            bg = IsElevated || IsSelected ? c.OnSurface.WithAlpha(t.StateLayer.DisabledContainer) : Colors.Transparent;
            fg = c.OnSurface.WithAlpha(t.StateLayer.DisabledContent);
            iconCol = fg;
            stroke = IsElevated || IsSelected ? Colors.Transparent : c.OnSurface.WithAlpha(t.StateLayer.DisabledContainer);
            strokeW = stroke.A > 0 ? 1 : 0;
            elev = 0;
        }
        else if (IsSelected)
        {
            bg = c.SecondaryContainer;
            fg = c.OnSecondaryContainer;
            iconCol = c.OnSecondaryContainer;
            stroke = Colors.Transparent;
            strokeW = 0;
        }
        else if (IsElevated)
        {
            bg = c.SurfaceContainerLow;
            fg = c.OnSurface;
            iconCol = c.Primary;
            stroke = Colors.Transparent;
            strokeW = 0;
        }
        else
        {
            bg = Colors.Transparent;
            fg = c.OnSurfaceVariant;
            iconCol = Kind == MavChipKind.Assist ? c.Primary : c.OnSurfaceVariant;
            stroke = c.OutlineVariant;
            strokeW = 1;
        }

        _surface.FillColor = bg;
        _surface.StrokeColor = stroke;
        _surface.StrokeThickness = strokeW;
        _surface.RippleColor = fg;
        _surface.Elevation = elev;
        _label.Foreground = MavTheme.BrushOf(fg);
        _leadingIcon.Color = iconCol;
        _closeIcon.Color = fg;
    }
}

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

    public MavChipGroup() : this(false, false, 8)
    {
    }

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
            Content = _linePanel,
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

    public void Add(
        string text,
        MavChipKind kind = MavChipKind.Filter,
        string? icon = null,
        bool selected = false,
        bool elevated = false) => AddChip(text, kind, icon, selected, elevated);

    public void Add(MavChip chip) => AddChip(chip);

    public IEnumerator<MavChip> GetEnumerator() => _chips.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    private void RebuildHost()
    {
        _wrapPanel.Children.Clear();
        _linePanel.Children.Clear();

        foreach (var chip in _chips)
            AttachChipToPanel(chip);

        Content = SingleLine ? _lineScroller : _wrapPanel;
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
