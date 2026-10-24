using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Mav;
using Mav.Controls;
using Mav.Platform;
using Mav.Theming;
using Xunit;

namespace Mav.Tests;

/// <summary>
/// The FAB speed dial must not move the FAB when it opens: the action pills float above it in the
/// overlay. Before, the pills sat in a stack above the trigger, so opening the menu made the
/// control taller and a parent that lays its children out from the top pushed the FAB down while
/// the pills appeared where it used to be.
/// </summary>
public class FabMenuTests
{
    private const double FabGap = 12;

    private const double WindowWidth = 420;
    private const double WindowHeight = 600;

    /// <summary>
    /// The pills float in the overlay, whose host belongs to <see cref="MavNavigationPage"/>: the
    /// menu is shown the way the gallery shows it, inside a page in a navigation host. The column
    /// stacks its children from the top, like the gallery's wrap panel: with the pills in the
    /// menu's own layout, opening moved the FAB down and left the pills where it used to be.
    /// </summary>
    private static (Window Window, MavFabMenu Menu) ShowMenu(double spacerHeight = 320)
    {
        var menu = new MavFabMenu()
            .AddItem("New document", MavIcons.NoteAdd)
            .AddItem("Upload", MavIcons.Upload);

        var column = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
        column.Children.Add(new TextBlock { Text = "Actions" });
        column.Children.Add(new Border { Height = spacerHeight });
        column.Children.Add(menu);

        var navigation = new MavNavigationPage(new MavContentPage("Root", column));
        var window = new Window { Width = WindowWidth, Height = WindowHeight, Content = navigation };
        window.Show();
        LayOut(window);
        return (window, menu);
    }

    private static void LayOut(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(WindowWidth, WindowHeight));
        window.Arrange(new Rect(0, 0, WindowWidth, WindowHeight));
    }

    private static Point Origin(Visual visual, Visual relativeTo) =>
        visual.TranslatePoint(default, relativeTo) ?? default;

    [AvaloniaFact]
    public void Opening_the_menu_keeps_the_fab_in_place()
    {
        var previousMotion = MavTheme.Motion;
        MavTheme.Configure(motion: MavMotionScheme.None);
        var (window, menu) = ShowMenu();
        try
        {
            var closed = menu.Bounds;

            menu.Open();
            LayOut(window);

            Assert.True(menu.IsOpen);
            Assert.Equal(closed.Width, menu.Bounds.Width, 3);
            Assert.Equal(closed.Height, menu.Bounds.Height, 3);
            Assert.Equal(closed.X, menu.Bounds.X, 3);
            Assert.Equal(closed.Y, menu.Bounds.Y, 3);
        }
        finally
        {
            while (MavOverlay.TryDismissTop()) { }
            window.Close();
            MavTheme.Configure(motion: previousMotion);
        }
    }

    [AvaloniaFact]
    public void The_pills_float_above_the_fab_with_their_right_edges_aligned()
    {
        var previousMotion = MavTheme.Motion;
        MavTheme.Configure(motion: MavMotionScheme.None);
        var (window, menu) = ShowMenu();
        try
        {
            menu.Open();
            LayOut(window);

            var pills = menu.ItemsHost;
            Assert.Equal(2, pills.Children.Count);
            Assert.Same(window, TopLevel.GetTopLevel(pills));

            var fab = Origin(menu, window);
            var stack = Origin(pills, window);

            // Just above the FAB, bottom-aligned with the gap, never overlapping it.
            Assert.Equal(fab.Y - FabGap, stack.Y + pills.Bounds.Height, 1);
            Assert.True(stack.Y + pills.Bounds.Height <= fab.Y + 0.5);

            // The pills are not in the menu's layout: it stays a 56 px FAB, right edges aligned.
            Assert.Equal(56, menu.Bounds.Height, 3);
            Assert.Equal(fab.X + menu.Bounds.Width, stack.X + pills.Bounds.Width, 1);
        }
        finally
        {
            while (MavOverlay.TryDismissTop()) { }
            window.Close();
            MavTheme.Configure(motion: previousMotion);
        }
    }

    [AvaloniaFact]
    public void Closing_the_menu_removes_the_pills()
    {
        var previousMotion = MavTheme.Motion;
        MavTheme.Configure(motion: MavMotionScheme.None);
        var (window, menu) = ShowMenu();
        try
        {
            menu.Open();
            LayOut(window);
            Assert.Same(window, TopLevel.GetTopLevel(menu.ItemsHost));

            menu.Close();
            LayOut(window);

            Assert.False(menu.IsOpen);
            Assert.Null(TopLevel.GetTopLevel(menu.ItemsHost));
            Assert.Equal(56, menu.Bounds.Height, 3);
        }
        finally
        {
            while (MavOverlay.TryDismissTop()) { }
            window.Close();
            MavTheme.Configure(motion: previousMotion);
        }
    }

    [AvaloniaFact]
    public void With_no_room_above_the_fab_the_pills_open_below_it()
    {
        var previousMotion = MavTheme.Motion;
        MavTheme.Configure(motion: MavMotionScheme.None);
        // A FAB near the top of the screen: the pills would not fit above it.
        var (window, menu) = ShowMenu(spacerHeight: 0);
        try
        {
            menu.Open();
            LayOut(window);

            var fab = Origin(menu, window);
            var stack = Origin(menu.ItemsHost, window);

            Assert.True(stack.Y >= fab.Y + menu.Bounds.Height - 0.5);
            Assert.Equal(fab.X + menu.Bounds.Width, stack.X + menu.ItemsHost.Bounds.Width, 1);
        }
        finally
        {
            while (MavOverlay.TryDismissTop()) { }
            window.Close();
            MavTheme.Configure(motion: previousMotion);
        }
    }

    [AvaloniaFact]
    public void IsOpen_set_before_the_menu_is_on_screen_opens_it_when_it_attaches()
    {
        var previousMotion = MavTheme.Motion;
        MavTheme.Configure(motion: MavMotionScheme.None);
        Window? window = null;
        try
        {
            var menu = new MavFabMenu().AddItem("Share", MavIcons.Share);
            menu.IsOpen = true;

            window = new Window
            {
                Width = WindowWidth,
                Height = WindowHeight,
                Content = new MavNavigationPage(new MavContentPage("Root", menu)),
            };
            window.Show();
            LayOut(window);

            Assert.True(menu.IsOpen);
            Assert.Same(window, TopLevel.GetTopLevel(menu.ItemsHost));
            Assert.Single(menu.ItemsHost.Children);
        }
        finally
        {
            while (MavOverlay.TryDismissTop()) { }
            window?.Close();
            MavTheme.Configure(motion: previousMotion);
        }
    }
}
