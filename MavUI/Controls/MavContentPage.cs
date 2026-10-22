using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Mav.Platform;
using Mav.Primitives;

namespace Mav.Controls;

/// <summary>
/// Material Design content page used by <see cref="MavNavigationPage"/>.
/// The page supplies content and app-bar metadata; navigation chrome belongs to the host.
/// </summary>
public class MavContentPage : MavContentView
{
    private readonly List<MavPageAction> _actions = [];

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MavContentPage, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<MavContentPage, string?>(nameof(Subtitle));

    public static readonly StyledProperty<string?> NavigationIconProperty =
        AvaloniaProperty.Register<MavContentPage, string?>(nameof(NavigationIcon));

    public static readonly StyledProperty<bool> ShowAppBarProperty =
        AvaloniaProperty.Register<MavContentPage, bool>(nameof(ShowAppBar), true);

    public static readonly StyledProperty<bool> ShowBackButtonProperty =
        AvaloniaProperty.Register<MavContentPage, bool>(nameof(ShowBackButton), true);

    public static readonly StyledProperty<MavTopAppBarVariant> AppBarVariantProperty =
        AvaloniaProperty.Register<MavContentPage, MavTopAppBarVariant>(
            nameof(AppBarVariant),
            MavTopAppBarVariant.Small);

    /// <summary>Creates an empty content page.</summary>
    public MavContentPage()
    {
        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch;
        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch;
    }

    /// <summary>Creates a content page with the given title and content.</summary>
    public MavContentPage(string title, Control? content = null) : this()
    {
        Title = title;
        Content = content;
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

    /// <summary>
    /// Optional leading icon. When the page can go back, the host replaces it with a back arrow.
    /// </summary>
    public string? NavigationIcon
    {
        get => GetValue(NavigationIconProperty);
        set => SetValue(NavigationIconProperty, value);
    }

    public bool ShowAppBar
    {
        get => GetValue(ShowAppBarProperty);
        set => SetValue(ShowAppBarProperty, value);
    }

    public bool ShowBackButton
    {
        get => GetValue(ShowBackButtonProperty);
        set => SetValue(ShowBackButtonProperty, value);
    }

    public MavTopAppBarVariant AppBarVariant
    {
        get => GetValue(AppBarVariantProperty);
        set => SetValue(AppBarVariantProperty, value);
    }

    public IReadOnlyList<MavPageAction> Actions => _actions;

    public Control? BottomBar { get; set; }

    public Control? FloatingActionButton { get; set; }

    /// <summary>
    /// Optional custom action for the app-bar leading button. When set, it takes
    /// precedence over the default back action.
    /// </summary>
    public Action? NavigationAction { get; set; }

    public MavNavigationPage? Navigation { get; internal set; }

    public bool IsInNavigation => Navigation is not null;

    public event EventHandler? Appearing;
    public event EventHandler? Disappearing;
    public event EventHandler? NavigationClicked;

    /// <summary>Adds an action to the app bar and returns it.</summary>
    public MavPageAction AddAction(string icon, Action? onClick = null, string? tooltip = null)
    {
        var action = new MavPageAction
        {
            Icon = icon,
            OnClick = onClick,
            Tooltip = tooltip,
        };
        _actions.Add(action);
        return action;
    }

    public void ClearActions() => _actions.Clear();

    /// <summary>
    /// Called before the host pops the page. Return <c>true</c> when the page
    /// handled back itself (for example, closed an internal search state).
    /// </summary>
    public virtual bool OnBackPressed() => false;

    /// <summary>Raises the appearing notification, which calls <see cref="OnAppearing"/>.</summary>
    internal void NotifyAppearing()
    {
        OnAppearing();
        Appearing?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Raises the disappearing notification, which calls <see cref="OnDisappearing"/>.</summary>
    internal void NotifyDisappearing()
    {
        // Dialogs, sheets and other overlays belong to the page that opened them.
        // Close them before the page leaves so they cannot outlive the page state.
        MavOverlay.DismissForPage(this);

        OnDisappearing();
        Disappearing?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Handles a click on the navigation button and returns <c>true</c> if the click was handled.</summary>
    internal bool HandleNavigationClick()
    {
        if (NavigationAction is not null)
        {
            NavigationAction();
            return true;
        }

        if (NavigationClicked is not null)
        {
            NavigationClicked(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    /// <summary>Called when the page becomes visible. Override to load data.</summary>
    protected virtual void OnAppearing()
    {
    }

    /// <summary>Called when the page stops being visible. Override to release resources.</summary>
    protected virtual void OnDisappearing()
    {
    }
}
