using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Expandable Settings Group with header card and collapsible child items.
/// </summary>
public class MavSettingsExpander : MavContentView, System.Collections.Generic.IEnumerable<Control>
{
    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<MavSettingsExpander, string>(nameof(Header), string.Empty);

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<MavSettingsExpander, string?>(nameof(Description));

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MavSettingsExpander, string?>(nameof(Icon));

    public static readonly StyledProperty<Control?> HeaderActionProperty =
        AvaloniaProperty.Register<MavSettingsExpander, Control?>(nameof(HeaderAction));

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<MavSettingsExpander, bool>(
            nameof(IsExpanded),
            false,
            defaultBindingMode: BindingMode.TwoWay);

    private readonly AvaloniaList<Control> _items = new();
    private readonly MavSurface _container;
    private readonly MavInkSurface _headerSurface;
    private readonly MavSurface _iconBox;
    private readonly MavIcon _iconView;
    private readonly TextBlock _headerLbl;
    private readonly TextBlock _descLbl;
    private readonly Border _headerActionHost;
    private readonly MavIcon _chevron;
    private readonly StackPanel _itemsStack;
    private readonly Border _itemsWrapper;

    public event EventHandler<bool>? ExpandedChanged;

    static MavSettingsExpander()
    {
        HeaderProperty.Changed.AddClassHandler<MavSettingsExpander>((ex, e) => ex._headerLbl.Text = e.GetNewValue<string>());
        DescriptionProperty.Changed.AddClassHandler<MavSettingsExpander>((ex, e) =>
        {
            var val = e.GetNewValue<string?>();
            ex._descLbl.Text = val ?? string.Empty;
            ex._descLbl.IsVisible = !string.IsNullOrEmpty(val);
        });
        IconProperty.Changed.AddClassHandler<MavSettingsExpander>((ex, e) =>
        {
            var val = e.GetNewValue<string?>();
            ex._iconView.Glyph = val;
            ex._iconBox.IsVisible = !string.IsNullOrEmpty(val);
        });
        HeaderActionProperty.Changed.AddClassHandler<MavSettingsExpander>((ex, e) =>
        {
            var ctrl = e.GetNewValue<Control?>();
            MavVisualTreeHelpers.DetachFromParent(ctrl);
            ex._headerActionHost.Child = ctrl;
            ex._headerActionHost.IsVisible = ctrl is not null;
        });
        IsExpandedProperty.Changed.AddClassHandler<MavSettingsExpander>((ex, e) =>
            ex.OnExpandedChanged(e.GetNewValue<bool>()));
    }

    /// <summary>Creates an empty settings expander.</summary>
    public MavSettingsExpander() : this(string.Empty)
    {
    }

    /// <summary>Creates a settings expander with a title, description and icon.</summary>
    public MavSettingsExpander(
        string header = "",
        string? description = null,
        string? icon = null,
        Control? headerAction = null,
        bool isExpanded = false)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _iconView = new MavIcon(icon, 22) { UseThemeDefaultColor = false };
        _iconBox = new MavSurface
        {
            Width = 40,
            Height = 40,
            Radius = 12,
            Margin = new Thickness(0, 0, 16, 0),
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = !string.IsNullOrEmpty(icon),
            Content = _iconView,
        };

        _headerLbl = new TextBlock { Text = header, TextWrapping = TextWrapping.Wrap };
        _descLbl = new TextBlock
        {
            Text = description ?? string.Empty,
            TextWrapping = TextWrapping.Wrap,
            IsVisible = !string.IsNullOrEmpty(description),
        };

