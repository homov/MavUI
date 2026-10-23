using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.ControlsGallery.Pages;

public partial class LayoutPage : UserControl
{
    public LayoutPage()
    {
        InitializeComponent();

        SurfacesHost.Children.Add(BuildSurfaces());
        ExpandablesHost.Children.Add(BuildExpandables());
        ResponsiveHost.Children.Add(BuildResponsive());
        PeopleHost.Children.Add(BuildPeople());
        WindowHost.Children.Add(BuildWindow());
        InsetsHost.Children.Add(BuildInsets());
    }

    private static Control Row(params Control[] children)
    {
        var panel = new WrapPanel();
        foreach (var child in children)
        {
            child.Margin = new Thickness(0, 0, 8, 8);
            panel.Children.Add(child);
        }
        return panel;
    }

    private static Control Column(params Control[] children)
    {
        var stack = new StackPanel { Spacing = 8 };
        foreach (var child in children)
            stack.Children.Add(child);
        return stack;
    }

    private static Control BuildSurfaces()
    {
        var elevated = new MavCard(MavCardVariant.Elevated, new MavLabel { Text = "Elevated card" });
        var filled = new MavCard(MavCardVariant.Filled, new MavLabel { Text = "Filled card" });
        var outlined = new MavCard(MavCardVariant.Outlined, new MavLabel { Text = "Outlined card" });

        var surface = new MavSurface
        {
            Width = 200,
            Height = 88,
            Radius = 16,
            Elevation = 2,
            FillColor = MavTheme.ColorOf(MavColorRole.SurfaceContainerHigh),
            Content = new MavLabel
            {
                Text = "MavSurface",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };

        var labels = Column(
            new MavLabel { Text = "TitleLarge", Role = MavTypeRole.TitleLarge },
            new MavLabel { Text = "BodyMedium secondary", Role = MavTypeRole.BodyMedium, Secondary = true },
            new MavLabel { Text = "Label with primary color", Role = MavTypeRole.LabelLarge, ColorRole = MavColorRole.Primary });

        var vertical = new MavDivider { IsVertical = true, Height = 32 };
        var badge = new MavBadge("3");
        var badged = new MavBadgedBox(new MavIconButton(MavIcons.Notifications), "5");

        return Column(
            Row(elevated, filled, outlined, surface),
            labels,
            new MavDivider(),
            Row(vertical, badge, badged));
    }

    private static Control BuildExpandables()
    {
        var panel = new MavExpansionPanel
        {
            Header = "Release notes",
            IsExpanded = true,
            Content = new MavLabel
            {
                Text = "Expansion panels keep secondary content out of the way until it is needed.",
                TextWrapping = TextWrapping.Wrap,
            },
        };

        var list = new MavExpansionPanelList { SingleExpand = true };
        list.AddPanel(new MavExpansionPanel { Header = "Install", Content = new MavLabel { Text = "dotnet add package Mav" } });
        list.AddPanel(new MavExpansionPanel { Header = "Theme", Content = new MavLabel { Text = "MavTheme.Configure(seed: ...)" } });

        var settings = new MavSettingsCard
        {
            Header = "Appearance",
            Title = "Dark mode",
            Description = "Follow the system theme or force a mode",
            Icon = MavIcons.DarkMode,
            ActionContent = new MavSwitch(MavTheme.Current.IsDark, showIcons: true),
        };

        var expander = new MavSettingsExpander { Header = "Notifications", IsExpanded = true };
        expander.AddItem("Email", "Daily digest", new MavSwitch(true), MavIcons.Email);
        expander.AddItem("Push", "Mentions and replies", new MavSwitch(), MavIcons.Notifications);

        return Column(panel, list, settings, expander);
    }

    private static Control BuildResponsive()
    {
        var split = new MavSplitView
        {
            PaneWidth = 180,
            MinPaneWidth = 140,
            MaxPaneWidth = 320,
            IsResizable = true,
            Pane = new MavCard(MavCardVariant.Filled, new MavLabel { Text = "Pane" }),
        };

        // On narrow windows the pane becomes a drawer that closes on a tap outside it; this button opens it again.
        var toggle = new MavButton(
            MavLocalization.Get("Layout.Split.Toggle", "Toggle pane"),
            MavButtonVariant.Tonal,
            MavIcons.Menu);
        toggle.Clicked += (_, _) => split.IsPaneOpen = !split.IsPaneOpen;
        split.DetailContent = new MavCard(MavCardVariant.Outlined, Column(
            new MavLabel { Text = "Detail content" },
            toggle));

        var carousel = new MavCarousel { Height = 220 };
        carousel.AddItem("Material 3", "Tonal palettes and shape", MavIcons.Palette);
        carousel.AddItem("Adaptive", "Phone, tablet and desktop", MavIcons.Devices);
        carousel.AddItem("Pure C#", "No AXAML templates", MavIcons.Code);

        var longContent = new StackPanel { Spacing = 8 };
        for (var i = 1; i <= 12; i++)
            longContent.Children.Add(new MavLabel { Text = $"Scrollable row {i}", Role = MavTypeRole.BodyMedium });

        var scroll = new MavScrollViewer { Height = 180, Content = longContent };

        return Column(
            new Border { Height = 260, Child = split },
            carousel,
            new MavCard(MavCardVariant.Outlined, scroll));
    }

    private static Control BuildPeople()
    {
        var avatar = new MavAvatar("AL", size: 48, status: MavAvatarStatus.Online);
        var group = new MavAvatarGroup();
        foreach (var initials in new[] { "AL", "GH", "AT", "KJ", "LT", "MH" })
            group.AddAvatar(initials);

        var drop = new MavFileDropZone("Drop files here", "or browse from the device", allowMultiple: true);
        drop.AddFile("design-spec.pdf", 248_000);
        drop.AddFile("screenshot.png", 512_000);

        return Column(
            Row(avatar, group),
            new Border { MaxWidth = 460, Child = drop },
            new MavChatBubble("The gallery builds on desktop, web and Android.", false, "Ada", "10:24", "AL"),
            new MavChatBubble("I can see every control in one place.", true, "You", "10:26", "YO", isRead: true));
    }

    private static Control BuildWindow()
    {
        if (OperatingSystem.IsBrowser() || OperatingSystem.IsAndroid())
        {
            return new MavLabel
            {
                Text = MavLocalization.Get("Layout.Window.Unsupported",
                    "MavWindow is a desktop-only TopLevel and is not available on this platform."),
                TextWrapping = TextWrapping.Wrap,
            };
        }

        var button = new MavButton(
            MavLocalization.Get("Layout.Window.Open", "Open a new window"),
            MavButtonVariant.Filled,
            MavIcons.Window);
        button.Clicked += (_, _) =>
        {
            var window = new MavWindow
            {
                Title = "MavWindow demo",
                Width = 520,
                Height = 360,
                Content = new MavCard(
                    MavCardVariant.Filled,
                    new MavLabel
                    {
                        Text = MavLocalization.Get("Layout.Window.Body", "This is a second MavWindow with its own overlay layer."),
                        Margin = new Thickness(24),
                    }),
            };
            window.Show();
        };
        return button;
    }

    private static Control BuildInsets()
    {
        var safe = new MavSafeArea
        {
            Edges = MavSafeAreaEdges.All,
            SimulatedInset = new Thickness(16, 24, 16, 24),
            BasePadding = new Thickness(8),
            Child = new MavLabel
            {
                Text = "Content inside simulated safe-area insets",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
            Background = MavTheme.BrushOf(MavTheme.ColorOf(MavColorRole.SurfaceContainerLow)),
            Height = 140,
        };

        var keyboard = new MavKeyboardAvoidingHost
        {
            SimulatedKeyboardHeight = 120,
            Child = new MavLabel
            {
                Text = "Content lifted above a simulated 120 dp keyboard",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
            Height = 220,
            Background = MavTheme.BrushOf(MavTheme.ColorOf(MavColorRole.SurfaceContainerLow)),
        };

        var wrap = new AdaptiveWrap { MinItemWidth = 300 };
        wrap.Children.Add(new Border { Child = safe });
        wrap.Children.Add(new Border { Child = keyboard });
        return wrap;
    }
}
