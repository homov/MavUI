using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Mav;
using Mav.Controls;
using Mav.Theming;

namespace Mav.ControlsGallery.Pages;

/// <summary>
/// Demos that need the imperative navigation APIs (AddAction, AddDestination, AddTab, Push)
/// are built here into the named hosts declared in AXAML.
/// </summary>
public partial class NavigationGroupPage : UserControl
{
    public NavigationGroupPage()
    {
        InitializeComponent();

        AppBarsHost.Children.Add(new Border { MaxWidth = 420, Child = BuildSmallBar() });
        AppBarsHost.Children.Add(new Border { MaxWidth = 420, Child = BuildCenteredBar() });
        AppBarsHost.Children.Add(new Border { MaxWidth = 420, Child = BuildLargeBar() });
        AppBarsHost.Children.Add(new Border { MaxWidth = 420, Child = BuildBottomBar() });

        StructuresHost.Children.Add(new Border { MaxWidth = 420, Child = BuildBottomNavigation() });
        StructuresHost.Children.Add(BuildRailAndDrawer());

        TabsHost.Children.Add(Framed(BuildTabs()));
        TabsHost.Children.Add(Framed(BuildBreadcrumbs()));
        TabsHost.Children.Add(Framed(BuildPagination()));
        TabsHost.Children.Add(Framed(new Border { Height = 260, Child = BuildStepper() }));

        ToolbarsHost.Children.Add(new Border { MaxWidth = 420, Child = BuildToolbar() });
        ToolbarsHost.Children.Add(new Border { MaxWidth = 480, Child = BuildContextualBar() });

        ScaffoldHost.Children.Add(new Border { Height = 380, Child = BuildScaffold() });
        ScaffoldHost.Children.Add(new Border { Height = 320, Child = BuildPageStack() });
    }

    private static MavTopAppBar BuildSmallBar()
    {
        var bar = new MavTopAppBar(MavLocalization.Get("Nav.Demo.Inbox", "Inbox"))
        {
            Subtitle = MavLocalization.Get("Nav.Demo.Messages", "12 messages"),
            NavigationIcon = MavIcons.Menu,
            Variant = MavTopAppBarVariant.Small,
        };
        bar.AddAction(MavIcons.Search);
        bar.AddAction(MavIcons.MoreVert);
        return bar;
    }

    private static MavTopAppBar BuildCenteredBar()
    {
        var bar = new MavTopAppBar(MavLocalization.Get("Nav.Demo.Photos", "Photos"))
        {
            NavigationIcon = MavIcons.ArrowBack,
            Variant = MavTopAppBarVariant.CenterAligned,
        };
        bar.AddAction(MavIcons.Share);
        return bar;
    }

    private static MavTopAppBar BuildLargeBar() => new(MavLocalization.Get("Nav.Demo.Library", "Library"))
    {
        NavigationIcon = MavIcons.Menu,
        Variant = MavTopAppBarVariant.Large,
    };

    private static MavBottomAppBar BuildBottomBar()
    {
        var bar = new MavBottomAppBar(MavIcons.Add, () => { });
        bar.AddAction(MavIcons.Search, () => { });
        bar.AddAction(MavIcons.Settings, () => { });
        return bar;
    }

    private static MavNavigationBar BuildBottomNavigation()
    {
        var bar = new MavNavigationBar();
        bar.AddDestination(MavLocalization.Get("Nav.Demo.Home", "Home"), MavIcons.Home);
        bar.AddDestination(MavLocalization.Get("Nav.Demo.Explore", "Explore"), MavIcons.Explore);
        bar.AddDestination(MavLocalization.Get("Nav.Demo.Saved", "Saved"), MavIcons.Bookmark);
        bar.AddDestination(MavLocalization.Get("Nav.Demo.Profile", "Profile"), MavIcons.Person);
        return bar;
    }

