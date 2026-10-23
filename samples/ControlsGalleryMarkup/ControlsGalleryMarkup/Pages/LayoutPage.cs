using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Primitives;
using Mav.Theming;
using static Mav.ControlsGalleryMarkup.Pages.PageParts;

namespace Mav.ControlsGalleryMarkup.Pages;

/// <summary>Surfaces, expandables, responsive containers, people widgets, windows and insets. Mirrors the AXAML Layout page.</summary>
public static class LayoutPage
{
    private static Color SurfaceContainerHigh => MavTheme.ColorOf(MavColorRole.SurfaceContainerHigh);

    private static IBrush SurfaceContainerLowBrush =>
        MavTheme.BrushOf(MavTheme.ColorOf(MavColorRole.SurfaceContainerLow));

    private static bool IsDarkMode => MavTheme.Current.IsDark;

    private static bool IsDesktop => !(OperatingSystem.IsBrowser() || OperatingSystem.IsAndroid());

    public static Control Build() =>
        Page(
            Section(
                "Layout.Surfaces.Title", "Surfaces",
                "Layout.Surfaces.Body", "Cards, surfaces, labels, dividers and badges.",
                Card(
                    "MavCard, MavSurface, MavLabel, MavDivider, MavBadge and MavBadgedBox",
                    "Layout.Surfaces.CardBody", "MD3 surfaces with two-layer elevation, typography-aware labels, dividers and notification badges.",
                    "new MavCard(variant, body) · MavSurface.Elevation · MavLabel.Role · MavBadge.Count",
                    Row(
                        new MavCard(MavCardVariant.Elevated, new MavLabel("Elevated card")),
                        new MavCard(MavCardVariant.Filled, new MavLabel("Filled card")),
                        new MavCard(MavCardVariant.Outlined, new MavLabel("Outlined card")),
                        new MavSurface()
                            .Width(200)
                            .Height(88)
                            .Radius(16)
                            .Elevation(2)
                            .FillColor(SurfaceContainerHigh)
                            .Child(new MavLabel("MavSurface")
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .VerticalAlignment(VerticalAlignment.Center))),
                    Stack(8,
                        new MavLabel("TitleLarge", MavTypeRole.TitleLarge),
                        new MavLabel("BodyMedium secondary", MavTypeRole.BodyMedium).Secondary(true),
                        new MavLabel("Label with primary color", MavTypeRole.LabelLarge, MavColorRole.Primary)),
                    new MavDivider(),
                    Row(
                        new MavDivider(0, 0, true).Height(32),
                        new MavBadge("3"),
                        new MavBadgedBox(new MavIconButton(MavIcons.Notifications), "5")))),
            Section(
                "Layout.Expandables.Title", "Expandable and settings",
                "Layout.Expandables.Body", "Expansion panels, settings cards and settings expanders.",
                Card(
                    "MavExpansionPanel, MavExpansionPanelList, MavSettingsCard and MavSettingsExpander",
                    "Layout.Expandables.CardBody", "Progressive disclosure for details, settings groups and compact forms.",
                    "MavExpansionPanel.IsExpanded · MavExpansionPanelList.SingleExpand · MavSettingsCard.ActionContent",
                    Stack(8,
                        new MavExpansionPanel("Release notes", null,
                            new MavLabel("Expansion panels keep secondary content out of the way until it is needed.")
                                .TextWrapping(TextWrapping.Wrap),
                            true),
                        new MavExpansionPanelList(true)
                            .AddPanel(new MavExpansionPanel("Install", null, new MavLabel("dotnet add package Mav")))
                            .AddPanel(new MavExpansionPanel("Theme", null, new MavLabel("MavTheme.Configure(seed: ...)"))),
                        new MavSettingsCard("Appearance", "Follow the system theme or force a mode", MavIcons.DarkMode,
                                new MavSwitch(IsDarkMode, true))
                            .Title("Dark mode"),
                        new MavSettingsExpander("Notifications", null, null, null, true)
                            .AddItem("Email", "Daily digest", new MavSwitch(true), MavIcons.Email)
                            .AddItem("Push", "Mentions and replies", new MavSwitch(), MavIcons.Notifications)))),
            Section(
                "Layout.Responsive.Title", "Responsive containers",
                "Layout.Responsive.Body", "Split view, carousel and the theme-independent scroll viewer.",
                Card(
                    "MavSplitView, MavCarousel and MavScrollViewer",
                    "Layout.Responsive.CardBody", "Resizable master/detail, swipeable carousel and the theme-independent scroll host.",
                    "MavSplitView.Pane/DetailContent · MavCarousel.AddItem(...) · MavScrollViewer.Content",
                    Responsive())),
            Section(
                "Layout.People.Title", "People and messages",
                "Layout.People.Body", "Avatars, avatar groups, file drop and chat bubbles.",
                Card(
                    "MavAvatar, MavAvatarGroup, MavFileDropZone and MavChatBubble",
                    "Layout.People.CardBody", "People and workspace widgets: avatars with presence, overlapping groups, file drops and chat bubbles.",
                    "MavAvatar.Status · MavAvatarGroup.AddAvatar · MavFileDropZone.AddFile · MavChatBubble.IsOutgoing",
                    Stack(8,
                        Row(
                            new MavAvatar("AL", null, 48, MavAvatarStatus.Online),
                            new MavAvatarGroup()
                                .AddAvatar("AL")
                                .AddAvatar("GH")
                                .AddAvatar("AT")
                                .AddAvatar("KJ")
                                .AddAvatar("LT")
                                .AddAvatar("MH")),
                        Bounded(
                            new MavFileDropZone(L("Layout.Drop.Title", "Drop files here"),
                                    L("Layout.Drop.Subtitle", "or browse from the device"), true)
                                .AddFile("design-spec.pdf", 248000)
                                .AddFile("screenshot.png", 512000),
                            460),
                        new MavChatBubble("The gallery builds on desktop, web and Android.", false, "Ada", "10:24", "AL"),
                        new MavChatBubble("I can see every control in one place.", true, "You", "10:26", "YO", true)))),
            Section(
                "Layout.Windows.Title", "Windows",
                "Layout.Windows.Body", "MavWindow is a desktop-only TopLevel with a built-in theme-free template and overlay layer.",
                Card(
                    "MavWindow",
                    "Layout.Window.CardBody", "Opens a secondary window; on web and Android it is not applicable.",
                    "new MavWindow { Content = ... }.Show()",
                    Row(
                        new MavButton(L("Layout.Window.Open", "Open a new window"), MavButtonVariant.Filled, MavIcons.Window)
                            .IsVisible(IsDesktop)
                            .OnClicked(() => DemoWindow().Show()),
                        new MavLabel(L("Layout.Window.Unsupported",
                                "MavWindow is a desktop-only TopLevel and is not available on this platform."))
                            .TextWrapping(TextWrapping.Wrap)
                            .IsVisible(!IsDesktop)))),
            Section(
                "Layout.Insets.Title", "Insets and keyboard",
                "Layout.Insets.Body", "Safe-area and keyboard-aware hosts with simulated insets.",
                Card(
                    "MavSafeArea and MavKeyboardAvoidingHost",
                    "Layout.Insets.CardBody", "Insets and IME handling used by MavNavigationPage on mobile; SimulatedInset/SimulatedKeyboardHeight preview them on desktop.",
                    "MavSafeArea.Edges/SimulatedInset · MavKeyboardAvoidingHost.SimulatedKeyboardHeight",
                    Adaptive(300,
                        new MavSafeArea(new MavLabel("Content inside simulated safe-area insets")
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .VerticalAlignment(VerticalAlignment.Center))
                            .BasePadding(new Thickness(8))
                            .SimulatedInset(new Thickness(16, 24, 16, 24))
                            .Height(140)
                            .Background(SurfaceContainerLowBrush),
                        new MavKeyboardAvoidingHost(new MavLabel("Content lifted above a simulated 120 dp keyboard")
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .VerticalAlignment(VerticalAlignment.Center))
                            .SimulatedKeyboardHeight(120)
                            .Height(220)
                            .Background(SurfaceContainerLowBrush)))));

