using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Expressive Toolbar supporting Floating pill (64dp height, full-rounded, elevation 3)
/// and Docked full-width configurations in Standard or Vibrant color styles, with optional companion FAB.
/// </summary>
public class MavToolbar : MavContentView, IEnumerable<Control>
{
    public static readonly StyledProperty<MavToolbarVariant> VariantProperty =
        AvaloniaProperty.Register<MavToolbar, MavToolbarVariant>(nameof(Variant), MavToolbarVariant.Floating);

    public static readonly StyledProperty<MavToolbarColorStyle> ColorStyleProperty =
        AvaloniaProperty.Register<MavToolbar, MavToolbarColorStyle>(nameof(ColorStyle), MavToolbarColorStyle.Standard);

    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<MavToolbar, Orientation>(nameof(Orientation), Orientation.Horizontal);

    public static readonly StyledProperty<Control?> CompanionFabProperty =
        AvaloniaProperty.Register<MavToolbar, Control?>(nameof(CompanionFab));

    private readonly StackPanel _outerRow;
    private readonly MavSurface _barSurface;
    private readonly StackPanel _itemsPanel;
    private readonly List<MavIconButton> _actionButtons = [];
    private Control? _companionFab;

    static MavToolbar()
    {
        VariantProperty.Changed.AddClassHandler<MavToolbar>((tb, _) => tb.ApplyTheme(MavTheme.Current));
        ColorStyleProperty.Changed.AddClassHandler<MavToolbar>((tb, _) => tb.ApplyTheme(MavTheme.Current));
        OrientationProperty.Changed.AddClassHandler<MavToolbar>((tb, _) => tb.ApplyTheme(MavTheme.Current));
        CompanionFabProperty.Changed.AddClassHandler<MavToolbar>((tb, e) =>
        {
            if (tb._companionFab is not null)
                tb._outerRow.Children.Remove(tb._companionFab);
            tb._companionFab = e.GetNewValue<Control?>();
            if (tb._companionFab is not null)
            {
                MavVisualTreeHelpers.DetachFromParent(tb._companionFab);
                tb._outerRow.Children.Add(tb._companionFab);
            }
        });
    }

    /// <summary>Creates a floating standard toolbar with a horizontal layout.</summary>
    public MavToolbar() : this(MavToolbarVariant.Floating, MavToolbarColorStyle.Standard, Orientation.Horizontal)
    {
    }

    /// <summary>Creates a toolbar with the given variant, color style and orientation.</summary>
    public MavToolbar(
        MavToolbarVariant variant = MavToolbarVariant.Floating,
        MavToolbarColorStyle colorStyle = MavToolbarColorStyle.Standard,
        Orientation orientation = Orientation.Horizontal)
    {
        _itemsPanel = new StackPanel
        {
            Orientation = orientation,
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        _barSurface = new MavSurface
        {
            Child = _itemsPanel,
        };

        _outerRow = new StackPanel
        {
            Orientation = orientation,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _outerRow.Children.Add(_barSurface);

        Child = _outerRow;

        Variant = variant;
        ColorStyle = colorStyle;
        Orientation = orientation;

        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public MavToolbarVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public MavToolbarColorStyle ColorStyle
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

    /// <summary>Adds an icon action and returns the toolbar for chaining.</summary>
    public MavToolbar AddAction(
        string icon,
        Action? onClick = null,
        bool isToggle = false,
        bool isSelected = false,
        string? tooltip = null)
    {
        var btn = new MavIconButton(
            icon,
            isToggle ? MavIconButtonVariant.Tonal : MavIconButtonVariant.Standard,
            isToggle)
        {
            IsSelected = isSelected,
            ButtonSize = 44,
        };
        if (!string.IsNullOrEmpty(tooltip))
            MavTooltip.Attach(btn, tooltip);
        if (onClick is not null)
            btn.Clicked += (_, _) => onClick();

        _actionButtons.Add(btn);
        _itemsPanel.Children.Add(btn);
        ApplyTheme(MavTheme.Current);
        return this;
    }

    /// <summary>Adds a custom control and returns the toolbar for chaining.</summary>
    public MavToolbar AddControl(Control control)
    {
        _itemsPanel.Children.Add(control);
        return this;
    }

    /// <summary>
    /// Adds a control to the toolbar.
    /// </summary>
    public void Add(Control control) => _itemsPanel.Children.Add(control);

    /// <summary>
    /// Returns an enumerator over the toolbar items.
    /// </summary>
    public IEnumerator<Control> GetEnumerator() => _itemsPanel.Children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Adds a divider and returns the toolbar for chaining.</summary>
    public MavToolbar AddDivider()
    {
        var vertical = Orientation == Orientation.Horizontal;
        var div = new MavDivider(insetStart: 8, insetEnd: 8, vertical: vertical);
        if (vertical) div.Height = 28;
        else div.Width = 28;
        _itemsPanel.Children.Add(div);
        return this;
    }

    private void ApplyTheme(MavTheme t)
    {
        var c = t.Color;
        var isFloating = Variant == MavToolbarVariant.Floating;
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

        var isVibrant = ColorStyle == MavToolbarColorStyle.Vibrant;
        _barSurface.FillColor = isVibrant ? c.PrimaryContainer : c.SurfaceContainerHigh;
    }
}
