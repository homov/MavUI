using Avalonia.Controls;
using Mav;
using Mav.Controls;
using Mav.Theming;

namespace Mav.Gallery;

internal static class OverlaysPage
{
    public static Control Build()
    {
        return GalleryUi.Page(
            GalleryUi.Section("Menus and popovers", "Anchored menus with submenus, and a popover with arbitrary content.", Menus()),
            GalleryUi.Section("Tooltips", "Simple and rich tooltips.", Tooltips()),
            GalleryUi.Section("Search", "A full search view with suggestions and recent searches.", Search()),
            GalleryUi.Section("Declarative flyouts", "MavFlyout opens a menu, popover, sheet or dialog when the control is clicked.", Flyouts()));
    }

    private static Control Menus()
    {
        var menu = new MavMenu()
            .AddItem("Rename", MavIcons.Edit, onSelected: () => { })
            .AddItem("Duplicate", MavIcons.ContentCopy, onSelected: () => { })
            .AddDivider()
            .AddSubmenu("Move to", sub => sub
                .AddItem("Archive", MavIcons.Archive, onSelected: () => { })
                .AddItem("Trash", MavIcons.Delete, onSelected: () => { }), MavIcons.DriveFileMove)
            .AddItem("Delete", MavIcons.Delete, onSelected: () => { });

        var menuButton = new MavButton("Open menu", MavButtonVariant.Outlined, MavIcons.MoreVert);
        menuButton.Clicked += (_, _) => menu.Show(menuButton);

        var popover = new MavPopover
        {
            Title = "Details",
            Placement = MavPopoverPlacement.Bottom,
            Content = new MavLabel
            {
                Text = "Any control can live in a popover. It clamps to the viewport and stays near its anchor.",
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
            },
        };

        var popoverButton = new MavButton("Open popover", MavButtonVariant.Outlined, MavIcons.OpenInNew);
        popoverButton.Clicked += (_, _) => popover.Show(popoverButton);

        return GalleryUi.Showcase(
            "MavMenu, MavMenuItem and MavPopover",
            "Menus support icons, trailing text, dividers and nested submenus; popovers host custom content.",
            GalleryUi.Row(menuButton, popoverButton),
            "menu.Show(anchor) · AddItem/AddSubmenu/AddDivider · new MavPopover { Content }.Show(anchor)");
    }

    private static Control Tooltips()
    {
        var simple = new MavButton("Hover for tooltip", MavButtonVariant.Tonal, MavIcons.Info);
        MavTooltip.SetText(simple, "A simple MD3 tooltip");

        var rich = new MavButton("Rich tooltip", MavButtonVariant.Outlined, MavIcons.Keyboard);
        rich.Clicked += (_, _) => MavRichTooltip.Show(
            rich,
            "Keyboard shortcut",
            "Ctrl+K opens the command palette.",
            actionText: "Got it");

        return GalleryUi.Showcase(
            "MavTooltip and MavRichTooltip",
            "A global simple tooltip and a rich tooltip with a title, body and optional action.",
            GalleryUi.Row(simple, rich),
            "MavTooltip.SetText(control, text) · MavRichTooltip.Show(anchor, title, text, actionText)");
    }

    private static Control Search()
    {
        var button = new MavButton("Open search view", MavButtonVariant.Filled, MavIcons.Search);
        var view = new MavSearchView
        {
            Placeholder = "Search every control",
        };
        button.Clicked += (_, _) => view.Show(button);

        return GalleryUi.Showcase(
            "MavSearchView",
            "Full-screen or compact search with suggestions, recent searches and result templates.",
            button,
            "new MavSearchView { Suggestions }.Show(anchor) · QueryChanged · ResultSelected");
    }

    private static Control Flyouts()
    {
        var menu = new MavMenu()
            .AddItem("First", MavIcons.LooksOne, onSelected: () => { })
            .AddItem("Second", MavIcons.LooksTwo, onSelected: () => { });

        var menuButton = new MavButton("Flyout menu", MavButtonVariant.Outlined, MavIcons.Menu);
        MavFlyout.SetMenu(menuButton, menu);

        var dialogButton = new MavButton("Flyout dialog", MavButtonVariant.Outlined, MavIcons.Chat);
        MavFlyout.SetDialog(dialogButton, new MavDialog
        {
            Title = "Declarative dialog",
            Message = "Opened by MavFlyout when the button is clicked.",
            ConfirmText = "Close",
        });

        var drawerButton = new MavButton("Flyout drawer", MavButtonVariant.Outlined, MavIcons.MenuOpen);
        var drawer = new MavModalNavigationDrawer("Flyout drawer");
        drawer.AddItem("One", MavIcons.LooksOne);
        drawer.AddItem("Two", MavIcons.LooksTwo);
        MavFlyout.SetModalDrawer(drawerButton, drawer);

        return GalleryUi.Showcase(
            "MavFlyout",
            "Attach a menu, popover, rich tooltip, sheet, dialog or modal drawer to any control declaratively.",
            GalleryUi.Row(menuButton, dialogButton, drawerButton),
            "MavFlyout.SetMenu/SetPopover/SetDialog/SetBottomSheet/SetSideSheet/SetModalDrawer");
    }
}
