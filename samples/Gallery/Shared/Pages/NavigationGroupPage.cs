using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Theming;

namespace Md3.Gallery;

internal static class NavigationGroupPage
{
    public static Control Build()
    {
        return GalleryUi.Page(
            GalleryUi.Section("App bars", "Top app bars in three variants and a bottom app bar with a FAB.", AppBars()),
            GalleryUi.Section("Bars, rails and drawers", "The same destinations adapt from a bottom bar to a rail and a drawer.", NavStructures()),
            GalleryUi.Section("Tabs, breadcrumbs and steps", "Secondary navigation and progress through a flow.", TabsAndSteps()),
            GalleryUi.Section("Toolbars and contextual actions", "Floating/docked toolbars and a contextual action bar.", Toolbars()),
            GalleryUi.Section("Adaptive scaffold and page stack", "MdNavigationSuiteScaffold and a nested MdNavigationPage.", ScaffoldAndStack()));
    }

    private static Control AppBars()
    {
        var small = new MdTopAppBar("Inbox")
        {
            Subtitle = "12 messages",
            NavigationIcon = MdIcons.Menu,
            Variant = MdTopAppBarVariant.Small,
        };
        small.AddAction(MdIcons.Search);
        small.AddAction(MdIcons.MoreVert);

        var centered = new MdTopAppBar("Photos")
        {
            NavigationIcon = MdIcons.ArrowBack,
            Variant = MdTopAppBarVariant.CenterAligned,
        };
        centered.AddAction(MdIcons.Share);

        var large = new MdTopAppBar("Library")
        {
            NavigationIcon = MdIcons.Menu,
            Variant = MdTopAppBarVariant.Large,
        };

        var bottom = new MdBottomAppBar(MdIcons.Add, () => { });
        bottom.AddAction(MdIcons.Search, () => { });
        bottom.AddAction(MdIcons.Settings, () => { });

        return GalleryUi.Showcase(
            "MdTopAppBar and MdBottomAppBar",
            "Small, center-aligned and large top bars plus the bottom bar with a docked FAB and actions.",
            GalleryUi.Column(
                new Border { Width = 420, Child = small },
                new Border { Width = 420, Child = centered },
                new Border { Width = 420, Child = large },
                new Border { Width = 420, Child = bottom }),
            "MdTopAppBar(title) · Variant · AddAction · SafeAreaPadding · MdBottomAppBar(fabIcon, onFabClick)");
    }

