using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mav;
using Mav.Controls;
using Mav.Platform;
using Xunit;

namespace Mav.Tests;

public class KeyboardTests
{
    [AvaloniaFact]
    public void Simulated_keyboard_inset_lifts_the_page_and_the_fab()
    {
        var page = new MavContentPage("Root")
        {
            FloatingActionButton = new MavFab(MavIcons.Add),
            Child = new Border { Height = 2000 },
        };
        var navigation = new MavNavigationPage(page);
        var window = new Window
        {
            Width = 400,
            Height = 800,
            Content = navigation,
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        navigation.KeyboardHost.AnimateInset = false;
        var pageHeightBefore = page.Bounds.Height;
        var fab = navigation.GetVisualDescendants().OfType<MavFab>().Single();
        var fabTopBefore = fab.TranslatePoint(default, navigation)!.Value.Y;

        navigation.KeyboardHost.SimulatedKeyboardHeight = 300;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(300, navigation.KeyboardHost.Padding.Bottom);
        Assert.True(pageHeightBefore > 300);
        Assert.Equal(pageHeightBefore - 300, page.Bounds.Height, 1);

        var fabTopAfter = fab.TranslatePoint(default, navigation)!.Value.Y;
        Assert.True(fabTopAfter < fabTopBefore);
        Assert.True(fabTopAfter + fab.Bounds.Height <= navigation.KeyboardHost.TranslatePoint(default, navigation)!.Value.Y + page.Bounds.Height + 1);

        window.Close();
    }

    [AvaloniaFact]
    public void Clearing_the_simulated_keyboard_restores_the_full_page()
    {
        var page = new MavContentPage("Root")
        {
            Child = new Border { Height = 2000 },
        };
        var navigation = new MavNavigationPage(page);
        var window = new Window
        {
            Width = 400,
            Height = 800,
            Content = navigation,
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        navigation.KeyboardHost.AnimateInset = false;
        var pageHeightBefore = page.Bounds.Height;

        navigation.KeyboardHost.SimulatedKeyboardHeight = 280;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(pageHeightBefore - 280, page.Bounds.Height, 1);

        navigation.KeyboardHost.SimulatedKeyboardHeight = 0;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, navigation.KeyboardHost.Padding.Bottom);
        Assert.Equal(pageHeightBefore, page.Bounds.Height, 1);

        window.Close();
    }

    [AvaloniaFact]
    public void Leaving_a_keyboard_preview_page_restores_the_full_overlay_height()
    {
        var page = new MavContentPage("Root");
        var navigation = new MavNavigationPage(page);
        var window = new Window
        {
            Width = 400,
            Height = 800,
            Content = navigation,
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // The gallery previews the soft keyboard on one of its pages. That preview is
        // global to overlays, so it must stop contributing when the page is left.
        var preview = new MavKeyboardAvoidingHost
        {
            SimulatedKeyboardHeight = 120,
            Child = new Border(),
        };
        page.Child = preview;
        for (var i = 0; i < 3; i++)
            Dispatcher.UIThread.RunJobs();
        MavOverlay.GetOverlaySize(preview);
        for (var i = 0; i < 3; i++)
            Dispatcher.UIThread.RunJobs();
        Assert.True(MavOverlay.GetOverlaySize(preview).Height < 700);

        page.Child = new Border();
        for (var i = 0; i < 3; i++)
            Dispatcher.UIThread.RunJobs();

        Assert.Equal(800, MavOverlay.GetOverlaySize(navigation).Height, 1);

        window.Close();
    }

    [AvaloniaFact]
    public void Dialog_opened_after_a_keyboard_preview_page_is_centered()
    {
        var page = new MavContentPage("Root");
        var navigation = new MavNavigationPage(page);
        var window = new Window
        {
            Width = 400,
            Height = 800,
            Content = navigation,
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var preview = new MavKeyboardAvoidingHost
        {
            SimulatedKeyboardHeight = 120,
            Child = new Border(),
        };
        page.Child = preview;
        Dispatcher.UIThread.RunJobs();

        page.Child = new Border();
        for (var i = 0; i < 3; i++)
            Dispatcher.UIThread.RunJobs();

        var dialog = new MavDialog
        {
            Title = "Delete project?",
            Message = "This action cannot be undone.",
            ConfirmText = "Delete",
            DismissText = "Cancel",
        };
        dialog.Show(navigation);
        for (var i = 0; i < 3; i++)
            Dispatcher.UIThread.RunJobs();

        var card = navigation.GetVisualDescendants()
            .OfType<Mav.Primitives.MavSurface>()
            .First(surface => surface.Radius == 28);
        var center = card.TranslatePoint(
            new Point(card.Bounds.Width / 2, card.Bounds.Height / 2), window)!.Value;

        Assert.InRange(center.Y, 399, 401);
        Assert.Equal(800, MavOverlay.GetOverlaySize(navigation).Height, 1);

        dialog.Close();
        window.Close();
    }

    [AvaloniaFact]
    public void Popover_stays_inside_the_overlay_when_the_keyboard_opens()
    {
        var page = new MavContentPage("Root");
        var navigation = new MavNavigationPage(page);
        var window = new Window
        {
            Width = 400,
            Height = 800,
            Content = navigation,
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        navigation.KeyboardHost.AnimateInset = false;

        var field = new MavTextField("Name")
        {
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        page.Child = field;
        Dispatcher.UIThread.RunJobs();

        var popover = new MavPopover(new Border { Height = 120 }, "Details");
        popover.Show(field);
        Dispatcher.UIThread.RunJobs();

        var card = navigation.GetVisualDescendants()
            .OfType<Mav.Primitives.MavSurface>()
            .First(surface => surface.Radius == 16);

        navigation.KeyboardHost.SimulatedKeyboardHeight = 300;
        for (var i = 0; i < 3; i++)
            Dispatcher.UIThread.RunJobs();

        var overlaySize = MavOverlay.GetOverlaySize(field);
        Assert.True(overlaySize.Height < 800);
        Assert.True(card.Bounds.Bottom <= overlaySize.Height + 0.5);

        popover.Close();
        window.Close();
    }
}
