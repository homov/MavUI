using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Theming;
using static Mav.ControlsGalleryMarkup.Pages.PageParts;

namespace Mav.ControlsGalleryMarkup.Pages;

/// <summary>App bars, navigation structures, tabs, toolbars and the page stack. Mirrors the AXAML Navigation page.</summary>
public static class NavigationGroupPage
{
    public static Control Build()
    {
        var push = new MavButton(L("Nav.Demo.Push", "Push details"), MavButtonVariant.Filled, MavIcons.ChevronRight);
        var root = new MavContentPage(L("Nav.Demo.PageStack", "Page stack"),
            Stack(12,
                new MavLabel(L("Nav.Demo.RootPage", "Root page"), MavTypeRole.TitleMedium),
                new MavLabel(L("Nav.Demo.RootHint", "Root page. Push another page with the button.")).TextWrapping(TextWrapping.Wrap),
                push).Margin(new Thickness(16)));
        root.AddAction(MavIcons.MoreVert);
        var stack = new MavNavigationPage(root);
        push.OnClicked(() => stack.Push(DetailsPage()));

        return Page(
            Section(
                "Nav.Bars.AppBars.Section", "App bars",
                "Nav.Bars.AppBars.Section.Body", "Top app bars in three variants and a bottom app bar with a FAB.",
                Card(
                    "MavTopAppBar and MavBottomAppBar",
                    "Nav.Bars.AppBars.Card.Body", "Small, center-aligned and large top bars plus the bottom bar with a docked FAB and actions.",
                    "MavTopAppBar(title) · Variant · AddAction · SafeAreaPadding · MavBottomAppBar(fabIcon, onFabClick)",
                    Stack(8,
                        Bounded(new MavTopAppBar(L("Nav.Demo.Inbox", "Inbox"), MavTopAppBarVariant.Small, MavIcons.Menu,
                                L("Nav.Demo.Messages", "12 messages"))
                            .AddAction(MavIcons.Search)
                            .AddAction(MavIcons.MoreVert)),
                        Bounded(new MavTopAppBar(L("Nav.Demo.Photos", "Photos"), MavTopAppBarVariant.CenterAligned, MavIcons.ArrowBack)
                            .AddAction(MavIcons.Share)),
                        Bounded(new MavTopAppBar(L("Nav.Demo.Library", "Library"), MavTopAppBarVariant.Large, MavIcons.Menu)),
                        Bounded(new MavBottomAppBar()
                            .AddAction(MavIcons.Search)
                            .AddAction(MavIcons.Settings)))))),
            Section(
                "Nav.Bars.Structures.Section", "Bars, rails and drawers",
                "Nav.Bars.Structures.Section.Body", "The same destinations adapt from a bottom bar to a rail and a drawer.",
                Card(
                    "MavNavigationBar, MavNavigationRail, MavNavigationDrawer and MavModalNavigationDrawer",
                    "Nav.Bars.Structures.Card.Body", "Bottom navigation, side rail, permanent drawer and modal drawer. The gallery shell uses all three adaptively.",
                    "AddDestination(...) · AddItem(...) · SelectedIndex · Modal drawer Show(anchor)",
                    Stack(8,
                        Bounded(new MavNavigationBar()
                            .AddDestination(L("Nav.Demo.Home", "Home"), MavIcons.Home)
                            .AddDestination(L("Nav.Demo.Explore", "Explore"), MavIcons.Explore)
                            .AddDestination(L("Nav.Demo.Saved", "Saved"), MavIcons.Bookmark)
                            .AddDestination(L("Nav.Demo.Profile", "Profile"), MavIcons.Person)),
                        Stack(8,
                            OpenDrawer(
                                new MavButton(L("Nav.Demo.OpenModal", "Open modal drawer"), MavButtonVariant.Outlined, MavIcons.MenuOpen),
                                new MavModalNavigationDrawer(L("Nav.Demo.ModalDrawer", "Modal drawer"), 280)
                                    .AddItem(L("Nav.Demo.Inbox", "Inbox"), MavIcons.Inbox)
                                    .AddItem(L("Nav.Demo.Starred", "Starred"), MavIcons.Star)
                                    .AddItem(L("Nav.Demo.Sent", "Sent"), MavIcons.Send)),
                            Row(
                                new Border().Width(96).Height(380).Child(
                                    new MavNavigationRail(new MavAvatar().Initials("MD"))
                                        .AddDestination(L("Nav.Demo.Home", "Home"), MavIcons.Home)
                                        .AddDestination(L("Nav.Demo.Explore", "Explore"), MavIcons.Explore)
                                        .AddDestination(L("Nav.Demo.Saved", "Saved"), MavIcons.Bookmark)
                                        .AddDestination(L("Nav.Demo.Profile", "Profile"), MavIcons.Person)),
                                new Border().Width(280).Height(380).Child(
                                    new MavNavigationDrawer(L("Nav.Demo.Workspace", "Workspace"), 260)
                                        .AddItem(L("Nav.Demo.Inbox", "Inbox"), MavIcons.Inbox, "12")
                                        .AddItem(L("Nav.Demo.Starred", "Starred"), MavIcons.Star)
                                        .AddItem(L("Nav.Demo.Sent", "Sent"), MavIcons.Send)
                                        .AddItem(L("Nav.Demo.Trash", "Trash"), MavIcons.Delete)))))))),
            Section(
                "Nav.Bars.Tabs.Section", "Tabs, breadcrumbs and steps",
                "Nav.Bars.Tabs.Section.Body", "Secondary navigation and progress through a flow.",
                Card(
                    "MavTabs, MavBreadcrumbBar, MavPagination and MavStepper",
                    "Nav.Bars.Tabs.Card.Body", "Tabs switch panels, breadcrumbs show a trail, pagination moves through pages and the stepper tracks a flow.",
                    "AddTab(text, icon, badge, content) · SetPath(...) · MavPagination.TotalPages · AddStep(...)",
                    Stack(8,
                        Outlined(new MavTabs()
                            .AddTab(L("Nav.Demo.Overview", "Overview"), MavIcons.Dashboard, null,
                                new MavLabel(L("Nav.Demo.OverviewTab", "Overview tab content")))
                            .AddTab(L("Nav.Demo.Activity", "Activity"), MavIcons.Timeline, "3",
                                new MavLabel(L("Nav.Demo.ActivityTab", "Activity tab content")))
                            .AddTab(L("Nav.Demo.Settings", "Settings"), MavIcons.Settings, null,
                                new MavLabel(L("Nav.Demo.SettingsTab", "Settings tab content")))),
                        Outlined(Crumbs(
                            L("Nav.Demo.Home", "Home"),
                            L("Nav.Demo.Projects", "Projects"),
                            "Mav")),
                        Outlined(new MavPagination(12, 4)),
                        Outlined(new Border().Height(260).Child(
                            new MavStepper()
                                .AddStep(L("Nav.Demo.Account", "Account"), L("Nav.Demo.AccountSub", "Email and password"),
                                    new MavLabel(L("Nav.Demo.AccountTab", "Account details")))
                                .AddStep(L("Nav.Demo.Profile", "Profile"), L("Nav.Demo.ProfileSub", "Name and avatar"),
                                    new MavLabel(L("Nav.Demo.ProfileTab", "Profile details")))
                                .AddStep(L("Nav.Demo.Done", "Done"), L("Nav.Demo.DoneSub", "Review and finish"),
                                    new MavLabel(L("Nav.Demo.Review", "Review")))
                                .CurrentStep(1))))))),
            Section(
                "Nav.Bars.Toolbars.Section", "Toolbars and contextual actions",
                "Nav.Bars.Toolbars.Section.Body", "Floating/docked toolbars and a contextual action bar.",
                Card(
                    "MavToolbar and MavContextualActionBar",
                    "Nav.Bars.Toolbars.Card.Body", "A floating toolbar for editor actions and a contextual bar that appears while items are selected.",
                    "toolbar.AddAction(icon) · AddDivider · contextual.AddAction/AddButton",
                    Stack(8,
                        Bounded(new MavToolbar()
                            .AddAction(MavIcons.FormatBold)
                            .AddAction(MavIcons.FormatItalic)
                            .AddAction(MavIcons.FormatUnderlined)
                            .AddDivider()
                            .AddAction(MavIcons.Link)),
                        Bounded(new MavContextualActionBar(3)
                                .AddAction(MavIcons.Delete, L("Nav.Demo.Delete", "Delete"))
                                .AddAction(MavIcons.Share, L("Nav.Demo.Share", "Share"))
                                .AddButton(L("Nav.Demo.Archive", "Archive"), MavIcons.Archive),
                            480)))),
            Section(
                "Nav.Bars.Scaffold.Section", "Adaptive scaffold and page stack",
                "Nav.Bars.Scaffold.Section.Body", "MavNavigationSuiteScaffold and a nested MavNavigationPage.",
                Card(
                    "MavNavigationSuiteScaffold and MavNavigationPage",
                    "Nav.Bars.Scaffold.Card.Body", "The suite scaffold adapts its navigation to width; the nested page stack demonstrates MavContentPage and push/pop.",
                    "MavNavigationSuiteScaffold.AddDestination(...) · new MavNavigationPage(root) · Push/Pop",
                    new Border().Height(380).Child(
                        new MavNavigationSuiteScaffold()
                            .TopAppBar(new MavTopAppBar(L("Nav.Demo.Adaptive", "Adaptive")))
                            .Body(new MavLabel(L("Nav.Demo.Resize", "Resize the window: bar → rail → drawer."))
                                .Margin(new Thickness(24)))
                            .AddDestination(L("Nav.Demo.Home", "Home"), MavIcons.Home, MavIcons.Home)
                            .AddDestination(L("Nav.Demo.Search", "Search"), MavIcons.Search, MavIcons.Search)
                            .AddDestination(L("Nav.Demo.Profile", "Profile"), MavIcons.Person, MavIcons.Person)),
                    new Border().Height(320).Child(stack))))));
    }

    private static MavContentPage DetailsPage() =>
        new MavContentPage(L("Nav.Demo.Details", "Details"),
            new MavLabel(L("Nav.Demo.DetailsHint", "Details page. The app bar back arrow pops the stack."))
                .Margin(new Thickness(16))
                .TextWrapping(TextWrapping.Wrap));

    private static StackPanel Stack(double spacing, params Control[] items) =>
        new StackPanel().Spacing(spacing).Children(items);

    private static Control Bounded(Control control, double maxWidth = 420) =>
        new Border().MaxWidth(maxWidth).Child(control);

    private static Control Outlined(Control content) =>
        new MavCard(MavCardVariant.Outlined, content);

    /// <summary>Breadcrumb bar with the given path. SetPath returns nothing, so the bar is built first.</summary>
    private static MavBreadcrumbBar Crumbs(params string[] path)
    {
        var bar = new MavBreadcrumbBar();
        bar.SetPath(path);
        return bar;
    }

    /// <summary>Attaches a modal navigation drawer to the button; the button opens it.</summary>
    private static MavButton OpenDrawer(MavButton button, MavModalNavigationDrawer drawer)
    {
        MavFlyout.SetModalDrawer(button, drawer);
        return button;
    }
}
