using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using Mav;
using Mav.Controls;
using Mav.ControlsGalleryMarkup.Pages;
using Mav.Theming;

namespace Mav.ControlsGalleryMarkup;

/// <summary>
/// Window with a navigation column and a page area, all built in C# markup. Texts come from
/// MavLocalization, so the window is rebuilt whenever the language changes.
/// </summary>
public sealed class MainWindow : MavWindow
{
    private static readonly (string Key, string English, string Icon, Func<Control> Build)[] Destinations =
    [
        ("Overview", "Overview", MavIcons.Home, () => OverviewPage.Build()),
        ("Actions", "Actions", MavIcons.SmartButton, () => ActionsPage.Build()),
        ("Inputs", "Inputs", MavIcons.Edit, () => InputsPage.Build()),
        ("Selection", "Selection", MavIcons.CheckBox, () => SelectionPage.Build()),
    ];

    private int _selected;

    public MainWindow()
    {
        Width = 1100;
        Height = 720;
        Rebuild();
        MavLocalization.Changed += (_, _) => Rebuild();
    }

    private void Rebuild()
    {
        Title = MavLocalization.Get("Shell.WindowTitle", "ControlsGallery (markup)");

        var navigation = new StackPanel().Spacing(8);
        navigation.Children.Add(new MavLabel(MavLocalization.Get("Shell.AppTitle", "Mav Controls"), MavTypeRole.TitleMedium));
        for (var i = 0; i < Destinations.Length; i++)
        {
            var index = i;
            var destination = Destinations[i];
            var label = MavLocalization.Get($"Nav.{destination.Key}.Label", destination.English);
            var variant = index == _selected ? MavButtonVariant.Tonal : MavButtonVariant.Text;
            navigation.Children.Add(new MavButton(label, variant, destination.Icon)
                .OnClicked(() =>
                {
                    _selected = index;
                    Rebuild();
                }));
        }

        var toolbar = new StackPanel().Orientation(Orientation.Horizontal).Spacing(4);
        toolbar.Children.Add(new MavIconButton(MavIcons.DarkMode).OnClicked(() => MavTheme.ToggleDark()));
        toolbar.Children.Add(new MavIconButton(MavIcons.Globe).OnClicked(() => ToggleLanguage()));

        var header = new DockPanel();
        DockPanel.SetDock(toolbar, Dock.Right);
        header.Children.Add(toolbar);
        header.Children.Add(new MavLabel(MavLocalization.Get("Shell.Title", "Controls gallery"), MavTypeRole.HeadlineSmall));

        var body = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        body.Children.Add(header);
        body.Children.Add(new Border().Padding(24, 8, 24, 0).Child(Destinations[_selected].Build()));

        var grid = new Grid().Cols("220, *");
        grid.Children.Add(navigation);
        Grid.SetColumn(body, 1);
        grid.Children.Add(body);
        Content = grid;
    }

    private static void ToggleLanguage()
    {
        var isRussian = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";
        MavLocalization.SetCulture(new CultureInfo(isRussian ? "en-US" : "ru-RU"));
    }
}
