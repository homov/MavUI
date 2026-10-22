using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Top App Bar (CenterAligned, Small, Medium, Large) with scroll-lift tinting and collapsible headline.
/// </summary>
public class MavTopAppBar : MavContentView
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MavTopAppBar, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<MavTopAppBar, string?>(nameof(Subtitle));

    public static readonly StyledProperty<string?> NavigationIconProperty =
        AvaloniaProperty.Register<MavTopAppBar, string?>(nameof(NavigationIcon));

    public static readonly StyledProperty<MavTopAppBarVariant> VariantProperty =
        AvaloniaProperty.Register<MavTopAppBar, MavTopAppBarVariant>(nameof(Variant), MavTopAppBarVariant.Small);

    public static readonly StyledProperty<bool> IsScrolledProperty =
        AvaloniaProperty.Register<MavTopAppBar, bool>(nameof(IsScrolled));

    public static readonly StyledProperty<ICommand?> NavigationCommandProperty =
        AvaloniaProperty.Register<MavTopAppBar, ICommand?>(nameof(NavigationCommand));

    public static readonly StyledProperty<object?> NavigationCommandParameterProperty =
        AvaloniaProperty.Register<MavTopAppBar, object?>(nameof(NavigationCommandParameter));

    public static readonly StyledProperty<double> CollapseProgressProperty =
        AvaloniaProperty.Register<MavTopAppBar, double>(nameof(CollapseProgress));

    public static readonly StyledProperty<ScrollViewer?> ScrollViewerProperty =
        AvaloniaProperty.Register<MavTopAppBar, ScrollViewer?>(nameof(ScrollViewer));

    /// <summary>
    /// Extra padding added inside the app bar surface. Used by
    /// <see cref="MavNavigationPage"/> to keep content out of the status bar / notch
    /// while the surface color still fills the inset area.
    /// </summary>
    public static readonly StyledProperty<Thickness> SafeAreaPaddingProperty =
        AvaloniaProperty.Register<MavTopAppBar, Thickness>(nameof(SafeAreaPadding));

    private readonly MavSurface _surface;
    private readonly MavIconButton _navButton;
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

    static MavTopAppBar()
    {
        TitleProperty.Changed.AddClassHandler<MavTopAppBar>((b, _) => b.Sync());
        SubtitleProperty.Changed.AddClassHandler<MavTopAppBar>((b, _) => b.Sync());
        NavigationIconProperty.Changed.AddClassHandler<MavTopAppBar>((b, _) => b.Sync());
        VariantProperty.Changed.AddClassHandler<MavTopAppBar>((b, _) =>
        {
            b.Sync();
            b.ApplyTheme(MavTheme.Current);
        });
        IsScrolledProperty.Changed.AddClassHandler<MavTopAppBar>((b, _) => b.ApplyTheme(MavTheme.Current));
        NavigationCommandProperty.Changed.AddClassHandler<MavTopAppBar>((b, e) =>
            b._navButton.Command = e.GetNewValue<ICommand?>());
        NavigationCommandParameterProperty.Changed.AddClassHandler<MavTopAppBar>((b, e) =>
            b._navButton.CommandParameter = e.GetNewValue<object?>());
        CollapseProgressProperty.Changed.AddClassHandler<MavTopAppBar>((b, e) =>
        {
            b._collapseProgress = Math.Clamp(e.GetNewValue<double>(), 0, 1);
            b.IsScrolled = b._collapseProgress > 0.15;
            b.Sync();
        });
        ScrollViewerProperty.Changed.AddClassHandler<MavTopAppBar>((b, e) =>
            b.OnScrollViewerChanged(e.GetNewValue<ScrollViewer?>()));
        SafeAreaPaddingProperty.Changed.AddClassHandler<MavTopAppBar>((b, _) => b.ApplySafeAreaPadding());
    }

    /// <summary>Creates an empty top app bar.</summary>
    public MavTopAppBar() : this(string.Empty)
    {
    }

    /// <summary>Creates a top app bar with the given title and layout variant.</summary>
    public MavTopAppBar(
        string title = "",
        MavTopAppBarVariant variant = MavTopAppBarVariant.Small,
        string? navigationIcon = null,
        string? subtitle = null)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _navButton = new MavIconButton(navigationIcon ?? MavIcons.Menu)
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

        _surface = new MavSurface
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
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
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

    public MavTopAppBarVariant Variant
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

    /// <summary>Adds an icon action to the trailing side of the bar and returns the bar for chaining.</summary>
    public MavTopAppBar AddAction(string icon, Action? onClick = null)
    {
        var btn = new MavIconButton(icon);
        if (onClick is not null)
            btn.Clicked += (_, _) => onClick();
        _actionsRow.Children.Add(btn);
        return this;
    }

    /// <summary>
    /// Attaches a scroll viewer whose offset drives the app bar's scroll behavior.
    /// </summary>
    public void AttachScrollViewer(ScrollViewer scroll) => ScrollViewer = scroll;

    /// <summary>Subscribes to theme and layout changes once the bar is in the visual tree.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SubscribeToScrollViewer();
    }

    /// <summary>Unsubscribes from theme and layout changes so the bar can be collected.</summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        UnsubscribeFromScrollViewer();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnScrollViewerChanged(ScrollViewer? scroll)
    {
        UnsubscribeFromScrollViewer();
        _attachedScroll = scroll;
        if (scroll is null)
        {
            _scrollHandler = null;
            return;
        }

        _scrollHandler ??= (_, _) =>
        {
            if (_attachedScroll is not { } attached)
                return;

            var y = attached.Offset.Y;
            if (Variant is MavTopAppBarVariant.Medium or MavTopAppBarVariant.Large)
            {
                var maxScroll = Variant == MavTopAppBarVariant.Large ? 88.0 : 52.0;
                CollapseProgress = Math.Clamp(y / maxScroll, 0, 1);
            }
            else
            {
                IsScrolled = y > 4;
            }
        };

        SubscribeToScrollViewer();
    }

    private void SubscribeToScrollViewer()
    {
        if (_attachedScroll is null || _scrollHandler is null || !this.IsAttachedToVisualTree())
            return;

        _attachedScroll.ScrollChanged -= _scrollHandler;
        _attachedScroll.ScrollChanged += _scrollHandler;
    }

    private void UnsubscribeFromScrollViewer()
    {
        if (_attachedScroll is not null && _scrollHandler is not null)
            _attachedScroll.ScrollChanged -= _scrollHandler;
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

        var isCentered = Variant == MavTopAppBarVariant.CenterAligned;
        _compactTitleBox.HorizontalAlignment = isCentered ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        _compactTitle.TextAlignment = isCentered ? TextAlignment.Center : TextAlignment.Left;
        _compactSubtitle.TextAlignment = isCentered ? TextAlignment.Center : TextAlignment.Left;

        var isExpandable = Variant is MavTopAppBarVariant.Medium or MavTopAppBarVariant.Large;
        if (isExpandable)
        {
            _expandedBox.IsVisible = _collapseProgress < 0.98;
            _expandedBox.Opacity = 1.0 - _collapseProgress;
            _compactTitleBox.Opacity = _collapseProgress;
            _compactTitleBox.IsHitTestVisible = _collapseProgress > 0.5;
            _expandedBox.Padding = Variant == MavTopAppBarVariant.Large
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

    private void ApplyTheme(MavTheme t)
    {
        var c = t.Color;
        _compactTitle.Apply(MavTypeRole.TitleLarge, t.Typography);
        _compactSubtitle.Apply(MavTypeRole.BodySmall, t.Typography);
        _expandedTitle.Apply(
            Variant == MavTopAppBarVariant.Large ? MavTypeRole.HeadlineMedium : MavTypeRole.HeadlineSmall,
            t.Typography);
        _expandedSubtitle.Apply(MavTypeRole.BodyMedium, t.Typography);

        _surface.FillColor = IsScrolled ? c.SurfaceContainerHigh : c.SurfaceContainerLow;
        _bottomDivider.Background = MavTheme.BrushOf(IsScrolled ? c.OutlineVariant : c.OutlineVariant.WithAlpha(0.5f));
        _compactTitle.Foreground = MavTheme.BrushOf(c.OnSurface);
        _compactSubtitle.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);
        _expandedTitle.Foreground = MavTheme.BrushOf(c.OnSurface);
        _expandedSubtitle.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);
    }
}
