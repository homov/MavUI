using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Material Design 3 collapsible accordion panel with animated chevron and body container.
/// </summary>
public class MdExpansionPanel : MdContentView
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MdExpansionPanel, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<MdExpansionPanel, string?>(nameof(Subtitle));

    public static readonly StyledProperty<string?> LeadingIconProperty =
        AvaloniaProperty.Register<MdExpansionPanel, string?>(nameof(LeadingIcon));

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<MdExpansionPanel, bool>(nameof(IsExpanded));

    public static readonly StyledProperty<Control?> BodyProperty =
        AvaloniaProperty.Register<MdExpansionPanel, Control?>(nameof(Body));

    private readonly MdSurface _card;
    private readonly MdInkSurface _header;
    private readonly MdIcon _leadIcon;
    private readonly TextBlock _titleLabel;
    private readonly TextBlock _subtitleLabel;
    private readonly MdIcon _chevron;
    private readonly MdDivider _divider;
    private readonly Border _bodyHost;

    public event EventHandler<bool>? ExpandedChanged;

    static MdExpansionPanel()
    {
        TitleProperty.Changed.AddClassHandler<MdExpansionPanel>((p, _) => p.Sync());
        SubtitleProperty.Changed.AddClassHandler<MdExpansionPanel>((p, _) => p.Sync());
        LeadingIconProperty.Changed.AddClassHandler<MdExpansionPanel>((p, _) => p.Sync());
        IsExpandedProperty.Changed.AddClassHandler<MdExpansionPanel>((p, e) => p.OnExpandedChanged(e.GetNewValue<bool>()));
        BodyProperty.Changed.AddClassHandler<MdExpansionPanel>((p, e) =>
        {
            var body = e.GetNewValue<Control?>();
            MdVisualTreeHelpers.DetachFromParent(body);
            p._bodyHost.Child = body;
        });
    }

    public MdExpansionPanel() : this(string.Empty)
    {
    }

    public MdExpansionPanel(
        string title = "",
        string? subtitle = null,
        Control? body = null,
        bool isExpanded = false,
        string? leadingIcon = null)
    {
        _leadIcon = new MdIcon { Size = 22, IsVisible = false, UseThemeDefaultColor = false, Margin = new Thickness(0, 0, 12, 0) };
        _titleLabel = new TextBlock { TextWrapping = TextWrapping.NoWrap };
        _subtitleLabel = new TextBlock { IsVisible = false, TextWrapping = TextWrapping.NoWrap };

        var titles = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
        };
        titles.Children.Add(_titleLabel);
        titles.Children.Add(_subtitleLabel);

        _chevron = new MdIcon(MdIcons.ExpandMore, 24)
        {
            UseThemeDefaultColor = false,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var headerRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
        };
        headerRow.Add(_leadIcon, 0, 0);
        headerRow.Add(titles, 1, 0);
        headerRow.Add(_chevron, 2, 0);

        _header = new MdInkSurface
        {
            MinHeight = 56,
            CornerRadius = 12,
            Padding = new Thickness(16, 12),
            Content = headerRow,
        };
        _header.Clicked += (_, _) => IsExpanded = !IsExpanded;

        _divider = new MdDivider { IsVisible = isExpanded };
        _bodyHost = new Border
        {
            Padding = new Thickness(16, 12, 16, 16),
            IsVisible = isExpanded,
            Child = body,
        };

        var stack = new StackPanel { Orientation = Orientation.Vertical };
        stack.Children.Add(_header);
        stack.Children.Add(_divider);
        stack.Children.Add(_bodyHost);

        _card = new MdSurface
        {
            Radius = 12,
            BorderStrokeThickness = 1,
            ClipToBounds = true,
            Content = stack,
        };

        base.Content = _card;

        Title = title;
        Subtitle = subtitle;
        LeadingIcon = leadingIcon;
        if (body is not null) Body = body;
        if (isExpanded)
        {
            _chevron.SetRotation(180);
            SetValue(IsExpandedProperty, true);
        }

        Sync();
        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Header
    {
        get => Title;
        set => Title = value;
    }

    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string? LeadingIcon
    {
        get => GetValue(LeadingIconProperty);
        set => SetValue(LeadingIconProperty, value);
    }

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    [Content]
    public Control? Body
    {
        get => GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public new Control? Content
    {
        get => Body;
        set => Body = value;
    }

    private void Sync()
    {
        _titleLabel.Text = Title;
        _subtitleLabel.Text = Subtitle ?? string.Empty;
        _subtitleLabel.IsVisible = !string.IsNullOrEmpty(Subtitle);
        _leadIcon.Glyph = LeadingIcon;
        _leadIcon.IsVisible = !string.IsNullOrEmpty(LeadingIcon);
    }

    private void OnExpandedChanged(bool expanded)
    {
        MdMotion.Animate(this, "exp_chev", _chevron.GetRotation(), expanded ? 180 : 0, MdMotion.Short4, MdMotion.CubicOut, v =>
        {
            _chevron.SetRotation(v);
        });

        _divider.IsVisible = expanded;
        if (expanded)
        {
            _bodyHost.IsVisible = true;
            _bodyHost.Opacity = 0;
            MdMotion.Animate(this, "exp_body", 0, 1, MdMotion.Short4, MdMotion.CubicOut, v => _bodyHost.Opacity = v);
        }
        else
        {
            MdMotion.Animate(this, "exp_body", _bodyHost.Opacity, 0, MdMotion.Short3, MdMotion.CubicIn, v => _bodyHost.Opacity = v, () =>
            {
                if (!IsExpanded) _bodyHost.IsVisible = false;
            });
        }

        ApplyTheme(MdTheme.Current);
        ExpandedChanged?.Invoke(this, expanded);
    }

    private void ApplyTheme(MdTheme t)
    {
        var c = t.Color;
        _titleLabel.Apply(MdTypeRole.TitleSmall, t.Typography);
        _subtitleLabel.Apply(MdTypeRole.BodySmall, t.Typography);

        _card.FillColor = IsExpanded ? c.SurfaceContainerLow : c.Surface;
        _card.BorderStrokeColor = c.OutlineVariant;
        var innerR = Math.Max(0, _card.Radius - _card.BorderStrokeThickness);
        _header.CornerRadii = IsExpanded ? new CornerRadius(innerR, innerR, 0, 0) : new CornerRadius(innerR);
        _header.RippleColor = c.OnSurface;
        _titleLabel.Foreground = MdTheme.BrushOf(c.OnSurface);
        _subtitleLabel.Foreground = MdTheme.BrushOf(c.OnSurfaceVariant);
        _leadIcon.Color = IsExpanded ? c.Primary : c.OnSurfaceVariant;
        _chevron.Color = c.OnSurfaceVariant;
    }
}

/// <summary>
/// Accordion group of <see cref="MdExpansionPanel"/> items with optional single-expand behavior.
/// </summary>
public class MdExpansionPanelList : StackPanel, System.Collections.Generic.IEnumerable<Control>
{
    public static readonly StyledProperty<bool> SingleExpandProperty =
        AvaloniaProperty.Register<MdExpansionPanelList, bool>(nameof(SingleExpand));

    private readonly List<MdExpansionPanel> _panels = [];
    private bool _updating;

    public MdExpansionPanelList() : this(false)
    {
    }

    public MdExpansionPanelList(bool singleExpand = false)
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

    public MdExpansionPanelList AddPanel(MdExpansionPanel panel)
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
            if (item is MdExpansionPanel panel && !_panels.Contains(panel))
            {
                _panels.Add(panel);
                panel.ExpandedChanged += OnPanelExpandedChanged;
            }
        }
    }

    private void OnPanelExpandedChanged(object? sender, bool expanded)
    {
        if (_updating || !SingleExpand || !expanded || sender is not MdExpansionPanel active) return;
        _updating = true;
        foreach (var p in _panels)
        {
            if (!ReferenceEquals(p, active))
                p.IsExpanded = false;
        }
        _updating = false;
    }
}
