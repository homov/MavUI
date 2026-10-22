using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

public enum MdTopAppBarVariant
{
    CenterAligned,
    Small,
    Medium,
    Large,
}

/// <summary>
/// Material Design 3 Top App Bar (CenterAligned, Small, Medium, Large) with scroll-lift tinting and collapsible headline.
/// </summary>
public class MdTopAppBar : MdContentView
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MdTopAppBar, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<MdTopAppBar, string?>(nameof(Subtitle));

    public static readonly StyledProperty<string?> NavigationIconProperty =
        AvaloniaProperty.Register<MdTopAppBar, string?>(nameof(NavigationIcon));

    public static readonly StyledProperty<MdTopAppBarVariant> VariantProperty =
        AvaloniaProperty.Register<MdTopAppBar, MdTopAppBarVariant>(nameof(Variant), MdTopAppBarVariant.Small);

    public static readonly StyledProperty<bool> IsScrolledProperty =
        AvaloniaProperty.Register<MdTopAppBar, bool>(nameof(IsScrolled));

    public static readonly StyledProperty<ICommand?> NavigationCommandProperty =
        AvaloniaProperty.Register<MdTopAppBar, ICommand?>(nameof(NavigationCommand));

    public static readonly StyledProperty<object?> NavigationCommandParameterProperty =
        AvaloniaProperty.Register<MdTopAppBar, object?>(nameof(NavigationCommandParameter));

    public static readonly StyledProperty<double> CollapseProgressProperty =
        AvaloniaProperty.Register<MdTopAppBar, double>(nameof(CollapseProgress));

    public static readonly StyledProperty<ScrollViewer?> ScrollViewerProperty =
        AvaloniaProperty.Register<MdTopAppBar, ScrollViewer?>(nameof(ScrollViewer));

    /// <summary>
    /// Extra padding added inside the app bar surface. Used by
    /// <see cref="MdNavigationPage"/> to keep content out of the status bar / notch
    /// while the surface color still fills the inset area.
    /// </summary>
    public static readonly StyledProperty<Thickness> SafeAreaPaddingProperty =
        AvaloniaProperty.Register<MdTopAppBar, Thickness>(nameof(SafeAreaPadding));

    private readonly MdSurface _surface;
    private readonly MdIconButton _navButton;
    private readonly StackPanel _compactTitleBox;
    private readonly TextBlock _compactTitle;
    private readonly TextBlock _compactSubtitle;
    private readonly StackPanel _actionsRow;
    private readonly Border _expandedBox;
    private readonly StackPanel _expandedTitleStack;
    private readonly TextBlock _expandedTitle;
    private readonly TextBlock _expandedSubtitle;
    private readonly Border _bottomDivider;
    private double _collapseProgress;
    private ScrollViewer? _attachedScroll;
    private EventHandler<ScrollChangedEventArgs>? _scrollHandler;

    public event EventHandler? NavigationClicked;

    static MdTopAppBar()
    {
        TitleProperty.Changed.AddClassHandler<MdTopAppBar>((b, _) => b.Sync());
        SubtitleProperty.Changed.AddClassHandler<MdTopAppBar>((b, _) => b.Sync());
        NavigationIconProperty.Changed.AddClassHandler<MdTopAppBar>((b, _) => b.Sync());
        VariantProperty.Changed.AddClassHandler<MdTopAppBar>((b, _) =>
        {
            b.Sync();
            b.ApplyTheme(MdTheme.Current);
        });
        IsScrolledProperty.Changed.AddClassHandler<MdTopAppBar>((b, _) => b.ApplyTheme(MdTheme.Current));
        NavigationCommandProperty.Changed.AddClassHandler<MdTopAppBar>((b, e) =>
            b._navButton.Command = e.GetNewValue<ICommand?>());
        NavigationCommandParameterProperty.Changed.AddClassHandler<MdTopAppBar>((b, e) =>
            b._navButton.CommandParameter = e.GetNewValue<object?>());
        CollapseProgressProperty.Changed.AddClassHandler<MdTopAppBar>((b, e) =>
        {
            b._collapseProgress = Math.Clamp(e.GetNewValue<double>(), 0, 1);
            b.IsScrolled = b._collapseProgress > 0.15;
            b.Sync();
        });
        ScrollViewerProperty.Changed.AddClassHandler<MdTopAppBar>((b, e) =>
            b.OnScrollViewerChanged(e.GetNewValue<ScrollViewer?>()));
        SafeAreaPaddingProperty.Changed.AddClassHandler<MdTopAppBar>((b, _) => b.ApplySafeAreaPadding());
    }

    public MdTopAppBar() : this(string.Empty)
    {
    }

    public MdTopAppBar(
        string title = "",
        MdTopAppBarVariant variant = MdTopAppBarVariant.Small,
        string? navigationIcon = null,
        string? subtitle = null)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _navButton = new MdIconButton(navigationIcon ?? MdIcons.Menu)
        {
            IsVisible = !string.IsNullOrEmpty(navigationIcon),
            VerticalAlignment = VerticalAlignment.Center,
        };
        _navButton.Clicked += (_, _) => NavigationClicked?.Invoke(this, EventArgs.Empty);

        _compactTitle = new TextBlock
        {
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        _compactSubtitle = new TextBlock
        {
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            IsVisible = false,
        };

        _compactTitleBox = new StackPanel
        {
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0),
        };
        _compactTitleBox.Children.Add(_compactTitle);
        _compactTitleBox.Children.Add(_compactSubtitle);

        _actionsRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var topRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            MinHeight = 64,
            Margin = new Thickness(4, 0, 8, 0),
        };
        topRow.Add(_navButton, 0, 0);
        topRow.Add(_compactTitleBox, 1, 0);
        topRow.Add(_actionsRow, 2, 0);

        _expandedTitle = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 2,
        };
        _expandedSubtitle = new TextBlock
        {
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            IsVisible = false,
        };
        _expandedTitleStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
        };
        _expandedTitleStack.Children.Add(_expandedTitle);
        _expandedTitleStack.Children.Add(_expandedSubtitle);

        _expandedBox = new Border
        {
            Padding = new Thickness(16, 0, 16, 16),
            IsVisible = false,
            Child = _expandedTitleStack,
        };

        _bottomDivider = new Border
        {
            Height = 1,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        var stack = new StackPanel { Orientation = Orientation.Vertical };
        stack.Children.Add(topRow);
        stack.Children.Add(_expandedBox);
        stack.Children.Add(_bottomDivider);

        _surface = new MdSurface
        {
            Radius = 0,
            ClipToBounds = true,
            Content = stack,
        };

        Content = _surface;

        Title = title;
        Variant = variant;
        NavigationIcon = navigationIcon;
        Subtitle = subtitle;

        Sync();
        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string? NavigationIcon
    {
        get => GetValue(NavigationIconProperty);
        set => SetValue(NavigationIconProperty, value);
    }

    public MdTopAppBarVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public bool IsScrolled
    {
        get => GetValue(IsScrolledProperty);
        set => SetValue(IsScrolledProperty, value);
    }

    public ICommand? NavigationCommand
    {
        get => GetValue(NavigationCommandProperty);
        set => SetValue(NavigationCommandProperty, value);
    }

    public object? NavigationCommandParameter
    {
        get => GetValue(NavigationCommandParameterProperty);
        set => SetValue(NavigationCommandParameterProperty, value);
    }

    public double CollapseProgress
    {
        get => GetValue(CollapseProgressProperty);
        set => SetValue(CollapseProgressProperty, Math.Clamp(value, 0, 1));
    }

    public ScrollViewer? ScrollViewer
    {
        get => GetValue(ScrollViewerProperty);
        set => SetValue(ScrollViewerProperty, value);
    }

    public Thickness SafeAreaPadding
    {
        get => GetValue(SafeAreaPaddingProperty);
        set => SetValue(SafeAreaPaddingProperty, value);
    }

    private void ApplySafeAreaPadding()
    {
        _surface.Padding = SafeAreaPadding;
    }

    [Content]
    public global::Avalonia.Controls.Controls Actions => _actionsRow.Children;

    public MdTopAppBar AddAction(string icon, Action? onClick = null)
    {
        var btn = new MdIconButton(icon);
        if (onClick is not null)
            btn.Clicked += (_, _) => onClick();
        _actionsRow.Children.Add(btn);
        return this;
    }

    public void AttachScrollViewer(ScrollViewer scroll) => ScrollViewer = scroll;

    private void OnScrollViewerChanged(ScrollViewer? scroll)
    {
        if (_attachedScroll is not null && _scrollHandler is not null)
        {
            _attachedScroll.ScrollChanged -= _scrollHandler;
        }
        _attachedScroll = scroll;
        if (scroll is null) return;
        _scrollHandler = (_, _) =>
        {
            var y = scroll.Offset.Y;
            if (Variant is MdTopAppBarVariant.Medium or MdTopAppBarVariant.Large)
            {
                var maxScroll = Variant == MdTopAppBarVariant.Large ? 88.0 : 52.0;
                CollapseProgress = Math.Clamp(y / maxScroll, 0, 1);
            }
            else
            {
                IsScrolled = y > 4;
            }
        };
        scroll.ScrollChanged += _scrollHandler;
    }

    private void Sync()
    {
        var hasSub = !string.IsNullOrEmpty(Subtitle);
        _compactTitle.Text = Title;
        _expandedTitle.Text = Title;
        _compactSubtitle.Text = Subtitle ?? string.Empty;
        _expandedSubtitle.Text = Subtitle ?? string.Empty;
        _compactSubtitle.IsVisible = hasSub;
        _expandedSubtitle.IsVisible = hasSub;

        var hasNav = !string.IsNullOrEmpty(NavigationIcon);
        if (hasNav) _navButton.Icon = NavigationIcon!;
        _navButton.IsVisible = hasNav;

        var isCentered = Variant == MdTopAppBarVariant.CenterAligned;
        _compactTitleBox.HorizontalAlignment = isCentered ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        _compactTitle.TextAlignment = isCentered ? TextAlignment.Center : TextAlignment.Left;
        _compactSubtitle.TextAlignment = isCentered ? TextAlignment.Center : TextAlignment.Left;

        var isExpandable = Variant is MdTopAppBarVariant.Medium or MdTopAppBarVariant.Large;
        if (isExpandable)
        {
            _expandedBox.IsVisible = _collapseProgress < 0.98;
            _expandedBox.Opacity = 1.0 - _collapseProgress;
            _compactTitleBox.Opacity = _collapseProgress;
            _compactTitleBox.IsHitTestVisible = _collapseProgress > 0.5;
            _expandedBox.Padding = Variant == MdTopAppBarVariant.Large
                ? new Thickness(16, 12 * (1.0 - _collapseProgress), 16, 20 * (1.0 - _collapseProgress))
                : new Thickness(16, 2 * (1.0 - _collapseProgress), 16, 16 * (1.0 - _collapseProgress));
        }
        else
        {
            _expandedBox.IsVisible = false;
            _compactTitleBox.Opacity = 1.0;
            _compactTitleBox.IsHitTestVisible = true;
        }
    }

    private void ApplyTheme(MdTheme t)
    {
        var c = t.Color;
        _compactTitle.Apply(MdTypeRole.TitleLarge, t.Typography);
        _compactSubtitle.Apply(MdTypeRole.BodySmall, t.Typography);
        _expandedTitle.Apply(
            Variant == MdTopAppBarVariant.Large ? MdTypeRole.HeadlineMedium : MdTypeRole.HeadlineSmall,
            t.Typography);
        _expandedSubtitle.Apply(MdTypeRole.BodyMedium, t.Typography);

        _surface.FillColor = IsScrolled ? c.SurfaceContainerHigh : c.SurfaceContainerLow;
        _bottomDivider.Background = MdTheme.BrushOf(IsScrolled ? c.OutlineVariant : c.OutlineVariant.WithAlpha(0.5f));
        _compactTitle.Foreground = MdTheme.BrushOf(c.OnSurface);
        _compactSubtitle.Foreground = MdTheme.BrushOf(c.OnSurfaceVariant);
        _expandedTitle.Foreground = MdTheme.BrushOf(c.OnSurface);
        _expandedSubtitle.Foreground = MdTheme.BrushOf(c.OnSurfaceVariant);
    }
}
