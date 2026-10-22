using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Metadata;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Material Design 3 Bottom App Bar (80dp height, action icons on the left, optional docked FAB on the right).
/// </summary>
public class MdBottomAppBar : MdContentView
{
    public static readonly StyledProperty<MdFab?> FabProperty =
        AvaloniaProperty.Register<MdBottomAppBar, MdFab?>(nameof(Fab));

    public static readonly StyledProperty<string?> FabIconProperty =
        AvaloniaProperty.Register<MdBottomAppBar, string?>(nameof(FabIcon), MdIcons.Add);

    public static readonly StyledProperty<ICommand?> FabCommandProperty =
        AvaloniaProperty.Register<MdBottomAppBar, ICommand?>(nameof(FabCommand));

    /// <summary>
    /// Extra padding added inside the bottom app bar surface. Used by
    /// <see cref="MdNavigationPage"/> to keep content above the system navigation
    /// bar while the surface color still fills the inset area.
    /// </summary>
    public static readonly StyledProperty<Thickness> SafeAreaPaddingProperty =
        AvaloniaProperty.Register<MdBottomAppBar, Thickness>(nameof(SafeAreaPadding));

    private readonly MdSurface _surface;
    private readonly StackPanel _actionsRow;
    private readonly Border _fabHost;
    private MdFab? _fab;

    public event EventHandler? FabClicked;

    static MdBottomAppBar()
    {
        FabProperty.Changed.AddClassHandler<MdBottomAppBar>((b, e) =>
        {
            if (b._fab is not null)
                b._fab.Clicked -= b.OnInnerFabClicked;
            b._fab = e.GetNewValue<MdFab?>();
            b._fabHost.Child = null;
            MdVisualTreeHelpers.DetachFromParent(b._fab);
            if (b._fab is not null)
            {
                b._fab.Clicked += b.OnInnerFabClicked;
                if (b.FabCommand is { } cmd) b._fab.Command = cmd;
            }
            b._fabHost.Child = b._fab;
        });
        FabIconProperty.Changed.AddClassHandler<MdBottomAppBar>((b, e) =>
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
        FabCommandProperty.Changed.AddClassHandler<MdBottomAppBar>((b, e) =>
        {
            if (b._fab is not null)
                b._fab.Command = e.GetNewValue<ICommand?>();
        });
        SafeAreaPaddingProperty.Changed.AddClassHandler<MdBottomAppBar>((b, _) =>
        {
            b._surface.Padding = b.SafeAreaPadding;
        });
    }

    public MdBottomAppBar() : this(MdIcons.Add, null)
    {
    }

    public MdBottomAppBar(string? fabIcon = MdIcons.Add, Action? onFabClick = null)
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
            var fab = new MdFab(fabIcon!, MdFabSize.Regular, MdFabColor.Secondary);
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

        _surface = new MdSurface
        {
            Radius = 0,
            Elevation = 2,
            Content = grid,
        };

        Content = _surface;
        MdTheme.Observe(this, (self, theme) =>
        {
            self._surface.FillColor = theme.Color.SurfaceContainer;
        });
    }

    private void OnInnerFabClicked(object? sender, EventArgs e) =>
        FabClicked?.Invoke(this, EventArgs.Empty);

    [Content]
    public global::Avalonia.Controls.Controls Actions => _actionsRow.Children;

    public MdFab? Fab
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

    public MdBottomAppBar AddAction(string icon, Action? onClick = null)
    {
        var btn = new MdIconButton(icon);
        if (onClick is not null)
            btn.Clicked += (_, _) => onClick();
        _actionsRow.Children.Add(btn);
        return this;
    }
}
