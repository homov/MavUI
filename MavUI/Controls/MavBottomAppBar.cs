using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Bottom App Bar (80dp height, action icons on the left, optional docked FAB on the right).
/// </summary>
public class MavBottomAppBar : MavContentView
{
    public static readonly StyledProperty<MavFab?> FabProperty =
        AvaloniaProperty.Register<MavBottomAppBar, MavFab?>(nameof(Fab));

    public static readonly StyledProperty<string?> FabIconProperty =
        AvaloniaProperty.Register<MavBottomAppBar, string?>(nameof(FabIcon), MavIcons.Add);

    public static readonly StyledProperty<ICommand?> FabCommandProperty =
        AvaloniaProperty.Register<MavBottomAppBar, ICommand?>(nameof(FabCommand));

    /// <summary>
    /// Extra padding added inside the bottom app bar surface. Used by
    /// <see cref="MavNavigationPage"/> to keep content above the system navigation
    /// bar while the surface color still fills the inset area.
    /// </summary>
    public static readonly StyledProperty<Thickness> SafeAreaPaddingProperty =
        AvaloniaProperty.Register<MavBottomAppBar, Thickness>(nameof(SafeAreaPadding));

    private readonly MavSurface _surface;
    private readonly StackPanel _actionsRow;
    private readonly Border _fabHost;
    private MavFab? _fab;

    public event EventHandler? FabClicked;

    static MavBottomAppBar()
    {
        FabProperty.Changed.AddClassHandler<MavBottomAppBar>((b, e) =>
        {
            if (b._fab is not null)
                b._fab.Clicked -= b.OnInnerFabClicked;
            b._fab = e.GetNewValue<MavFab?>();
            b._fabHost.Child = null;
            MavVisualTreeHelpers.DetachFromParent(b._fab);
            if (b._fab is not null)
            {
                b._fab.Clicked += b.OnInnerFabClicked;
                if (b.FabCommand is { } cmd) b._fab.Command = cmd;
            }
            b._fabHost.Child = b._fab;
        });
        FabIconProperty.Changed.AddClassHandler<MavBottomAppBar>((b, e) =>
        {
            var icon = e.GetNewValue<string?>();
            if (string.IsNullOrEmpty(icon))
            {
                b._fabHost.IsVisible = false;
            }
            else
            {
                b._fabHost.IsVisible = true;
                if (b._fab is not null) b._fab.Icon = icon!;
            }
        });
        FabCommandProperty.Changed.AddClassHandler<MavBottomAppBar>((b, e) =>
        {
            if (b._fab is not null)
                b._fab.Command = e.GetNewValue<ICommand?>();
        });
        SafeAreaPaddingProperty.Changed.AddClassHandler<MavBottomAppBar>((b, _) =>
        {
            b._surface.Padding = b.SafeAreaPadding;
        });
    }

    /// <summary>Creates a bottom app bar with the default add button.</summary>
    public MavBottomAppBar() : this(MavIcons.Add, null)
    {
    }

    /// <summary>Creates a bottom app bar with a floating button that uses <paramref name="fabIcon"/> and <paramref name="onFabClick"/>.</summary>
    public MavBottomAppBar(string? fabIcon = MavIcons.Add, Action? onFabClick = null)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _actionsRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
        };

        _fabHost = new Border
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
        };

        if (!string.IsNullOrEmpty(fabIcon))
        {
            var fab = new MavFab(fabIcon!, MavFabSize.Regular, MavFabColor.Secondary);
            if (onFabClick is not null)
                fab.Clicked += (_, _) => onFabClick();
            Fab = fab;
            FabIcon = fabIcon;
        }

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            MinHeight = 80,
            Margin = new Thickness(12, 0, 16, 0),
        };
        grid.Add(_actionsRow, 0, 0);
        grid.Add(_fabHost, 1, 0);

        _surface = new MavSurface
        {
            Radius = 0,
            Elevation = 2,
            Child = grid,
        };

        Child = _surface;
        MavTheme.Observe(this, (self, theme) =>
        {
            self._surface.FillColor = theme.Color.SurfaceContainer;
        });
    }

    private void OnInnerFabClicked(object? sender, EventArgs e) =>
        FabClicked?.Invoke(this, EventArgs.Empty);

    [Content]
    public global::Avalonia.Controls.Controls Actions => _actionsRow.Children;

    public MavFab? Fab
    {
        get => GetValue(FabProperty);
        set => SetValue(FabProperty, value);
    }

    public string? FabIcon
    {
        get => GetValue(FabIconProperty);
        set => SetValue(FabIconProperty, value);
    }

    public ICommand? FabCommand
    {
        get => GetValue(FabCommandProperty);
        set => SetValue(FabCommandProperty, value);
    }

    public Thickness SafeAreaPadding
    {
        get => GetValue(SafeAreaPaddingProperty);
        set => SetValue(SafeAreaPaddingProperty, value);
    }

    /// <summary>Adds an icon action to the bar and returns the bar for chaining.</summary>
    public MavBottomAppBar AddAction(string icon, Action? onClick = null)
    {
        var btn = new MavIconButton(icon);
        if (onClick is not null)
            btn.Clicked += (_, _) => onClick();
        _actionsRow.Children.Add(btn);
        return this;
    }
}