    private static Control BuildRailAndDrawer()
    {
        var rail = new MavNavigationRail(new MavAvatar { Initials = "MD" });
        rail.AddDestination(MavLocalization.Get("Nav.Demo.Home", "Home"), MavIcons.Home);
        rail.AddDestination(MavLocalization.Get("Nav.Demo.Explore", "Explore"), MavIcons.Explore);
        rail.AddDestination(MavLocalization.Get("Nav.Demo.Saved", "Saved"), MavIcons.Bookmark);
        rail.AddDestination(MavLocalization.Get("Nav.Demo.Profile", "Profile"), MavIcons.Person);

        var drawer = new MavNavigationDrawer(MavLocalization.Get("Nav.Demo.Workspace", "Workspace"), 260);
        drawer.AddItem(MavLocalization.Get("Nav.Demo.Inbox", "Inbox"), MavIcons.Inbox, "12");
        drawer.AddItem(MavLocalization.Get("Nav.Demo.Starred", "Starred"), MavIcons.Star);
        drawer.AddItem(MavLocalization.Get("Nav.Demo.Sent", "Sent"), MavIcons.Send);
        drawer.AddItem(MavLocalization.Get("Nav.Demo.Trash", "Trash"), MavIcons.Delete);

        var modal = new MavModalNavigationDrawer(MavLocalization.Get("Nav.Demo.ModalDrawer", "Modal drawer"), 280);
        modal.AddItem(MavLocalization.Get("Nav.Demo.Inbox", "Inbox"), MavIcons.Inbox);
        modal.AddItem(MavLocalization.Get("Nav.Demo.Starred", "Starred"), MavIcons.Star);
        modal.AddItem(MavLocalization.Get("Nav.Demo.Sent", "Sent"), MavIcons.Send);

        var openModal = new MavButton(
            MavLocalization.Get("Nav.Demo.OpenModal", "Open modal drawer"),
            MavButtonVariant.Outlined,
            MavIcons.MenuOpen);
        openModal.Clicked += (_, _) => modal.Show(openModal);

        var row = new WrapPanel();
        row.Children.Add(new Border { Width = 96, Height = 380, Margin = new Thickness(0, 0, 8, 8), Child = rail });
        row.Children.Add(new Border { Width = 280, Height = 380, Margin = new Thickness(0, 0, 8, 8), Child = drawer });

        var stack = new StackPanel { Spacing = 8 };
        stack.Children.Add(openModal);
        stack.Children.Add(row);
        return stack;
    }

    private static MavTabs BuildTabs()
    {
        var tabs = new MavTabs();
        tabs.AddTab(MavLocalization.Get("Nav.Demo.Overview", "Overview"), MavIcons.Dashboard,
            content: new MavLabel { Text = MavLocalization.Get("Nav.Demo.OverviewTab", "Overview tab content") });
        tabs.AddTab(MavLocalization.Get("Nav.Demo.Activity", "Activity"), MavIcons.Timeline, "3",
            new MavLabel { Text = MavLocalization.Get("Nav.Demo.ActivityTab", "Activity tab content") });
        tabs.AddTab(MavLocalization.Get("Nav.Demo.Settings", "Settings"), MavIcons.Settings,
            content: new MavLabel { Text = MavLocalization.Get("Nav.Demo.SettingsTab", "Settings tab content") });
        return tabs;
    }

    /// <summary>Each demo gets its own outlined card, like the other sections; the card is as wide as the page.</summary>
    private static Control Framed(Control content) => new MavCard(MavCardVariant.Outlined, content);

    private static MavBreadcrumbBar BuildBreadcrumbs()
    {
        var breadcrumbs = new MavBreadcrumbBar();
        breadcrumbs.SetPath(
            MavLocalization.Get("Nav.Demo.Home", "Home"),
            MavLocalization.Get("Nav.Demo.Projects", "Projects"),
            "Mav");
        return breadcrumbs;
    }

    private static MavPagination BuildPagination() => new() { TotalPages = 12, CurrentPage = 4 };

