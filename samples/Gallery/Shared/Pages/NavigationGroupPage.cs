using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Mav;
using Mav.Controls;
using Mav.Theming;

namespace Mav.Gallery;

internal static class NavigationGroupPage
{
    public static Control Build()
    {
        return GalleryUi.Page(
            GalleryUi.Section("App bars", "Top app bars in three variants and a bottom app bar with a FAB.", AppBars()),
            GalleryUi.Section("Bars, rails and drawers", "The same destinations adapt from a bottom bar to a rail and a drawer.", NavStructures()),
            GalleryUi.Section("Tabs, breadcrumbs and steps", "Secondary navigation and progress through a flow.", TabsAndSteps()),
            GalleryUi.Section("Toolbars and contextual actions", "Floating/docked toolbars and a contextual action bar.", Toolbars()),
            GalleryUi.Section("Adaptive scaffold and page stack", "MavNavigationSuiteScaffold and a nested MavNavigationPage.", ScaffoldAndStack()));
    }

    private static Control AppBars()
    {
        var small = new MavTopAppBar("Inbox")
        {
            Subtitle = "12 messages",
            NavigationIcon = MavIcons.Menu,
            Variant = MavTopAppBarVariant.Small,
        };
        small.AddAction(MavIcons.Search);
        small.AddAction(MavIcons.MoreVert);

        var centered = new MavTopAppBar("Photos")
        {
            NavigationIcon = MavIcons.ArrowBack,
            Variant = MavTopAppBarVariant.CenterAligned,
        };
        centered.AddAction(MavIcons.Share);

        var large = new MavTopAppBar("Library")
        {
            NavigationIcon = MavIcons.Menu,
            Variant = MavTopAppBarVariant.Large,
        };

        var bottom = new MavBottomAppBar(MavIcons.Add, () => { });
        bottom.AddAction(MavIcons.Search, () => { });
        bottom.AddAction(MavIcons.Settings, () => { });

        return GalleryUi.Showcase(
            "MavTopAppBar and MavBottomAppBar",
            "Small, center-aligned and large top bars plus the bottom bar with a docked FAB and actions.",
            GalleryUi.Column(
                new Border { Width = 420, Child = small },
                new Border { Width = 420, Child = centered },
                new Border { Width = 420, Child = large },
                new Border { Width = 420, Child = bottom }),
            "MavTopAppBar(title) · Variant · AddAction · SafeAreaPadding · MavBottomAppBar(fabIcon, onFabClick)");
    }

    private static Control NavStructures()
    {
        var bar = new MavNavigationBar();
        bar.AddDestination("Home", MavIcons.Home);
        bar.AddDestination("Explore", MavIcons.Explore);
        bar.AddDestination("Saved", MavIcons.Bookmark);
        bar.AddDestination("Profile", MavIcons.Person);

        var rail = new MavNavigationRail(new MavAvatar { Initials = "MD" });
        rail.AddDestination("Home", MavIcons.Home);
        rail.AddDestination("Explore", MavIcons.Explore);
        rail.AddDestination("Saved", MavIcons.Bookmark);
        rail.AddDestination("Profile", MavIcons.Person);

        var drawer = new MavNavigationDrawer("Workspace", 260);
        drawer.AddItem("Inbox", MavIcons.Inbox, "12");
        drawer.AddItem("Starred", MavIcons.Star);
        drawer.AddItem("Sent", MavIcons.Send);
        drawer.AddItem("Trash", MavIcons.Delete);

        var modal = new MavModalNavigationDrawer("Modal drawer", 280);
        modal.AddItem("Inbox", MavIcons.Inbox);
        modal.AddItem("Starred", MavIcons.Star);
        modal.AddItem("Sent", MavIcons.Send);

        var openModal = new MavButton("Open modal drawer", MavButtonVariant.Outlined, MavIcons.MenuOpen);
        openModal.Clicked += (_, _) => modal.Show(openModal);

        return GalleryUi.Showcase(
            "MavNavigationBar, MavNavigationRail, MavNavigationDrawer and MavModalNavigationDrawer",
            "Bottom navigation, side rail, permanent drawer and modal drawer. The gallery shell uses all three adaptively.",
            GalleryUi.Column(
                new Border { Width = 420, Child = bar },
                GalleryUi.Row(
                    new Border { Width = 96, Height = 380, Child = rail },
                    new Border { Width = 280, Height = 380, Child = drawer })),
            "AddDestination(...) · AddItem(...) · SelectedIndex · Modal drawer Show(anchor)");
    }

