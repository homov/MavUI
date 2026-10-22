using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

public enum MdToolbarVariant
{
    Floating,
    Docked,
}

public enum MdToolbarColorStyle
{
    Standard,
    Vibrant,
}

/// <summary>
/// Material Design 3 Expressive Toolbar supporting Floating pill (64dp height, full-rounded, elevation 3)
/// and Docked full-width configurations in Standard or Vibrant color styles, with optional companion FAB.
/// </summary>
public class MdToolbar : MdContentView, IEnumerable<Control>
{
    public static readonly StyledProperty<MdToolbarVariant> VariantProperty =
        AvaloniaProperty.Register<MdToolbar, MdToolbarVariant>(nameof(Variant), MdToolbarVariant.Floating);

    public static readonly StyledProperty<MdToolbarColorStyle> ColorStyleProperty =
        AvaloniaProperty.Register<MdToolbar, MdToolbarColorStyle>(nameof(ColorStyle), MdToolbarColorStyle.Standard);

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<MdToolbar, Orientation>(nameof(Orientation), Orientation.Horizontal);

    public static readonly StyledProperty<Control?> CompanionFabProperty =
        AvaloniaProperty.Register<MdToolbar, Control?>(nameof(CompanionFab));

    private readonly StackPanel _outerRow;
    private readonly MdSurface _barSurface;
    private readonly StackPanel _itemsPanel;
    private readonly List<MdIconButton> _actionButtons = [];
    private Control? _companionFab;

    static MdToolbar()
    {
        VariantProperty.Changed.AddClassHandler<MdToolbar>((tb, _) => tb.ApplyTheme(MdTheme.Current));
        ColorStyleProperty.Changed.AddClassHandler<MdToolbar>((tb, _) => tb.ApplyTheme(MdTheme.Current));
        OrientationProperty.Changed.AddClassHandler<MdToolbar>((tb, _) => tb.ApplyTheme(MdTheme.Current));
        CompanionFabProperty.Changed.AddClassHandler<MdToolbar>((tb, e) =>
        {
            if (tb._companionFab is not null)
                tb._outerRow.Children.Remove(tb._companionFab);
            tb._companionFab = e.GetNewValue<Control?>();
            if (tb._companionFab is not null)
            {
                MdVisualTreeHelpers.DetachFromParent(tb._companionFab);
                tb._outerRow.Children.Add(tb._companionFab);
            }
        });
    }

    public MdToolbar() : this(MdToolbarVariant.Floating, MdToolbarColorStyle.Standard, Orientation.Horizontal)
    {
    }

    public MdToolbar(
        MdToolbarVariant variant = MdToolbarVariant.Floating,
        MdToolbarColorStyle colorStyle = MdToolbarColorStyle.Standard,
        Orientation orientation = Orientation.Horizontal)
    {
        _itemsPanel = new StackPanel
        {
            Orientation = orientation,
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        _barSurface = new MdSurface
        {
            Content = _itemsPanel,
        };

        _outerRow = new StackPanel
        {
            Orientation = orientation,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _outerRow.Children.Add(_barSurface);

        Content = _outerRow;

        Variant = variant;
        ColorStyle = colorStyle;
        Orientation = orientation;

        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public MdToolbarVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public MdToolbarColorStyle ColorStyle
    {
        get => GetValue(ColorStyleProperty);
        set => SetValue(ColorStyleProperty, value);
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    [Content]
    public global::Avalonia.Controls.Controls Items => _itemsPanel.Children;

    public Control? CompanionFab
    {
        get => GetValue(CompanionFabProperty);
        set => SetValue(CompanionFabProperty, value);
    }

    public MdToolbar AddAction(
        string icon,
        Action? onClick = null,
        bool isToggle = false,
        bool isSelected = false,
        string? tooltip = null)
    {
        var btn = new MdIconButton(
            icon,
            isToggle ? MdIconButtonVariant.Tonal : MdIconButtonVariant.Standard,
            isToggle)
        {
            IsSelected = isSelected,
            ButtonSize = 44,
        };
        if (!string.IsNullOrEmpty(tooltip))
            MdTooltip.Attach(btn, tooltip);
        if (onClick is not null)
            btn.Clicked += (_, _) => onClick();

        _actionButtons.Add(btn);
        _itemsPanel.Children.Add(btn);
        ApplyTheme(MdTheme.Current);
        return this;
    }

    public MdToolbar AddControl(Control control)
    {
        _itemsPanel.Children.Add(control);
        return this;
    }

    public void Add(Control control) => _itemsPanel.Children.Add(control);

    public IEnumerator<Control> GetEnumerator() => _itemsPanel.Children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public MdToolbar AddDivider()
    {
        var vertical = Orientation == Orientation.Horizontal;
        var div = new MdDivider(insetStart: 8, insetEnd: 8, vertical: vertical);
        if (vertical) div.Height = 28;
        else div.Width = 28;
        _itemsPanel.Children.Add(div);
        return this;
    }

    private void ApplyTheme(MdTheme t)
    {
        var c = t.Color;
        var isFloating = Variant == MdToolbarVariant.Floating;
        var isHorizontal = Orientation == Orientation.Horizontal;

        _outerRow.Orientation = Orientation;
        _itemsPanel.Orientation = Orientation;

        HorizontalAlignment = isFloating ? HorizontalAlignment.Center : HorizontalAlignment.Stretch;
        _barSurface.HorizontalAlignment = isFloating ? HorizontalAlignment.Center : HorizontalAlignment.Stretch;

        if (isFloating)
        {
            if (isHorizontal)
            {
                _barSurface.MinHeight = Math.Max(52, 64 + t.DensityOffset);
                _barSurface.Padding = new Thickness(12, 6);
            }
            else
            {
                _barSurface.MinWidth = Math.Max(52, 64 + t.DensityOffset);
                _barSurface.Padding = new Thickness(6, 12);
            }
            _barSurface.Radius = 32;
            _barSurface.Elevation = 3;
        }
        else
        {
            _barSurface.MinHeight = Math.Max(56, 64 + t.DensityOffset);
            _barSurface.Padding = new Thickness(16, 8);
            _barSurface.Radius = 0;
            _barSurface.Elevation = 2;
        }

        var isVibrant = ColorStyle == MdToolbarColorStyle.Vibrant;
        _barSurface.FillColor = isVibrant ? c.PrimaryContainer : c.SurfaceContainerHigh;
    }
}
