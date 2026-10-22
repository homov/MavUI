using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
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
/// Material Design 3 Modal Navigation Drawer presented over a scrim with slide-in animation and swipe-left dismissal.
/// </summary>
public class MavModalNavigationDrawer : Control
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MavModalNavigationDrawer, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MavModalNavigationDrawer, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    private readonly MavNavigationDrawer _drawer;
    private IDisposable? _handle;
    private double _dragStartX;
    private bool _dragging;
    private bool _syncingIsOpen;

    static MavModalNavigationDrawer()
    {
        TitleProperty.Changed.AddClassHandler<MavModalNavigationDrawer>((d, e) =>
            d._drawer.Title = e.GetNewValue<string>() ?? string.Empty);
        IsOpenProperty.Changed.AddClassHandler<MavModalNavigationDrawer>((d, e) =>
        {
            if (d._syncingIsOpen) return;
            if (e.GetNewValue<bool>())
                d.Show(d.Parent as Visual ?? d);
            else
                d.Close();
        });
    }

    /// <summary>Creates an empty modal navigation drawer 300 dp wide.</summary>
    public MavModalNavigationDrawer() : this(null, 300)
    {
    }

    /// <summary>Creates a modal navigation drawer with an optional title and width.</summary>
    public MavModalNavigationDrawer(string? title = null, double width = 300)
    {
        _drawer = new MavNavigationDrawer(title, width)
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        MavVisualTreeHelpers.AttachLogicalChild(this, _drawer);
        if (title is not null) Title = title;
        _drawer.SelectedIndexChanged += (_, _) => Close();
        _drawer.PointerPressed += OnDragPressed;
        _drawer.PointerMoved += OnDragMoved;
        _drawer.PointerReleased += OnDragReleased;
        _drawer.PointerCaptureLost += (_, _) => _dragging = false;
    }

    protected override Size MeasureOverride(Size availableSize) => default;

    public MavNavigationDrawer Drawer => _drawer;

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Headline
    {
        get => Title;
        set => Title = value;
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    [Content]
    public AvaloniaList<MavDrawerItem> Items => _drawer.Items;

    /// <summary>Adds an item and returns the modal drawer for chaining.</summary>
    public MavModalNavigationDrawer AddItem(string label, string icon, string? badge = null, Action? onClick = null)
    {
        _drawer.AddItem(label, icon, badge, onClick);
        return this;
    }

    /// <summary>Adds a section heading and returns the modal drawer for chaining.</summary>
    public MavModalNavigationDrawer AddSectionHeader(string text)
    {
        _drawer.AddSectionHeader(text);
        return this;
    }

    /// <summary>Adds a divider and returns the modal drawer for chaining.</summary>
    public MavModalNavigationDrawer AddDivider()
    {
        _drawer.AddDivider();
        return this;
    }

    /// <summary>Shows the modal drawer over <paramref name="anchor"/> with a scrim behind it.</summary>
    public void Show(Visual? anchor = null)
    {
        Close();
        var c = MavTheme.Current.Color;
        var pushAnchor = anchor ?? (this.Parent as Visual) ?? this;

        // The drawer stretches vertically; on a narrow window (or after rotation to
        // portrait) its fixed width has to be clamped to the overlay viewport.
        void Reflow()
        {
            var size = MavOverlay.GetOverlaySize(pushAnchor);
            var insets = MavOverlay.SafeAreaOf(pushAnchor);
            var viewportWidth = Math.Max(0, size.Width - insets.Left - insets.Right);
            _drawer.Width = Math.Max(0, Math.Min(_drawer.DrawerWidth, viewportWidth));
        }

        Reflow();
        _handle = MavOverlay.Show(
            pushAnchor,
            _drawer,
            c.Scrim.WithAlpha(0.32f),
            Close,
            onViewportChanged: Reflow);
        _syncingIsOpen = true;
        SetValue(IsOpenProperty, true);
        _syncingIsOpen = false;
        _drawer.SetTranslationX(-300);
        MavMotion.Animate(_drawer, "mdraw_in", -300, 0, MavMotion.Medium2, MavMotion.CubicOut, v =>
        {
            _drawer.SetTranslationX(v);
        });
    }

    /// <summary>Hides the modal drawer and removes its scrim.</summary>
    public void Close()
    {
        if (_handle is null) return;
        var h = _handle;
        _handle = null;
        _syncingIsOpen = true;
        SetValue(IsOpenProperty, false);
        _syncingIsOpen = false;
        MavMotion.Animate(_drawer, "mdraw_out", _drawer.GetTranslationX(), -300, MavMotion.Short4, MavMotion.CubicIn, v =>
        {
            _drawer.SetTranslationX(v);
        }, h.Dispose);
    }

    private void OnDragPressed(object? sender, PointerPressedEventArgs e)
    {
        _dragging = true;
        _dragStartX = e.GetPosition(_drawer.Parent as Visual).X;
    }

    private void OnDragMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging) return;

        // Drag only while the button is really held. A release outside the drawer (for example on the
        // scrim) is never seen by it, so the drag must not survive into plain mouse movement.
        if (!e.GetCurrentPoint(_drawer).Properties.IsLeftButtonPressed)
        {
            _dragging = false;
            return;
        }
        var curX = e.GetPosition(_drawer.Parent as Visual).X;
        var dx = Math.Min(0, curX - _dragStartX);
        if (dx < -6)
            _drawer.SetTranslationX(dx);
    }

    private void OnDragReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        if (_drawer.GetTranslationX() < -80)
        {
            Close();
        }
        else if (_drawer.GetTranslationX() < 0)
        {
            MavMotion.Animate(_drawer, "mdraw_snap", _drawer.GetTranslationX(), 0, MavMotion.Short4, MavMotion.CubicOut, v =>
            {
                _drawer.SetTranslationX(v);
            });
        }
    }
}
