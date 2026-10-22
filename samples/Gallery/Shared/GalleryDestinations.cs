using Avalonia.Controls;
using Md3.Avalonia;

namespace Md3.Gallery;

internal sealed record GalleryDestination(
    string Label,
    string Subtitle,
    string Icon,
    Func<Control> Build);

internal static class GalleryDestinations
{
    public static readonly GalleryDestination[] All =
    [
        new("Overview", "Theme, typography, color and motion", MdIcons.Home, OverviewPage.Build),
        new("Actions", "Buttons, FABs and action groups", MdIcons.SmartButton, ActionsPage.Build),
        new("Inputs", "Text fields, validation and forms", MdIcons.Edit, InputsPage.Build),
        new("Selection", "Toggles, sliders, chips and value pickers", MdIcons.CheckBox, SelectionPage.Build),
        new("Pickers", "Date, time and color selection", MdIcons.CalendarMonth, PickersPage.Build),
        new("Navigation", "App bars, tabs, drawers, steps and pagination", MdIcons.Navigation, NavigationGroupPage.Build),
        new("Layout", "Cards, containers, avatars and responsive layout", MdIcons.Dashboard, LayoutPage.Build),
        new("Collections", "Lists, tables, trees and timelines", MdIcons.List, CollectionsPage.Build),
        new("Feedback", "Dialogs, sheets, banners, progress and status", MdIcons.Feedback, FeedbackPage.Build),
        new("Overlays", "Menus, popovers, tooltips and search", MdIcons.Layers, OverlaysPage.Build),
        new("Theming", "Color roles, type, shape and elevation", MdIcons.Palette, ThemingPage.Build),
    ];
}
