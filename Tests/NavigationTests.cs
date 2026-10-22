using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mav;
using Mav.Controls;
using Mav.Platform;
using Mav.Primitives;
using Xunit;

namespace Mav.Tests;

public class NavigationTests
{
    private sealed class BackPage(string title) : MavContentPage(title)
    {
        public int BackCalls { get; private set; }
        public bool ConsumeBack { get; set; }

        public override bool OnBackPressed()
        {
            BackCalls++;
            return ConsumeBack;
        }
    }

    private static (Window Window, MavNavigationPage Navigation) CreateHost(
        MavContentPage root,
        double width = 800,
        double height = 600)
    {
        var navigation = new MavNavigationPage(root);
        var window = new Window
        {
            Width = width,
            Height = height,
            Content = navigation,
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, navigation);
    }

    [AvaloniaFact]
    public void SetRoot_establishes_a_single_page_stack()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root);

        Assert.Equal(1, navigation.StackDepth);
        Assert.Same(root, navigation.CurrentPage);
        Assert.False(navigation.CanGoBack);
        Assert.False(navigation.GoBackCommand.CanExecute(null));

        window.Close();
    }

    [AvaloniaFact]
    public void Push_and_pop_update_depth_and_current_page()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root);
        var page = new MavContentPage("Page");

        navigation.Push(page, animated: false);

        Assert.Equal(2, navigation.StackDepth);
        Assert.Same(page, navigation.CurrentPage);
        Assert.True(navigation.CanGoBack);
        Assert.True(navigation.GoBackCommand.CanExecute(null));
        Assert.Same(navigation, page.Navigation);

        Assert.True(navigation.Pop(animated: false));

        Assert.Equal(1, navigation.StackDepth);
        Assert.Same(root, navigation.CurrentPage);
        Assert.False(navigation.CanGoBack);
        Assert.Null(page.Navigation);

        window.Close();
    }

    [AvaloniaFact]
    public void Pop_on_the_root_page_is_a_no_op()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root);

        Assert.False(navigation.Pop(animated: false));
        Assert.Equal(1, navigation.StackDepth);

        window.Close();
    }

    [AvaloniaFact]
    public void PopToRoot_removes_every_pushed_page()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root);
        var first = new MavContentPage("First");
        var second = new MavContentPage("Second");
        var third = new MavContentPage("Third");

        navigation.Push(first, animated: false);
        navigation.Push(second, animated: false);
        navigation.Push(third, animated: false);
        navigation.PopToRoot(animated: false);

        Assert.Equal(1, navigation.StackDepth);
        Assert.Same(root, navigation.CurrentPage);
        Assert.Null(first.Navigation);
        Assert.Null(second.Navigation);
        Assert.Null(third.Navigation);

        window.Close();
    }

    [AvaloniaFact]
    public void Replace_swaps_the_top_page_without_growing_the_stack()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root);
        var first = new MavContentPage("First");
        var replacement = new MavContentPage("Replacement");

        navigation.Push(first, animated: false);
        navigation.Replace(replacement, animated: false);

        Assert.Equal(2, navigation.StackDepth);
        Assert.Same(replacement, navigation.CurrentPage);
        Assert.Null(first.Navigation);
        Assert.DoesNotContain(first, navigation.NavigationStack);

        window.Close();
    }

    [AvaloniaFact]
    public void Pushing_the_same_page_twice_throws()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root);
        var page = new MavContentPage("Page");

        navigation.Push(page, animated: false);

        Assert.Throws<InvalidOperationException>(() => navigation.Push(page, animated: false));

        window.Close();
    }

    [AvaloniaFact]
    public void Page_can_consume_back_before_the_stack_pops()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root);
        var page = new BackPage("Page") { ConsumeBack = true };
        navigation.Push(page, animated: false);

        Assert.True(navigation.TryGoBack());
        Assert.Equal(1, page.BackCalls);
        Assert.Equal(2, navigation.StackDepth);

        page.ConsumeBack = false;
        Assert.True(navigation.TryGoBack());
        Assert.Equal(2, page.BackCalls);
        Assert.Equal(1, navigation.StackDepth);

        window.Close();
    }

    [AvaloniaFact]
    public void Safe_area_override_is_applied_to_the_chrome()
    {
        var bottomBar = new MavBottomAppBar();
        var root = new MavContentPage("Root") { BottomBar = bottomBar };
        var (window, navigation) = CreateHost(root);

        navigation.SafeAreaOverride = new Thickness(0, 24, 0, 48);

        Assert.Equal(new Thickness(0, 24, 0, 48), navigation.SafeAreaInsets);
        Assert.Equal(new Thickness(0, 0, 0, 48), bottomBar.SafeAreaPadding);

        var appBar = navigation.GetVisualDescendants().OfType<MavTopAppBar>().First();
        Assert.Equal(new Thickness(0, 24, 0, 0), appBar.SafeAreaPadding);

        navigation.ApplySafeArea = false;
        Assert.Equal(default, navigation.SafeAreaInsets);
        Assert.Equal(default, bottomBar.SafeAreaPadding);
        Assert.Equal(default, appBar.SafeAreaPadding);

        window.Close();
    }

    [AvaloniaFact]
    public void System_back_request_pops_the_top_page()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root);
        navigation.Push(new MavContentPage("Page"), animated: false);

        var args = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        window.RaiseEvent(args);

        Assert.True(args.Handled);
        Assert.Equal(1, navigation.StackDepth);
        Assert.Same(root, navigation.CurrentPage);

        window.Close();
    }

    [AvaloniaFact]
    public void Navigation_page_opts_out_of_automatic_safe_area_padding()
    {
        var (window, navigation) = CreateHost(new MavContentPage("Root"));

        Assert.False(TopLevel.GetAutoSafeAreaPadding(navigation));

        window.Close();
    }

    [AvaloniaFact]
    public void Overlay_falls_back_to_the_navigation_host_without_an_overlay_layer()
    {
        var (window, navigation) = CreateHost(new MavContentPage("Root"));
        var dismissed = false;

        using var handle = MavOverlay.Show(
            navigation,
            new Border { Width = 100, Height = 100 },
            onScrimTap: () => dismissed = true);

        Assert.True(MavOverlay.HasActiveOverlay);

        var args = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        window.RaiseEvent(args);

        Assert.True(args.Handled);
        Assert.True(dismissed);
        Assert.False(MavOverlay.HasActiveOverlay);
        Assert.Equal(1, navigation.StackDepth);

        window.Close();
    }

    [AvaloniaFact]
    public void MavScrollViewer_scrolls_without_an_avalonia_theme()
    {
        var scroll = new MavScrollViewer
        {
            Width = 300,
            Height = 400,
            Content = new Border { Width = 200, Height = 1200 },
        };
        var window = new Window
        {
            Width = 300,
            Height = 400,
            Content = scroll,
        };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.True(scroll.Extent.Height >= 1200);
        Assert.True(scroll.Viewport.Height > 0);

        scroll.ScrollToEnd();
        Dispatcher.UIThread.RunJobs();

        Assert.True(scroll.Offset.Y > 0);

        window.Close();
    }

    [AvaloniaFact]
    public void Pickers_open_in_the_navigation_overlay_host()
    {
        var (window, navigation) = CreateHost(new MavContentPage("Root"));

        var datePicker = new MavDatePicker { SelectedDate = DateTime.Today };
        datePicker.Show(navigation);
        Assert.True(MavOverlay.HasActiveOverlay);
        Assert.True(MavOverlay.TryDismissTop());
        Assert.False(MavOverlay.HasActiveOverlay);

        var timePicker = new MavTimePicker { SelectedTime = TimeSpan.FromHours(9) };
        timePicker.Show(navigation);
        Assert.True(MavOverlay.HasActiveOverlay);
        timePicker.Close();
        Assert.False(MavOverlay.HasActiveOverlay);

        window.Close();
    }

    [AvaloniaFact]
    public void Menu_is_clamped_to_a_narrow_navigation_host()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root, width: 320, height: 640);
        var anchor = new MavButton("Filters")
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        root.Content = anchor;
        Dispatcher.UIThread.RunJobs();

        var menu = new MavMenu { MinWidth = 420 };
        menu.AddItem("A deliberately long menu item");
        menu.Show(anchor);
        Dispatcher.UIThread.RunJobs();

        var card = window.GetVisualDescendants()
            .OfType<MavSurface>()
            .First(surface => surface.Radius == 12);

        Assert.True(card.Bounds.Width <= 304.5);
        Assert.True(card.Bounds.Right <= 312.5);

        menu.Close();
        window.Close();
    }

    [AvaloniaFact]
    public void Menu_reflows_when_the_host_is_resized()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root, width: 900, height: 640);
        var anchor = new MavButton("Filters")
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        root.Content = anchor;
        Dispatcher.UIThread.RunJobs();

        var menu = new MavMenu();
        menu.AddItem("A deliberately long menu item");
        menu.AddItem("Another long menu item");
        menu.Show(anchor);
        Dispatcher.UIThread.RunJobs();

        var wide = window.GetVisualDescendants()
            .OfType<MavSurface>()
            .First(surface => surface.Radius == 12);

        window.Width = 320;
        window.Height = 320;
        for (var i = 0; i < 3; i++)
            Dispatcher.UIThread.RunJobs();

        var card = window.GetVisualDescendants()
            .OfType<MavSurface>()
            .First(surface => surface.Radius == 12);

        Assert.Same(wide, card);
        Assert.True(card.Bounds.Width <= 304.5);
        Assert.True(card.Bounds.Right <= 312.5);
        Assert.True(card.Bounds.Bottom <= 312.5);

        menu.Close();
        window.Close();
    }

    [AvaloniaFact]
    public void Popover_reflows_and_does_not_stick_to_its_first_width()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root, width: 900, height: 640);
        var anchor = new MavButton("Details")
        {
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        root.Content = anchor;
        Dispatcher.UIThread.RunJobs();

        var popover = new MavPopover(new Border { Width = 800, Height = 40 }, "Details");
        popover.Show(anchor);
        Dispatcher.UIThread.RunJobs();

        var wide = window.GetVisualDescendants()
            .OfType<MavSurface>()
            .First(surface => surface.Radius == 16)
            .Bounds.Width;

        window.Width = 260;
        window.Height = 320;
        for (var i = 0; i < 3; i++)
            Dispatcher.UIThread.RunJobs();

        var card = window.GetVisualDescendants()
            .OfType<MavSurface>()
            .First(surface => surface.Radius == 16);

        Assert.True(card.Bounds.Width < wide);
        Assert.True(card.Bounds.Right <= 260.5);
        Assert.True(card.Bounds.Bottom <= 320.5);

        // Grow the host again: the card must follow instead of keeping the small width.
        window.Width = 900;
        window.Height = 640;
        for (var i = 0; i < 3; i++)
            Dispatcher.UIThread.RunJobs();

        card = window.GetVisualDescendants()
            .OfType<MavSurface>()
            .First(surface => surface.Radius == 16);

        Assert.True(card.Bounds.Width > 300);

        popover.Close();
        window.Close();
    }

    [AvaloniaFact]
    public void Dialog_reflows_when_the_host_is_resized()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root, width: 900, height: 640);

        var dialog = new MavDialog
        {
            Title = "Delete project?",
            Message = "This action cannot be undone.",
            ConfirmText = "Delete",
            DismissText = "Cancel",
        };
        dialog.Show(root);
        Dispatcher.UIThread.RunJobs();

        window.Width = 320;
        window.Height = 320;
        for (var i = 0; i < 3; i++)
            Dispatcher.UIThread.RunJobs();

        var card = window.GetVisualDescendants()
            .OfType<MavSurface>()
            .First(surface => surface.Radius == 28);

        Assert.True(card.Bounds.Width <= 272.5);
        Assert.True(card.Bounds.Height <= 272.5);
        Assert.True(card.Bounds.Right <= 320.5);
        Assert.True(card.Bounds.Bottom <= 320.5);

        dialog.Close();
        window.Close();
    }

    [AvaloniaFact]
    public void Time_picker_fits_after_shrinking_the_host()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root, width: 900, height: 640);

        var picker = new MavTimePicker { SelectedTime = TimeSpan.FromHours(9) };
        picker.Show(navigation);
        Dispatcher.UIThread.RunJobs();

        window.Width = 320;
        window.Height = 360;
        for (var i = 0; i < 3; i++)
            Dispatcher.UIThread.RunJobs();

        var card = window.GetVisualDescendants()
            .OfType<MavSurface>()
            .First(surface => surface.Radius == 28);

        Assert.True(card.Bounds.Width <= 280.5);
        Assert.True(card.Bounds.Height <= 320.5);
        Assert.True(card.Bounds.Right <= 320.5);
        Assert.True(card.Bounds.Bottom <= 360.5);

        picker.Close();
        window.Close();
    }

    [AvaloniaFact]
    public void Date_picker_fits_after_shrinking_the_host()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root, width: 900, height: 640);

        var picker = new MavDatePicker { SelectedDate = DateTime.Today };
        picker.Show(navigation);
        Dispatcher.UIThread.RunJobs();

        window.Width = 320;
        window.Height = 360;
        for (var i = 0; i < 3; i++)
            Dispatcher.UIThread.RunJobs();

        var card = window.GetVisualDescendants()
            .OfType<MavSurface>()
            .First(surface => surface.Radius == 28);

        Assert.True(card.Bounds.Width <= 288.5);
        Assert.True(card.Bounds.Height <= 328.5);
        Assert.True(card.Bounds.Right <= 320.5);
        Assert.True(card.Bounds.Bottom <= 360.5);

        picker.Close();
        window.Close();
    }

    [AvaloniaFact]
    public void Popover_is_clamped_and_scrolls_on_a_narrow_navigation_host()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root, width: 320, height: 320);
        var anchor = new MavButton("Details")
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        root.Content = anchor;
        Dispatcher.UIThread.RunJobs();

        var body = new TextBlock
        {
            Text = string.Join("\n", Enumerable.Repeat("A very wide line of popover text", 30)),
            TextWrapping = TextWrapping.NoWrap,
        };
        var popover = new MavPopover(body, "Details");
        popover.Show(anchor);
        Dispatcher.UIThread.RunJobs();

        var card = window.GetVisualDescendants()
            .OfType<MavSurface>()
            .First(surface => surface.Radius == 16);

        Assert.True(card.Bounds.Width <= 297);
        Assert.True(card.Bounds.Right <= 320.5);
        Assert.True(card.Bounds.Height <= 297);
        Assert.True(card.Bounds.Bottom <= 320.5);

        popover.Close();
        window.Close();
    }

    [AvaloniaFact]
    public void Popover_keeps_its_edge_margin_when_anchored_to_the_right_edge()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root, width: 360, height: 640);
        var anchor = new MavButton("Popover")
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
        };
        root.Content = anchor;
        Dispatcher.UIThread.RunJobs();

        var body = new StackPanel
        {
            Children =
            {
                new MavCheckBox("Archived"),
                new MavCheckBox("Shared with me"),
                new MavCheckBox("Has attachments"),
            },
        };
        var popover = new MavPopover(body, "Filters");
        popover.Show(anchor);
        Dispatcher.UIThread.RunJobs();

        var card = window.GetVisualDescendants()
            .OfType<MavSurface>()
            .First(surface => surface.Radius == 16);

        // 360 dp viewport, 12 dp edge margin: the card must not touch the right edge.
        Assert.True(card.Bounds.Right <= 348.5, $"popover right edge was {card.Bounds.Right}");
        Assert.True(card.Bounds.Left >= 11.5, $"popover left edge was {card.Bounds.Left}");

        popover.Close();
        window.Close();
    }

    [AvaloniaFact]
    public void Menu_keeps_its_edge_margin_when_anchored_to_the_right_edge()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root, width: 360, height: 640);
        var anchor = new MavButton("Menu")
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
        };
        root.Content = anchor;
        Dispatcher.UIThread.RunJobs();

        var menu = new MavMenu();
        menu.AddItem("Rename", MavIcons.Edit);
        menu.AddItem("Duplicate", MavIcons.ContentCopy);
        menu.AddDivider();
        menu.AddItem("Delete", MavIcons.Delete);
        menu.Show(anchor);
        Dispatcher.UIThread.RunJobs();

        var card = window.GetVisualDescendants()
            .OfType<MavSurface>()
            .First(surface => surface.Radius == 12);

        // 360 dp viewport, 8 dp edge margin for menus.
        Assert.True(card.Bounds.Right <= 352.5, $"menu right edge was {card.Bounds.Right}");
        Assert.True(card.Bounds.Left >= 7.5, $"menu left edge was {card.Bounds.Left}");

        menu.Close();
        window.Close();
    }

    [AvaloniaFact]
    public void Search_view_fits_a_narrow_navigation_host()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root, width: 320, height: 640);
        var anchor = new MavButton("Search")
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        root.Content = anchor;
        Dispatcher.UIThread.RunJobs();

        var search = new MavSearchView
        {
            SuggestionsSource = new[] { "Alpha", "Beta" },
        };
        search.Show(anchor);
        Dispatcher.UIThread.RunJobs();

        var card = window.GetVisualDescendants()
            .OfType<MavSurface>()
            .First(surface => surface.Radius == 28);

        Assert.True(card.Bounds.Width <= 320.5);
        Assert.True(card.Bounds.Right <= 320.5);

        search.Close();
        window.Close();
    }

    [AvaloniaFact]
    public void Time_and_date_pickers_fit_a_narrow_navigation_host()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root, width: 320, height: 640);

        var time = new MavTimePicker { SelectedTime = TimeSpan.FromHours(9) };
        time.Show(navigation);
        Dispatcher.UIThread.RunJobs();

        var timeCard = window.GetVisualDescendants()
            .OfType<MavSurface>()
            .First(surface => surface.Radius == 28);
        var dial = window.GetVisualDescendants().OfType<MavTimeDial>().First();
        var ok = window.GetVisualDescendants()
            .OfType<MavButton>()
            .First(button => button.Text == "OK");
        var okTopLeft = ok.TranslatePoint(default, timeCard);

        Assert.True(timeCard.Bounds.Width <= 280.5);
        Assert.True(dial.Bounds.Width <= 232.5);
        Assert.Equal(dial.Bounds.Width, dial.Bounds.Height, 1);
        Assert.True(okTopLeft.HasValue);
        Assert.True(okTopLeft.Value.Y + ok.Bounds.Height <= timeCard.Bounds.Height + 0.5);

        time.Close();

        var date = new MavDatePicker { SelectedDate = DateTime.Today };
        date.Show(navigation);
        Dispatcher.UIThread.RunJobs();

        var dateCard = window.GetVisualDescendants()
            .OfType<MavSurface>()
            .First(surface => surface.Radius == 28);
        var calendar = window.GetVisualDescendants().OfType<MavCalendarGrid>().First();

        Assert.True(dateCard.Bounds.Width <= 288.5);
        Assert.True(calendar.Bounds.Width <= 240.5);

        date.Close();
        window.Close();
    }

    [AvaloniaFact]
    public void Color_picker_fits_a_narrow_dialog()
    {
        var root = new MavContentPage("Root");
        var (window, navigation) = CreateHost(root, width: 320, height: 640);
        var anchor = new MavButton("Color")
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        root.Content = anchor;
        Dispatcher.UIThread.RunJobs();

        _ = MavColorPicker.PickAsync(anchor);
        Dispatcher.UIThread.RunJobs();

        var picker = window.GetVisualDescendants().OfType<MavColorPicker>().First();
        Assert.True(picker.Bounds.Width <= 225);

        var confirm = window.GetVisualDescendants()
            .OfType<MavButton>()
            .First(button => button.Text == "Select");
        var card = window.GetVisualDescendants()
            .OfType<MavSurface>()
            .First(surface => surface.Radius == 28);
        var confirmTopLeft = confirm.TranslatePoint(default, card);

        Assert.True(confirmTopLeft.HasValue);
        Assert.True(confirmTopLeft.Value.Y + confirm.Bounds.Height <= card.Bounds.Height + 0.5);

        MavOverlay.TryDismissTop();
        window.Close();
    }
}

