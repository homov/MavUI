using Avalonia.Controls;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Theming;

namespace Md3.Gallery;

internal static class OverlaysPage
{
    public static Control Build()
    {
        return GalleryUi.Page(
            GalleryUi.Section("Menus and popovers", "Anchored menus with submenus, and a popover with arbitrary content.", Menus()),
            GalleryUi.Section("Tooltips", "Simple and rich tooltips.", Tooltips()),
            GalleryUi.Section("Search", "A full search view with suggestions and recent searches.", Search()),
            GalleryUi.Section("Declarative flyouts", "MdFlyout opens a menu, popover, sheet or dialog when the control is clicked.", Flyouts()));
    }

    private static Control Menus()
    {
        var menu = new MdMenu()
            .AddItem("Rename", MdIcons.Edit, onSelected: () => { })
            .AddItem("Duplicate", MdIcons.ContentCopy, onSelected: () => { })
            .AddDivider()
            .AddSubmenu("Move to", sub => sub
                .AddItem("Archive", MdIcons.Archive, onSelected: () => { })
                .AddItem("Trash", MdIcons.Delete, onSelected: () => { }), MdIcons.DriveFileMove)
            .AddItem("Delete", MdIcons.Delete, onSelected: () => { });

        var menuButton = new MdButton("Open menu", MdButtonVariant.Outlined, MdIcons.MoreVert);
        menuButton.Clicked += (_, _) => menu.Show(menuButton);

        var popover = new MdPopover
        {
            Title = "Details",
            Placement = MdPopoverPlacement.Bottom,
            Content = new MdLabel
            {
                Text = "Any control can live in a popover. It clamps to the viewport and stays near its anchor.",
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
            },
        };

        var popoverButton = new MdButton("Open popover", MdButtonVariant.Outlined, MdIcons.OpenInNew);
        popoverButton.Clicked += (_, _) => popover.Show(popoverButton);

        return GalleryUi.Showcase(
            "MdMenu, MdMenuItem and MdPopover",
            "Menus support icons, trailing text, dividers and nested submenus; popovers host custom content.",
            GalleryUi.Row(menuButton, popoverButton),
            "menu.Show(anchor) · AddItem/AddSubmenu/AddDivider · new MdPopover { Content }.Show(anchor)");
    }

    private static Control Tooltips()
    {
        var simple = new MdButton("Hover for tooltip", MdButtonVariant.Tonal, MdIcons.Info);
        MdTooltip.SetText(simple, "A simple MD3 tooltip");

        var rich = new MdButton("Rich tooltip", MdButtonVariant.Outlined, MdIcons.Keyboard);
        rich.Clicked += (_, _) => MdRichTooltip.Show(
            rich,
            "Keyboard shortcut",
            "Ctrl+K opens the command palette.",
            actionText: "Got it");

        return GalleryUi.Showcase(
            "MdTooltip and MdRichTooltip",
            "A global simple tooltip and a rich tooltip with a title, body and optional action.",
            GalleryUi.Row(simple, rich),
            "MdTooltip.SetText(control, text) · MdRichTooltip.Show(anchor, title, text, actionText)");
    }

    private static Control Search()
    {
        var button = new MdButton("Open search view", MdButtonVariant.Filled, MdIcons.Search);
        var view = new MdSearchView
        {
            Placeholder = "Search every control",
        };
        button.Clicked += (_, _) => view.Show(button);

        return GalleryUi.Showcase(
            "MdSearchView",
            "Full-screen or compact search with suggestions, recent searches and result templates.",
            button,
            "new MdSearchView { Suggestions }.Show(anchor) · QueryChanged · ResultSelected");
    }

    private static Control Flyouts()
    {
        var menu = new MdMenu()
            .AddItem("First", MdIcons.LooksOne, onSelected: () => { })
            .AddItem("Second", MdIcons.LooksTwo, onSelected: () => { });

        var menuButton = new MdButton("Flyout menu", MdButtonVariant.Outlined, MdIcons.Menu);
        MdFlyout.SetMenu(menuButton, menu);

        var dialogButton = new MdButton("Flyout dialog", MdButtonVariant.Outlined, MdIcons.Chat);
        MdFlyout.SetDialog(dialogButton, new MdDialog
        {
            Title = "Declarative dialog",
            Message = "Opened by MdFlyout when the button is clicked.",
            ConfirmText = "Close",
        });

        var drawerButton = new MdButton("Flyout drawer", MdButtonVariant.Outlined, MdIcons.MenuOpen);
        var drawer = new MdModalNavigationDrawer("Flyout drawer");
        drawer.AddItem("One", MdIcons.LooksOne);
        drawer.AddItem("Two", MdIcons.LooksTwo);
        MdFlyout.SetModalDrawer(drawerButton, drawer);

        return GalleryUi.Showcase(
            "MdFlyout",
            "Attach a menu, popover, rich tooltip, sheet, dialog or modal drawer to any control declaratively.",
            GalleryUi.Row(menuButton, dialogButton, drawerButton),
            "MdFlyout.SetMenu/SetPopover/SetDialog/SetBottomSheet/SetSideSheet/SetModalDrawer");
    }
}
