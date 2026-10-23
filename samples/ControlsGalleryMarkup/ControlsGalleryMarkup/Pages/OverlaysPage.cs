using Avalonia;
using Avalonia.Controls;
using Mav;
using Mav.Controls;
using Mav.Theming;
using static Mav.ControlsGalleryMarkup.Pages.PageParts;

namespace Mav.ControlsGalleryMarkup.Pages;

/// <summary>Menus, popovers, tooltips, search and declarative flyouts. Mirrors the AXAML Overlays page.</summary>
public static class OverlaysPage
{
    public static Control Build() =>
        Page(
            Section(
                "Overlays.Menus.Title", "Menus and popovers",
                "Overlays.Menus.Body", "Anchored menus with submenus, and a popover with arbitrary content.",
                Card(
                    "MavMenu, MavMenuItem and MavPopover",
                    "Overlays.Menus.CardBody", "Menus support icons, trailing text, dividers and nested submenus; popovers host custom content.",
                    "menu.Show(anchor) · AddItem/AddSubmenu/AddDivider · new MavPopover { Content }.Show(anchor)",
                    Row(
                        WithMenu(
                            new MavButton(L("Overlays.Menu.Open", "Open menu"), MavButtonVariant.Outlined, MavIcons.MoreVert),
                            new MavMenu()
                                .AddItem(L("Overlays.Menu.Rename", "Rename"), MavIcons.Edit)
                                .AddItem(L("Overlays.Menu.Duplicate", "Duplicate"), MavIcons.ContentCopy)
                                .AddDivider()
                                .AddSubmenu(L("Overlays.Menu.MoveTo", "Move to"), sub => sub
                                    .AddItem(L("Overlays.Menu.Archive", "Archive"), MavIcons.Archive)
                                    .AddItem(L("Overlays.Menu.Trash", "Trash"), MavIcons.Delete),
                                    MavIcons.DriveFileMove)
                                .AddItem(L("Overlays.Menu.Delete", "Delete"), MavIcons.Delete)),
                        WithPopover(
                            new MavButton(L("Overlays.Popover.Open", "Open popover"), MavButtonVariant.Outlined, MavIcons.OpenInNew),
                            new MavPopover(
                                new MavLabel(L("Overlays.Popover.Body",
                                        "Any control can live in a popover. It clamps to the viewport and stays near its anchor."))
                                    .TextWrapping(TextWrapping.Wrap),
                                L("Overlays.Popover.Title", "Details"),
                                MavPopoverPlacement.Bottom))))),
            Section(
                "Overlays.Tooltips.Title", "Tooltips",
                "Overlays.Tooltips.Body", "Simple and rich tooltips.",
                Card(
                    "MavTooltip and MavRichTooltip",
                    "Overlays.Tooltips.CardBody", "A global simple tooltip and a rich tooltip with a title, body and optional action.",
                    "MavTooltip.SetText(control, text) · MavRichTooltip.Show(anchor, title, text, actionText)",
                    Row(
                        WithTooltip(
                            new MavButton(L("Overlays.Tip.Hover", "Hover for tooltip"), MavButtonVariant.Tonal, MavIcons.Info),
                            L("Overlays.Tip.Text", "A simple MD3 tooltip")),
                        WithRichTooltip(
                            new MavButton(L("Overlays.Tip.Rich", "Rich tooltip"), MavButtonVariant.Outlined, MavIcons.Keyboard),
                            new MavRichTooltip()
                                .Subhead(L("Overlays.Tip.ShortcutTitle", "Keyboard shortcut"))
                                .Text(L("Overlays.Tip.ShortcutText", "Ctrl+K opens the command palette."))
                                .ActionText(L("Overlays.Tip.GotIt", "Got it")))))),
            Section(
                "Overlays.Search.Title", "Search",
                "Overlays.Search.Body", "A full search view with suggestions and recent searches.",
                Card(
                    "MavSearchView",
                    "Overlays.Search.CardBody", "Full-screen or compact search with suggestions, recent searches and result templates.",
                    "new MavSearchView { Suggestions }.Show(anchor) · QueryChanged · ResultSelected",
                    Row(
                        WithSearchView(
                            new MavButton(L("Overlays.Search.Open", "Open search view"), MavButtonVariant.Filled, MavIcons.Search),
                            new MavSearchView()
                                .Placeholder(L("Overlays.Search.Placeholder", "Search every control")))))),
            Section(
                "Overlays.Flyouts.Title", "Declarative flyouts",
                "Overlays.Flyouts.Body", "MavFlyout opens a menu, popover, sheet or dialog when the control is clicked.",
                Card(
                    "MavFlyout",
                    "Overlays.Flyouts.CardBody", "Attach a menu, popover, rich tooltip, sheet, dialog or modal drawer to any control declaratively.",
                    "MavFlyout.SetMenu/SetPopover/SetDialog/SetBottomSheet/SetSideSheet/SetModalDrawer",
                    Row(
                        WithMenu(
                            new MavButton(L("Overlays.Flyout.Menu", "Flyout menu"), MavButtonVariant.Outlined, MavIcons.Menu),
                            new MavMenu()
                                .AddItem("First", MavIcons.LooksOne)
                                .AddItem("Second", MavIcons.LooksTwo)),
                        WithDialog(
                            new MavButton(L("Overlays.Flyout.Dialog", "Flyout dialog"), MavButtonVariant.Outlined, MavIcons.Chat),
                            new MavDialog()
                                .Title(L("Overlays.Flyout.DialogTitle", "Declarative dialog"))
                                .Message(L("Overlays.Flyout.DialogMessage", "Opened by MavFlyout when the button is clicked."))
                                .ConfirmText(L("Overlays.Flyout.Close", "Close"))),
                        WithModalDrawer(
                            new MavButton(L("Overlays.Flyout.Drawer", "Flyout drawer"), MavButtonVariant.Outlined, MavIcons.MenuOpen),
                            new MavModalNavigationDrawer(L("Overlays.Flyout.Drawer", "Flyout drawer"), 300)
                                .AddItem("One", MavIcons.LooksOne)
                                .AddItem("Two", MavIcons.LooksTwo))))));

    // The helpers below attach a declared element to a button; the button opens it on click.

    private static MavButton WithMenu(MavButton button, MavMenu menu)
    {
        MavFlyout.SetMenu(button, menu);
        return button;
    }

    private static MavButton WithPopover(MavButton button, MavPopover popover)
    {
        MavFlyout.SetPopover(button, popover);
        return button;
    }

    private static MavButton WithRichTooltip(MavButton button, MavRichTooltip tooltip)
    {
        MavFlyout.SetRichTooltip(button, tooltip);
        return button;
    }

    private static MavButton WithSearchView(MavButton button, MavSearchView search)
    {
        MavFlyout.SetSearchView(button, search);
        return button;
    }

    private static MavButton WithDialog(MavButton button, MavDialog dialog)
    {
        MavFlyout.SetDialog(button, dialog);
        return button;
    }

    private static MavButton WithModalDrawer(MavButton button, MavModalNavigationDrawer drawer)
    {
        MavFlyout.SetModalDrawer(button, drawer);
        return button;
    }

    private static MavButton WithTooltip(MavButton button, string text)
    {
        MavTooltip.SetText(button, text);
        return button;
    }
}
