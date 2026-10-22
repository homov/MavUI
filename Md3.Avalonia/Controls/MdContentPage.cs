using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;

namespace Md3.Avalonia.Controls;

public enum MdPageNavigationType
{
    Root,
    Push,
    Pop,
    Replace,
}

public sealed class MdPageAction
{
    public string Icon { get; init; } = string.Empty;
    public string? Tooltip { get; init; }
    public Action? OnClick { get; init; }
}

public sealed class MdPageChangedEventArgs : EventArgs
{
    public MdPageChangedEventArgs(
        MdContentPage? oldPage,
        MdContentPage newPage,
        MdPageNavigationType navigationType)
    {
        OldPage = oldPage;
        NewPage = newPage;
        NavigationType = navigationType;
    }

    public MdContentPage? OldPage { get; }
    public MdContentPage NewPage { get; }
    public MdPageNavigationType NavigationType { get; }
}

/// <summary>
/// Material Design content page used by <see cref="MdNavigationPage"/>.
/// The page supplies content and app-bar metadata; navigation chrome belongs to the host.
/// </summary>
public class MdContentPage : MdContentView
{
    private readonly List<MdPageAction> _actions = [];

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MdContentPage, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<MdContentPage, string?>(nameof(Subtitle));

    public static readonly StyledProperty<string?> NavigationIconProperty =
        AvaloniaProperty.Register<MdContentPage, string?>(nameof(NavigationIcon));

    public static readonly StyledProperty<bool> ShowAppBarProperty =
        AvaloniaProperty.Register<MdContentPage, bool>(nameof(ShowAppBar), true);

    public static readonly StyledProperty<bool> ShowBackButtonProperty =
        AvaloniaProperty.Register<MdContentPage, bool>(nameof(ShowBackButton), true);

    public static readonly StyledProperty<MdTopAppBarVariant> AppBarVariantProperty =
        AvaloniaProperty.Register<MdContentPage, MdTopAppBarVariant>(
            nameof(AppBarVariant),
            MdTopAppBarVariant.Small);

    public MdContentPage()
    {
        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch;
        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch;
    }

    public MdContentPage(string title, Control? content = null) : this()
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

    public MdTopAppBarVariant AppBarVariant
    {
        get => GetValue(AppBarVariantProperty);
        set => SetValue(AppBarVariantProperty, value);
    }

    public IReadOnlyList<MdPageAction> Actions => _actions;

    public Control? BottomBar { get; set; }

    public Control? FloatingActionButton { get; set; }

    /// <summary>
    /// Optional custom action for the app-bar leading button. When set, it takes
    /// precedence over the default back action.
    /// </summary>
    public Action? NavigationAction { get; set; }

    public MdNavigationPage? Navigation { get; internal set; }

    public bool IsInNavigation => Navigation is not null;

    public event EventHandler? Appearing;
    public event EventHandler? Disappearing;
    public event EventHandler? NavigationClicked;

    public MdPageAction AddAction(string icon, Action? onClick = null, string? tooltip = null)
    {
        var action = new MdPageAction
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

    internal void NotifyAppearing()
    {
        OnAppearing();
        Appearing?.Invoke(this, EventArgs.Empty);
    }

    internal void NotifyDisappearing()
    {
        // Dialogs, sheets and other overlays belong to the page that opened them.
        // Close them before the page leaves so they cannot outlive the page state.
        MdOverlay.DismissForPage(this);

        OnDisappearing();
        Disappearing?.Invoke(this, EventArgs.Empty);
    }

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

    protected virtual void OnAppearing()
    {
    }

    protected virtual void OnDisappearing()
    {
    }
}
