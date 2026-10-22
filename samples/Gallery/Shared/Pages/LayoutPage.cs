using Avalonia;
using Avalonia.Media;
using Md3.Avalonia.Primitives;
using Avalonia.Controls;
using Avalonia.Layout;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Theming;

namespace Md3.Gallery;

internal static class LayoutPage
{
    public static Control Build()
    {
        return GalleryUi.Page(
            GalleryUi.Section("Surfaces", "Cards, surfaces, labels, dividers and badges.", Surfaces()),
            GalleryUi.Section("Expandable and settings", "Expansion panels, settings cards and settings expanders.", Expandables()),
            GalleryUi.Section("Responsive containers", "Split view, carousel and the theme-independent scroll viewer.", Responsive()),
            GalleryUi.Section("People and messages", "Avatars, avatar groups, file drop and chat bubbles.", People()),
            GalleryUi.Section("Insets and keyboard", "Safe-area and keyboard-aware hosts with simulated insets.", Insets()),
            WindowSection());
    }

    private static Control Surfaces()
    {
        var elevated = new MdCard(MdCardVariant.Elevated, new MdLabel { Text = "Elevated card" });
        var filled = new MdCard(MdCardVariant.Filled, new MdLabel { Text = "Filled card" });
        var outlined = new MdCard(MdCardVariant.Outlined, new MdLabel { Text = "Outlined card" });

        var surface = new MdSurface
        {
            Width = 200,
            Height = 88,
            Radius = 16,
            Elevation = 2,
            FillColor = MdTheme.ColorOf(MdColorRole.SurfaceContainerHigh),
            Content = new MdLabel
            {
                Text = "MdSurface",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };

        var labels = GalleryUi.Column(
            new MdLabel { Text = "TitleLarge", Role = MdTypeRole.TitleLarge },
            new MdLabel { Text = "BodyMedium secondary", Role = MdTypeRole.BodyMedium, Secondary = true },
            new MdLabel { Text = "Label with primary color", Role = MdTypeRole.LabelLarge, ColorRole = MdColorRole.Primary });

        var divider = new MdDivider();
        var vertical = new MdDivider { IsVertical = true, Height = 32 };

        var badge = new MdBadge("3");
        var badged = new MdBadgedBox(new MdIconButton(MdIcons.Notifications), "5");

        return GalleryUi.Showcase(
            "MdCard, MdSurface, MdLabel, MdDivider, MdBadge and MdBadgedBox",
            "MD3 surfaces with two-layer elevation, typography-aware labels, dividers and notification badges.",
            GalleryUi.Column(
                GalleryUi.Row(elevated, filled, outlined, surface),
                labels,
                divider,
                GalleryUi.Row(vertical, badge, badged)),
            "new MdCard(variant, body) · MdSurface.Elevation · MdLabel.Role · MdBadge.Count");
    }

    private static Control Expandables()
    {
        var panel = new MdExpansionPanel
        {
            Header = "Release notes",
            IsExpanded = true,
            Body = new MdLabel
            {
                Text = "Expansion panels keep secondary content out of the way until it is needed.",
                TextWrapping = TextWrapping.Wrap,
            },
        };

        var list = new MdExpansionPanelList { SingleExpand = true };
        list.AddPanel(new MdExpansionPanel { Header = "Install", Body = new MdLabel { Text = "dotnet add package Md3.Avalonia" } });
        list.AddPanel(new MdExpansionPanel { Header = "Theme", Body = new MdLabel { Text = "MdTheme.Configure(seed: ...)" } });

        var settings = new MdSettingsCard
        {
            Header = "Appearance",
            Title = "Dark mode",
            Description = "Follow the system theme or force a mode",
            Icon = MdIcons.DarkMode,
            ActionContent = new MdSwitch(MdTheme.Current.IsDark, showIcons: true),
        };

        var expander = new MdSettingsExpander { Header = "Notifications", IsExpanded = true };
        expander.AddItem("Email", "Daily digest", new MdSwitch(true), MdIcons.Email);
        expander.AddItem("Push", "Mentions and replies", new MdSwitch(), MdIcons.Notifications);

        return GalleryUi.Showcase(
            "MdExpansionPanel, MdExpansionPanelList, MdSettingsCard and MdSettingsExpander",
            "Progressive disclosure for details, settings groups and compact forms.",
            GalleryUi.Column(panel, list, settings, expander),
            "MdExpansionPanel.IsExpanded · MdExpansionPanelList.SingleExpand · MdSettingsCard.ActionContent");
    }

    private static Control Responsive()
    {
        var split = new MdSplitView
        {
            PaneWidth = 180,
            MinPaneWidth = 140,
            MaxPaneWidth = 320,
            IsResizable = true,
            Pane = new MdCard(MdCardVariant.Filled, new MdLabel { Text = "Pane" }),
            DetailContent = new MdCard(MdCardVariant.Outlined, new MdLabel { Text = "Detail content" }),
        };

        var carousel = new MdCarousel { Height = 220 };
        carousel.AddItem("Material 3", "Tonal palettes and shape", MdIcons.Palette);
        carousel.AddItem("Adaptive", "Phone, tablet and desktop", MdIcons.Devices);
        carousel.AddItem("Pure C#", "No AXAML templates", MdIcons.Code);

        var longContent = new StackPanel { Spacing = 8 };
        for (var i = 1; i <= 12; i++)
            longContent.Children.Add(new MdLabel { Text = $"Scrollable row {i}", Role = MdTypeRole.BodyMedium });

        var scroll = new MdScrollViewer
        {
            Height = 180,
            Content = longContent,
        };

        return GalleryUi.Showcase(
            "MdSplitView, MdCarousel and MdScrollViewer",
            "Resizable master/detail, swipeable carousel and the theme-independent scroll host.",
            GalleryUi.Column(
                new Border { Height = 260, Child = split },
                carousel,
                new MdCard(MdCardVariant.Outlined, scroll)),
            "MdSplitView.Pane/DetailContent · MdCarousel.AddItem(...) · MdScrollViewer.Content");
    }

    private static Control People()
    {
        var avatar = new MdAvatar("AL", size: 48, status: MdAvatarStatus.Online);
        var group = new MdAvatarGroup();
        group.AddAvatar("AL");
        group.AddAvatar("GH");
        group.AddAvatar("AT");
        group.AddAvatar("KJ");
        group.AddAvatar("LT");
        group.AddAvatar("MH");

        var drop = new MdFileDropZone("Drop files here", "or browse from the device", allowMultiple: true);
        drop.AddFile("design-spec.pdf", 248_000);
        drop.AddFile("screenshot.png", 512_000);

        return GalleryUi.Showcase(
            "MdAvatar, MdAvatarGroup, MdFileDropZone and MdChatBubble",
            "People and workspace widgets: avatars with presence, overlapping groups, file drops and chat bubbles.",
            GalleryUi.Column(
                GalleryUi.Row(avatar, group),
                new Border { Width = 460, Child = drop },
                new MdChatBubble("The gallery builds on desktop, web and Android.", false, "Ada", "10:24", "AL"),
                new MdChatBubble("I can see every control in one place.", true, "You", "10:26", "YO", isRead: true)),
            "MdAvatar.Status · MdAvatarGroup.AddAvatar · MdFileDropZone.AddFile · MdChatBubble.IsOutgoing");
    }

    private static Control WindowSection()
    {
        if (OperatingSystem.IsBrowser() || OperatingSystem.IsAndroid())
        {
            return GalleryUi.Section(
                "MdWindow",
                "MdWindow is a desktop-only TopLevel: it provides a theme-free window template and a VisualLayerManager for overlays.");
        }

        var button = new MdButton("Open a new window", MdButtonVariant.Filled, MdIcons.Window);
        button.Clicked += (_, _) =>
        {
            var window = new MdWindow
            {
                Title = "MdWindow demo",
                Width = 520,
                Height = 360,
                Content = new MdCard(
                    MdCardVariant.Filled,
                    new MdLabel
                    {
                        Text = "This is a second MdWindow with its own overlay layer.",
                        Margin = new Thickness(24),
                    }),
            };
            window.Show();
        };

        return GalleryUi.Section(
            "Windows",
            "MdWindow is a desktop-only TopLevel with a built-in theme-free template and overlay layer.",
            GalleryUi.Showcase(
                "MdWindow",
                "Opens a secondary window; on web and Android it is not applicable.",
                button,
                "new MdWindow { Content = ... }.Show()"));
    }

    private static Control Insets()
    {
        var safe = new MdSafeArea
        {
            Edges = MdSafeAreaEdges.All,
            SimulatedInset = new Thickness(16, 24, 16, 24),
            BasePadding = new Thickness(8),
            Body = new MdLabel
            {
                Text = "Content inside simulated safe-area insets",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
            Background = MdTheme.BrushOf(MdTheme.ColorOf(MdColorRole.SurfaceContainerLow)),
            Height = 140,
        };

        var keyboard = new MdKeyboardAvoidingHost
        {
            SimulatedKeyboardHeight = 120,
            Body = new MdLabel
            {
                Text = "Content lifted above a simulated 120 dp keyboard",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
            Height = 220,
            Background = MdTheme.BrushOf(MdTheme.ColorOf(MdColorRole.SurfaceContainerLow)),
        };

        return GalleryUi.Showcase(
            "MdSafeArea and MdKeyboardAvoidingHost",
            "Insets and IME handling used by MdNavigationPage on mobile; SimulatedInset/SimulatedKeyboardHeight preview them on desktop.",
            GalleryUi.Row(
                new Border { Width = 360, Child = safe },
                new Border { Width = 360, Child = keyboard }),
            "MdSafeArea.Edges/SimulatedInset · MdKeyboardAvoidingHost.SimulatedKeyboardHeight");
    }
}