    private static Control Responsive()
    {
        var paneToggle = new MavButton(L("Layout.Split.Toggle", "Toggle pane"), MavButtonVariant.Tonal, MavIcons.Menu)
            .IsToggle(true)
            .IsSelected(true);

        var split = new MavSplitView(
                new MavCard(MavCardVariant.Filled, new MavLabel("Pane")),
                new MavCard(MavCardVariant.Outlined, Stack(8, new MavLabel("Detail content"), paneToggle)))
            .PaneWidth(180)
            .MinPaneWidth(140)
            .MaxPaneWidth(320)
            .IsResizable(true)
            .IsPaneOpen(true);
        paneToggle.OnToggled((_, open) => split.IsPaneOpen = open);

        var rows = Enumerable.Range(1, 12)
            .Select(i => (Control)new MavLabel($"Scrollable row {i}", MavTypeRole.BodyMedium))
            .ToArray();

        return Stack(8,
            new Border().Height(260).Child(split),
            new MavCarousel(220)
                .AddItem("Material 3", "Tonal palettes and shape", MavIcons.Palette)
                .AddItem("Adaptive", "Phone, tablet and desktop", MavIcons.Devices)
                .AddItem("Pure C#", "No AXAML templates", MavIcons.Code),
            new MavCard(MavCardVariant.Outlined,
                new MavScrollViewer().Height(180).Content(Stack(8, rows))));
    }

    /// <summary>A new window is created per click, so it can be shown again after it was closed.</summary>
    private static MavWindow DemoWindow() =>
        new MavWindow()
            .Title("MavWindow demo")
            .Width(520)
            .Height(360)
            .Content(new MavCard(MavCardVariant.Filled,
                new MavLabel(L("Layout.Window.Body", "This is a second MavWindow with its own overlay layer."))
                    .Margin(new Thickness(24))
                    .TextWrapping(TextWrapping.Wrap)));

    private static StackPanel Stack(double spacing, params Control[] items) =>
        new StackPanel().Spacing(spacing).Children(items);

    private static Control Bounded(Control control, double maxWidth) =>
        new Border().MaxWidth(maxWidth).Child(control);
}