    private static Control TabsAndSteps()
    {
        var tabs = new MavTabs();
        tabs.AddTab("Overview", MavIcons.Dashboard, content: new MavLabel { Text = "Overview tab content" });
        tabs.AddTab("Activity", MavIcons.Timeline, "3", new MavLabel { Text = "Activity tab content" });
        tabs.AddTab("Settings", MavIcons.Settings, content: new MavLabel { Text = "Settings tab content" });

        var breadcrumbs = new MavBreadcrumbBar();
        breadcrumbs.SetPath("Home", "Projects", "Mav");

        var pagination = new MavPagination { TotalPages = 12, CurrentPage = 4 };

        var stepper = new MavStepper();
        stepper.AddStep("Account", "Email and password", new MavLabel { Text = "Account details" });
        stepper.AddStep("Profile", "Name and avatar", new MavLabel { Text = "Profile details" });
        stepper.AddStep("Done", "Review and finish", new MavLabel { Text = "Review" });
        stepper.CurrentStep = 1;

        return GalleryUi.Showcase(
            "MavTabs, MavBreadcrumbBar, MavPagination and MavStepper",
            "Tabs switch panels, breadcrumbs show a trail, pagination moves through pages and the stepper tracks a flow.",
            GalleryUi.Column(
                tabs,
                breadcrumbs,
                pagination,
                new Border { Height = 260, Child = stepper }),
            "tabs.AddTab(text, icon, badge, content) · breadcrumbs.SetPath(...) · MavPagination.TotalPages · MavStepper.AddStep(...)");
    }

    private static Control Toolbars()
    {
        var toolbar = new MavToolbar(MavToolbarVariant.Floating, MavToolbarColorStyle.Standard, global::Avalonia.Layout.Orientation.Horizontal);
        toolbar.AddAction(MavIcons.FormatBold);
        toolbar.AddAction(MavIcons.FormatItalic);
        toolbar.AddAction(MavIcons.FormatUnderlined);
        toolbar.AddControl(new MavDivider { IsVertical = true });
        toolbar.AddAction(MavIcons.Link);

        var contextual = new MavContextualActionBar(selectedCount: 3);
        contextual.AddAction(MavIcons.Delete, "Delete");
        contextual.AddAction(MavIcons.Share, "Share");
        contextual.AddButton("Archive", MavIcons.Archive);

        return GalleryUi.Showcase(
            "MavToolbar and MavContextualActionBar",
            "A floating toolbar for editor actions and a contextual bar that appears while items are selected.",
            GalleryUi.Column(
                new Border { Width = 420, Child = toolbar },
                new Border { Width = 480, Child = contextual }),
            "toolbar.AddAction(icon) · AddControl · contextual.AddAction/AddButton");
    }

    private static Control ScaffoldAndStack()
    {
        var scaffold = new MavNavigationSuiteScaffold
        {
            TopAppBar = new MavTopAppBar("Adaptive"),
            Body = new MavLabel
            {
                Text = "Resize the window: bar → rail → drawer.",
                Margin = new Thickness(24),
            },
        };
        scaffold.AddDestination("Home", MavIcons.Home, MavIcons.Home, content: null);
        scaffold.AddDestination("Search", MavIcons.Search, MavIcons.Search, content: null);
        scaffold.AddDestination("Profile", MavIcons.Person, MavIcons.Person, content: null);

        var home = new MavContentPage("Page stack")
        {
            Content = new MavLabel
            {
                Text = "Root page. Push another page with the button.",
                Margin = new Thickness(16),
            },
        };
        var push = new MavButton("Push details", MavButtonVariant.Filled, MavIcons.ChevronRight);
        home.AddAction(MavIcons.MoreVert, () => { });
        home.Content = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(16),
            Children =
            {
                new MavLabel { Text = "Root page", Role = MavTypeRole.TitleMedium },
                push,
            },
        };

        var nested = new MavNavigationPage(home);
        var details = new MavContentPage("Details")
        {
            Content = new MavLabel
            {
                Text = "Details page. The app bar back arrow pops the stack.",
                Margin = new Thickness(16),
            },
        };
        push.Clicked += (_, _) => nested.Push(details);

        return GalleryUi.Showcase(
            "MavNavigationSuiteScaffold and MavNavigationPage",
            "The suite scaffold adapts its navigation to width; the nested page stack demonstrates MavContentPage and push/pop.",
            GalleryUi.Column(
                new Border { Height = 380, Child = scaffold },
                new Border { Height = 320, Child = nested }),
            "MavNavigationSuiteScaffold.AddDestination(...) · new MavNavigationPage(root) · Push/Pop");
    }
}
