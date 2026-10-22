using Avalonia.Controls;
using Mav;
using Mav.ControlsGallery.Pages;

namespace Mav.ControlsGallery.Shell;

/// <summary>One navigation entry. Label and subtitle are localized; English is the fallback.</summary>
public sealed record GalleryDestination(
    string Key,
    string EnglishLabel,
    string EnglishSubtitle,
    string Icon,
    Func<Control> Build)
{
    public string Label => MavLocalization.Get($"Nav.{Key}.Label", EnglishLabel);

    public string Subtitle => MavLocalization.Get($"Nav.{Key}.Subtitle", EnglishSubtitle);
}

public static class GalleryDestinations
{
    public static readonly GalleryDestination[] All =
    [
        new("Overview", "Overview", "Theme, typography, color and motion", MavIcons.Home, () => new OverviewPage()),
        new("Actions", "Actions", "Buttons, FABs and action groups", MavIcons.SmartButton, () => new ActionsPage()),
        new("Inputs", "Inputs", "Text fields, validation and forms", MavIcons.Edit, () => new InputsPage()),
        new("Selection", "Selection", "Toggles, sliders, chips and value pickers", MavIcons.CheckBox, () => new SelectionPage()),
        new("Pickers", "Pickers", "Date, time and color selection", MavIcons.CalendarMonth, () => new PickersPage()),
        new("Navigation", "Navigation", "App bars, tabs, drawers, steps and pagination", MavIcons.Navigation, () => new NavigationGroupPage()),
        new("Layout", "Layout", "Cards, containers, avatars and responsive layout", MavIcons.Dashboard, () => new LayoutPage()),
        new("Collections", "Collections", "Lists, tables, trees and timelines", MavIcons.List, () => new CollectionsPage()),
        new("Feedback", "Feedback", "Dialogs, sheets, banners, progress and status", MavIcons.Feedback, () => new FeedbackPage()),
        new("Overlays", "Overlays", "Menus, popovers, tooltips and search", MavIcons.Layers, () => new OverlaysPage()),
        new("Theming", "Theming", "Color roles, type, shape and elevation", MavIcons.Palette, () => new ThemingPage()),
    ];
}