    private static MavStepper BuildStepper()
    {
        var stepper = new MavStepper();
        stepper.AddStep(MavLocalization.Get("Nav.Demo.Account", "Account"),
            MavLocalization.Get("Nav.Demo.AccountSub", "Email and password"),
            new MavLabel { Text = MavLocalization.Get("Nav.Demo.AccountTab", "Account details") });
        stepper.AddStep(MavLocalization.Get("Nav.Demo.Profile", "Profile"),
            MavLocalization.Get("Nav.Demo.ProfileSub", "Name and avatar"),
            new MavLabel { Text = MavLocalization.Get("Nav.Demo.ProfileTab", "Profile details") });
        stepper.AddStep(MavLocalization.Get("Nav.Demo.Done", "Done"),
            MavLocalization.Get("Nav.Demo.DoneSub", "Review and finish"),
            new MavLabel { Text = MavLocalization.Get("Nav.Demo.Review", "Review") });
        stepper.CurrentStep = 1;
        return stepper;
    }

    private static MavToolbar BuildToolbar()
    {
        var toolbar = new MavToolbar(MavToolbarVariant.Floating, MavToolbarColorStyle.Standard,
            global::Avalonia.Layout.Orientation.Horizontal);
        toolbar.AddAction(MavIcons.FormatBold);
        toolbar.AddAction(MavIcons.FormatItalic);
        toolbar.AddAction(MavIcons.FormatUnderlined);
        toolbar.AddControl(new MavDivider { IsVertical = true });
        toolbar.AddAction(MavIcons.Link);
        return toolbar;
    }

    private static MavContextualActionBar BuildContextualBar()
    {
        var contextual = new MavContextualActionBar(selectedCount: 3);
        contextual.AddAction(MavIcons.Delete, MavLocalization.Get("Nav.Demo.Delete", "Delete"));
        contextual.AddAction(MavIcons.Share, MavLocalization.Get("Nav.Demo.Share", "Share"));
        contextual.AddButton(MavLocalization.Get("Nav.Demo.Archive", "Archive"), MavIcons.Archive);
        return contextual;
    }

    private static MavNavigationSuiteScaffold BuildScaffold()
    {
        var scaffold = new MavNavigationSuiteScaffold
        {
            TopAppBar = new MavTopAppBar(MavLocalization.Get("Nav.Demo.Adaptive", "Adaptive")),
            Body = new MavLabel
            {
                Text = MavLocalization.Get("Nav.Demo.Resize", "Resize the window: bar → rail → drawer."),
                Margin = new Thickness(24),
            },
        };
        scaffold.AddDestination(MavLocalization.Get("Nav.Demo.Home", "Home"), MavIcons.Home, MavIcons.Home, content: null);
        scaffold.AddDestination(MavLocalization.Get("Nav.Demo.Search", "Search"), MavIcons.Search, MavIcons.Search, content: null);
        scaffold.AddDestination(MavLocalization.Get("Nav.Demo.Profile", "Profile"), MavIcons.Person, MavIcons.Person, content: null);
        return scaffold;
    }

    private static MavNavigationPage BuildPageStack()
    {
        var push = new MavButton(
            MavLocalization.Get("Nav.Demo.Push", "Push details"),
            MavButtonVariant.Filled,
            MavIcons.ChevronRight);

        var root = new MavContentPage(MavLocalization.Get("Nav.Demo.PageStack", "Page stack"));
        root.AddAction(MavIcons.MoreVert, () => { });
        root.Content = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(16),
            Children =
            {
                new MavLabel { Text = MavLocalization.Get("Nav.Demo.RootPage", "Root page"), Role = MavTypeRole.TitleMedium },
                new MavLabel
                {
                    Text = MavLocalization.Get("Nav.Demo.RootHint", "Root page. Push another page with the button."),
                    TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                },
                push,
            },
        };

        var nested = new MavNavigationPage(root);
        var details = new MavContentPage(MavLocalization.Get("Nav.Demo.Details", "Details"))
        {
            Content = new MavLabel
            {
                Text = MavLocalization.Get("Nav.Demo.DetailsHint", "Details page. The app bar back arrow pops the stack."),
                Margin = new Thickness(16),
            },
        };
        push.Clicked += (_, _) => nested.Push(details);
        return nested;
    }
}
