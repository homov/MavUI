using Avalonia.Controls;
using Mav;

namespace Mav.Gallery;

internal sealed record GalleryDestination(
    string Label,
    string Subtitle,
    string Icon,
    Func<Control> Build);

internal static class GalleryDestinations
{
    public static readonly GalleryDestination[] All =
    [
        new("Overview", "Theme, typography, color and motion", MavIcons.Home, OverviewPage.Build),
        new("Actions", "Buttons, FABs and action groups", MavIcons.SmartButton, ActionsPage.Build),
        new("Inputs", "Text fields, validation and forms", MavIcons.Edit, InputsPage.Build),
        new("Selection", "Toggles, sliders, chips and value pickers", MavIcons.CheckBox, SelectionPage.Build),
        new("Pickers", "Date, time and color selection", MavIcons.CalendarMonth, PickersPage.Build),
        new("Navigation", "App bars, tabs, drawers, steps and pagination", MavIcons.Navigation, NavigationGroupPage.Build),
        new("Layout", "Cards, containers, avatars and responsive layout", MavIcons.Dashboard, LayoutPage.Build),
        new("Collections", "Lists, tables, trees and timelines", MavIcons.List, CollectionsPage.Build),
        new("Feedback", "Dialogs, sheets, banners, progress and status", MavIcons.Feedback, FeedbackPage.Build),
        new("Overlays", "Menus, popovers, tooltips and search", MavIcons.Layers, OverlaysPage.Build),
        new("Theming", "Color roles, type, shape and elevation", MavIcons.Palette, ThemingPage.Build),
    ];
}