        var textCol = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
        };
        textCol.Children.Add(_headerLbl);
        textCol.Children.Add(_descLbl);

        _headerActionHost = new Border
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 4, 0),
            IsVisible = headerAction is not null,
            Child = headerAction,
        };

        _chevron = new MavIcon(MavIcons.ExpandMore, 22)
        {
            UseThemeDefaultColor = false,
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };

        var headerRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        headerRow.Add(_iconBox, 0, 0);
        headerRow.Add(textCol, 1, 0);
        headerRow.Add(_headerActionHost, 2, 0);
        headerRow.Add(_chevron, 3, 0);

        _headerSurface = new MavInkSurface
        {
            MinHeight = 68,
            Padding = new Thickness(16, 14),
            Content = headerRow,
        };
        _headerSurface.Clicked += (_, _) => IsExpanded = !IsExpanded;

        _itemsStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0,
        };

        _itemsWrapper = new Border
        {
            IsVisible = isExpanded,
            ClipToBounds = true,
            Child = _itemsStack,
        };

        var rootCol = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0,
        };
        rootCol.Children.Add(_headerSurface);
        rootCol.Children.Add(_itemsWrapper);

        _container = new MavSurface
        {
            Radius = 16,
            BorderStrokeThickness = 1,
            ClipToBounds = true,
            Content = rootCol,
        };

        Content = _container;

        Header = header;
        if (description is not null) Description = description;
        if (icon is not null) Icon = icon;
        if (headerAction is not null) HeaderAction = headerAction;

        _items.CollectionChanged += (_, _) => RebuildItemsStack();

        if (isExpanded)
        {
            SetValue(IsExpandedProperty, true);
            _chevron.SetRotation(180);
        }

        UpdateHeaderCornerRadii();
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string Title
    {
        get => Header;
        set => Header = value;
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public Control? HeaderAction
    {
        get => GetValue(HeaderActionProperty);
        set => SetValue(HeaderActionProperty, value);
    }

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    [Content]
    public AvaloniaList<Control> Items => _items;

    public void Add(Control item) => _items.Add(item);

    public System.Collections.Generic.IEnumerator<Control> GetEnumerator() => _items.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Adds a row to the expanded section and returns the expander for chaining.</summary>
    public MavSettingsExpander AddItem(
        string title,
        string? subtitle = null,
        Control? trailingControl = null,
        string? leadingIcon = null)
    {
        var item = new MavListItem(
            title,
            subtitle,
            leadingIcon: leadingIcon,
            trailingView: trailingControl)
        {
            IsInteractive = trailingControl is null,
        };
        _items.Add(item);
        return this;
    }

    /// <summary>Adds custom content to the expanded section and returns the expander for chaining.</summary>
    public MavSettingsExpander AddCustomContent(Control content)
    {
        _items.Add(new Border
        {
            Padding = new Thickness(16, 12),
            Child = content,
        });
        return this;
    }

    private void RebuildItemsStack()
    {
        _itemsStack.Children.Clear();
        foreach (var ctrl in _items)
        {
            MavVisualTreeHelpers.DetachFromParent(ctrl);
            _itemsStack.Children.Add(new MavDivider());
            _itemsStack.Children.Add(ctrl);
        }
    }

    private void OnExpandedChanged(bool expanded)
    {
        _itemsWrapper.IsVisible = expanded;
        UpdateHeaderCornerRadii();
        var fromRot = expanded ? 0.0 : 180.0;
        var toRot = expanded ? 180.0 : 0.0;
        MavMotion.Animate(_chevron, "sexp_rot", fromRot, toRot, MavMotion.Short4, MavMotion.CubicOut, v =>
        {
            _chevron.SetRotation(v);
        });

        if (expanded)
            MavMotion.FadeIn(_itemsWrapper, MavMotion.Short3);

        ExpandedChanged?.Invoke(this, expanded);
    }

    private void UpdateHeaderCornerRadii()
    {
        var r = Math.Max(0, _container.Radius - _container.BorderStrokeThickness);
        _headerSurface.CornerRadii = IsExpanded
            ? new CornerRadius(r, r, 0, 0)
            : new CornerRadius(r);
        _itemsWrapper.CornerRadius = new CornerRadius(0, 0, r, r);
    }

    private void ApplyTheme(MavTheme t)
    {
        var c = t.Color;
        _headerLbl.Apply(MavTypeRole.TitleMedium, t.Typography);
        _descLbl.Apply(MavTypeRole.BodyMedium, t.Typography);

        _headerLbl.Foreground = MavTheme.BrushOf(c.OnSurface);
        _descLbl.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);

        _iconBox.FillColor = c.SecondaryContainer;
        _iconView.Color = c.OnSecondaryContainer;
        _chevron.Color = c.OnSurfaceVariant;

        _container.Radius = t.Shape.Large;
        _container.FillColor = c.SurfaceContainerLow;
        _container.BorderStrokeColor = c.OutlineVariant;
        _headerSurface.RippleColor = c.OnSurface;
        UpdateHeaderCornerRadii();
    }
}
