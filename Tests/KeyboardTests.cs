using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Platform;
using Xunit;

namespace Md3.Avalonia.Tests;

public class KeyboardTests
{
    [AvaloniaFact]
    public void Simulated_keyboard_inset_lifts_the_page_and_the_fab()
    {
        var page = new MdContentPage("Root")
        {
            FloatingActionButton = new MdFab(MdIcons.Add),
            Content = new Border { Height = 2000 },
        };
        var navigation = new MdNavigationPage(page);
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
        var fab = navigation.GetVisualDescendants().OfType<MdFab>().Single();
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
        var page = new MdContentPage("Root")
        {
            Content = new Border { Height = 2000 },
        };
        var navigation = new MdNavigationPage(page);
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
        var page = new MdContentPage("Root");
        var navigation = new MdNavigationPage(page);
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
        var preview = new MdKeyboardAvoidingHost
        {
            SimulatedKeyboardHeight = 120,
            Body = new Border(),
        };
        page.Content = preview;
        for (var i = 0; i < 3; i++)
            Dispatcher.UIThread.RunJobs();
        MdOverlay.GetOverlaySize(preview);
        for (var i = 0; i < 3; i++)
            Dispatcher.UIThread.RunJobs();
        Assert.True(MdOverlay.GetOverlaySize(preview).Height < 700);

        page.Content = new Border();
        for (var i = 0; i < 3; i++)
            Dispatcher.UIThread.RunJobs();

        Assert.Equal(800, MdOverlay.GetOverlaySize(navigation).Height, 1);

        window.Close();
    }

    [AvaloniaFact]
    public void Dialog_opened_after_a_keyboard_preview_page_is_centered()
    {
        var page = new MdContentPage("Root");
        var navigation = new MdNavigationPage(page);
        var window = new Window
        {
            Width = 400,
            Height = 800,
            Content = navigation,
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var preview = new MdKeyboardAvoidingHost
        {
            SimulatedKeyboardHeight = 120,
            Body = new Border(),
        };
        page.Content = preview;
        Dispatcher.UIThread.RunJobs();

        page.Content = new Border();
        for (var i = 0; i < 3; i++)
            Dispatcher.UIThread.RunJobs();

        var dialog = new MdDialog
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
            .OfType<Md3.Avalonia.Primitives.MdSurface>()
            .First(surface => surface.Radius == 28);
        var center = card.TranslatePoint(
            new Point(card.Bounds.Width / 2, card.Bounds.Height / 2), window)!.Value;

        Assert.InRange(center.Y, 399, 401);
        Assert.Equal(800, MdOverlay.GetOverlaySize(navigation).Height, 1);

        dialog.Close();
        window.Close();
    }

    [AvaloniaFact]
    public void Popover_stays_inside_the_overlay_when_the_keyboard_opens()
    {
        var page = new MdContentPage("Root");
        var navigation = new MdNavigationPage(page);
        var window = new Window
        {
            Width = 400,
            Height = 800,
            Content = navigation,
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        navigation.KeyboardHost.AnimateInset = false;

        var field = new MdTextField("Name")
        {
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        page.Content = field;
        Dispatcher.UIThread.RunJobs();

        var popover = new MdPopover(new Border { Height = 120 }, "Details");
        popover.Show(field);
        Dispatcher.UIThread.RunJobs();

        var card = navigation.GetVisualDescendants()
            .OfType<Md3.Avalonia.Primitives.MdSurface>()
            .First(surface => surface.Radius == 16);

        navigation.KeyboardHost.SimulatedKeyboardHeight = 300;
        for (var i = 0; i < 3; i++)
            Dispatcher.UIThread.RunJobs();

        var overlaySize = MdOverlay.GetOverlaySize(field);
        Assert.True(overlaySize.Height < 800);
        Assert.True(card.Bounds.Bottom <= overlaySize.Height + 0.5);

        popover.Close();
        window.Close();
    }
}