    private static Control NavStructures()
    {
        var bar = new MdNavigationBar();
        bar.AddDestination("Home", MdIcons.Home);
        bar.AddDestination("Explore", MdIcons.Explore);
        bar.AddDestination("Saved", MdIcons.Bookmark);
        bar.AddDestination("Profile", MdIcons.Person);

        var rail = new MdNavigationRail(new MdAvatar { Initials = "MD" });
        rail.AddDestination("Home", MdIcons.Home);
        rail.AddDestination("Explore", MdIcons.Explore);
        rail.AddDestination("Saved", MdIcons.Bookmark);
        rail.AddDestination("Profile", MdIcons.Person);

        var drawer = new MdNavigationDrawer("Workspace", 260);
        drawer.AddItem("Inbox", MdIcons.Inbox, "12");
        drawer.AddItem("Starred", MdIcons.Star);
        drawer.AddItem("Sent", MdIcons.Send);
        drawer.AddItem("Trash", MdIcons.Delete);

        var modal = new MdModalNavigationDrawer("Modal drawer", 280);
        modal.AddItem("Inbox", MdIcons.Inbox);
        modal.AddItem("Starred", MdIcons.Star);
        modal.AddItem("Sent", MdIcons.Send);

        var openModal = new MdButton("Open modal drawer", MdButtonVariant.Outlined, MdIcons.MenuOpen);
        openModal.Clicked += (_, _) => modal.Show(openModal);

        return GalleryUi.Showcase(
            "MdNavigationBar, MdNavigationRail, MdNavigationDrawer and MdModalNavigationDrawer",
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
        var tabs = new MdTabs();
        tabs.AddTab("Overview", MdIcons.Dashboard, content: new MdLabel { Text = "Overview tab content" });
        tabs.AddTab("Activity", MdIcons.Timeline, "3", new MdLabel { Text = "Activity tab content" });
        tabs.AddTab("Settings", MdIcons.Settings, content: new MdLabel { Text = "Settings tab content" });

        var breadcrumbs = new MdBreadcrumbBar();
        breadcrumbs.SetPath("Home", "Projects", "Md3.Avalonia");

        var pagination = new MdPagination { TotalPages = 12, CurrentPage = 4 };

        var stepper = new MdStepper();
        stepper.AddStep("Account", "Email and password", new MdLabel { Text = "Account details" });
        stepper.AddStep("Profile", "Name and avatar", new MdLabel { Text = "Profile details" });
        stepper.AddStep("Done", "Review and finish", new MdLabel { Text = "Review" });
        stepper.CurrentStep = 1;

        return GalleryUi.Showcase(
            "MdTabs, MdBreadcrumbBar, MdPagination and MdStepper",
            "Tabs switch panels, breadcrumbs show a trail, pagination moves through pages and the stepper tracks a flow.",
            GalleryUi.Column(
                tabs,
                breadcrumbs,
                pagination,
                new Border { Height = 260, Child = stepper }),
            "tabs.AddTab(text, icon, badge, content) · breadcrumbs.SetPath(...) · MdPagination.TotalPages · MdStepper.AddStep(...)");
    }

    private static Control Toolbars()
    {
        var toolbar = new MdToolbar(MdToolbarVariant.Floating, MdToolbarColorStyle.Standard, global::Avalonia.Layout.Orientation.Horizontal);
        toolbar.AddAction(MdIcons.FormatBold);
        toolbar.AddAction(MdIcons.FormatItalic);
        toolbar.AddAction(MdIcons.FormatUnderlined);
        toolbar.AddControl(new MdDivider { IsVertical = true });
        toolbar.AddAction(MdIcons.Link);

        var contextual = new MdContextualActionBar(selectedCount: 3);
        contextual.AddAction(MdIcons.Delete, "Delete");
        contextual.AddAction(MdIcons.Share, "Share");
        contextual.AddButton("Archive", MdIcons.Archive);

        return GalleryUi.Showcase(
            "MdToolbar and MdContextualActionBar",
            "A floating toolbar for editor actions and a contextual bar that appears while items are selected.",
            GalleryUi.Column(
                new Border { Width = 420, Child = toolbar },
                new Border { Width = 480, Child = contextual }),
            "toolbar.AddAction(icon) · AddControl · contextual.AddAction/AddButton");
    }

    private static Control ScaffoldAndStack()
    {
        var scaffold = new MdNavigationSuiteScaffold
        {
            TopAppBar = new MdTopAppBar("Adaptive"),
            Body = new MdLabel
            {
                Text = "Resize the window: bar → rail → drawer.",
                Margin = new Thickness(24),
            },
        };
        scaffold.AddDestination("Home", MdIcons.Home, MdIcons.Home, content: null);
        scaffold.AddDestination("Search", MdIcons.Search, MdIcons.Search, content: null);
        scaffold.AddDestination("Profile", MdIcons.Person, MdIcons.Person, content: null);

        var home = new MdContentPage("Page stack")
        {
            Content = new MdLabel
            {
                Text = "Root page. Push another page with the button.",
                Margin = new Thickness(16),
            },
        };
        var push = new MdButton("Push details", MdButtonVariant.Filled, MdIcons.ChevronRight);
        home.AddAction(MdIcons.MoreVert, () => { });
        home.Content = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(16),
            Children =
            {
                new MdLabel { Text = "Root page", Role = MdTypeRole.TitleMedium },
                push,
            },
        };

        var nested = new MdNavigationPage(home);
        var details = new MdContentPage("Details")
        {
            Content = new MdLabel
            {
                Text = "Details page. The app bar back arrow pops the stack.",
                Margin = new Thickness(16),
            },
        };
        push.Clicked += (_, _) => nested.Push(details);

        return GalleryUi.Showcase(
            "MdNavigationSuiteScaffold and MdNavigationPage",
            "The suite scaffold adapts its navigation to width; the nested page stack demonstrates MdContentPage and push/pop.",
            GalleryUi.Column(
                new Border { Height = 380, Child = scaffold },
                new Border { Height = 320, Child = nested }),
            "MdNavigationSuiteScaffold.AddDestination(...) · new MdNavigationPage(root) · Push/Pop");
    }
}
