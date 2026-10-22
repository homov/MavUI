using Avalonia;
using Avalonia.Media;
using Mav.Primitives;
using Avalonia.Controls;
using Avalonia.Layout;
using Mav;
using Mav.Controls;
using Mav.Theming;

namespace Mav.Gallery;

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

        var labels = GalleryUi.Column(
            new MavLabel { Text = "TitleLarge", Role = MavTypeRole.TitleLarge },
            new MavLabel { Text = "BodyMedium secondary", Role = MavTypeRole.BodyMedium, Secondary = true },
            new MavLabel { Text = "Label with primary color", Role = MavTypeRole.LabelLarge, ColorRole = MavColorRole.Primary });

        var divider = new MavDivider();
        var vertical = new MavDivider { IsVertical = true, Height = 32 };

        var badge = new MavBadge("3");
        var badged = new MavBadgedBox(new MavIconButton(MavIcons.Notifications), "5");

        return GalleryUi.Showcase(
            "MavCard, MavSurface, MavLabel, MavDivider, MavBadge and MavBadgedBox",
            "MD3 surfaces with two-layer elevation, typography-aware labels, dividers and notification badges.",
            GalleryUi.Column(
                GalleryUi.Row(elevated, filled, outlined, surface),
                labels,
                divider,
                GalleryUi.Row(vertical, badge, badged)),
            "new MavCard(variant, body) · MavSurface.Elevation · MavLabel.Role · MavBadge.Count");
    }

    private static Control Expandables()
    {
        var panel = new MavExpansionPanel
        {
            Header = "Release notes",
            IsExpanded = true,
            Body = new MavLabel
            {
                Text = "Expansion panels keep secondary content out of the way until it is needed.",
                TextWrapping = TextWrapping.Wrap,
            },
        };

        var list = new MavExpansionPanelList { SingleExpand = true };
        list.AddPanel(new MavExpansionPanel { Header = "Install", Body = new MavLabel { Text = "dotnet add package Mav" } });
        list.AddPanel(new MavExpansionPanel { Header = "Theme", Body = new MavLabel { Text = "MavTheme.Configure(seed: ...)" } });

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

        return GalleryUi.Showcase(
            "MavExpansionPanel, MavExpansionPanelList, MavSettingsCard and MavSettingsExpander",
            "Progressive disclosure for details, settings groups and compact forms.",
            GalleryUi.Column(panel, list, settings, expander),
            "MavExpansionPanel.IsExpanded · MavExpansionPanelList.SingleExpand · MavSettingsCard.ActionContent");
    }

    private static Control Responsive()
    {
        var split = new MavSplitView
        {
            PaneWidth = 180,
            MinPaneWidth = 140,
            MaxPaneWidth = 320,
            IsResizable = true,
            Pane = new MavCard(MavCardVariant.Filled, new MavLabel { Text = "Pane" }),
            DetailContent = new MavCard(MavCardVariant.Outlined, new MavLabel { Text = "Detail content" }),
        };

        var carousel = new MavCarousel { Height = 220 };
        carousel.AddItem("Material 3", "Tonal palettes and shape", MavIcons.Palette);
        carousel.AddItem("Adaptive", "Phone, tablet and desktop", MavIcons.Devices);
        carousel.AddItem("Pure C#", "No AXAML templates", MavIcons.Code);

        var longContent = new StackPanel { Spacing = 8 };
        for (var i = 1; i <= 12; i++)
            longContent.Children.Add(new MavLabel { Text = $"Scrollable row {i}", Role = MavTypeRole.BodyMedium });

        var scroll = new MavScrollViewer
        {
            Height = 180,
            Content = longContent,
        };

        return GalleryUi.Showcase(
            "MavSplitView, MavCarousel and MavScrollViewer",
            "Resizable master/detail, swipeable carousel and the theme-independent scroll host.",
            GalleryUi.Column(
                new Border { Height = 260, Child = split },
                carousel,
                new MavCard(MavCardVariant.Outlined, scroll)),
            "MavSplitView.Pane/DetailContent · MavCarousel.AddItem(...) · MavScrollViewer.Content");
    }

    private static Control People()
    {
        var avatar = new MavAvatar("AL", size: 48, status: MavAvatarStatus.Online);
        var group = new MavAvatarGroup();
        group.AddAvatar("AL");
        group.AddAvatar("GH");
        group.AddAvatar("AT");
        group.AddAvatar("KJ");
        group.AddAvatar("LT");
        group.AddAvatar("MH");

        var drop = new MavFileDropZone("Drop files here", "or browse from the device", allowMultiple: true);
        drop.AddFile("design-spec.pdf", 248_000);
        drop.AddFile("screenshot.png", 512_000);

        return GalleryUi.Showcase(
            "MavAvatar, MavAvatarGroup, MavFileDropZone and MavChatBubble",
            "People and workspace widgets: avatars with presence, overlapping groups, file drops and chat bubbles.",
            GalleryUi.Column(
                GalleryUi.Row(avatar, group),
                new Border { Width = 460, Child = drop },
                new MavChatBubble("The gallery builds on desktop, web and Android.", false, "Ada", "10:24", "AL"),
                new MavChatBubble("I can see every control in one place.", true, "You", "10:26", "YO", isRead: true)),
            "MavAvatar.Status · MavAvatarGroup.AddAvatar · MavFileDropZone.AddFile · MavChatBubble.IsOutgoing");
    }

    private static Control WindowSection()
    {
        if (OperatingSystem.IsBrowser() || OperatingSystem.IsAndroid())
        {
            return GalleryUi.Section(
                "MavWindow",
                "MavWindow is a desktop-only TopLevel: it provides a theme-free window template and a VisualLayerManager for overlays.");
        }

        var button = new MavButton("Open a new window", MavButtonVariant.Filled, MavIcons.Window);
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
                        Text = "This is a second MavWindow with its own overlay layer.",
                        Margin = new Thickness(24),
                    }),
            };
            window.Show();
        };

        return GalleryUi.Section(
            "Windows",
            "MavWindow is a desktop-only TopLevel with a built-in theme-free template and overlay layer.",
            GalleryUi.Showcase(
                "MavWindow",
                "Opens a secondary window; on web and Android it is not applicable.",
                button,
                "new MavWindow { Content = ... }.Show()"));
    }

    private static Control Insets()
    {
        var safe = new MavSafeArea
        {
            Edges = MavSafeAreaEdges.All,
            SimulatedInset = new Thickness(16, 24, 16, 24),
            BasePadding = new Thickness(8),
            Body = new MavLabel
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
            Body = new MavLabel
            {
                Text = "Content lifted above a simulated 120 dp keyboard",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
            Height = 220,
            Background = MavTheme.BrushOf(MavTheme.ColorOf(MavColorRole.SurfaceContainerLow)),
        };

        return GalleryUi.Showcase(
            "MavSafeArea and MavKeyboardAvoidingHost",
            "Insets and IME handling used by MavNavigationPage on mobile; SimulatedInset/SimulatedKeyboardHeight preview them on desktop.",
            GalleryUi.Row(
                new Border { Width = 360, Child = safe },
                new Border { Width = 360, Child = keyboard }),
            "MavSafeArea.Edges/SimulatedInset · MavKeyboardAvoidingHost.SimulatedKeyboardHeight");
    }
}
