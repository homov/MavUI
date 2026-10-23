using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mav.Controls;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;
using Xunit;

namespace Mav.Tests;

public class LifetimeTests
{
    [AvaloniaFact]
    public void Tooltip_does_not_root_its_owner()
    {
        var weak = CreateTooltipOwner();

        CollectGarbage();

        Assert.False(weak.IsAlive, "a detached tooltip owner was kept alive by static state");
    }

    [AvaloniaFact]
    public void Flyout_does_not_root_its_trigger()
    {
        var weak = CreateFlyoutTrigger();

        CollectGarbage();

        Assert.False(weak.IsAlive, "a detached flyout trigger was kept alive by static state");
    }

    [AvaloniaFact]
    public void Disposed_theme_service_does_not_stay_subscribed_to_global_changes()
    {
        var weak = CreateDisposedThemeService();

        CollectGarbage();

        Assert.False(weak.IsAlive, "a disposed MavThemeService was kept alive by MavTheme.Changed");
    }

    [AvaloniaFact]
    public void Closing_a_window_releases_its_overlay_handles_and_root()
    {
        var handlesBefore = StaticListCount("OverlayHandles");
        var rootsBefore = StaticListCount("OverlayRoots");

        var anchor = new MavButton("Anchor");
        var page = new MavContentPage("Root") { Child = anchor };
        var navigation = new MavNavigationPage(page);
        var window = new Window { Width = 480, Height = 640, Content = navigation };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        MavOverlay.Show(anchor, new Border { Width = 100, Height = 100 }, onScrimTap: () => { });
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(handlesBefore + 1, StaticListCount("OverlayHandles"));
        Assert.Equal(rootsBefore + 1, StaticListCount("OverlayRoots"));

        window.Close();
        Dispatcher.UIThread.RunJobs();

        Assert.False(MavOverlay.HasActiveOverlay);
        Assert.Equal(handlesBefore, StaticListCount("OverlayHandles"));
        Assert.Equal(rootsBefore, StaticListCount("OverlayRoots"));
    }

    [AvaloniaFact]
    public void Closing_an_overlay_releases_content_and_invokes_onClosed_once()
    {
        var anchor = new MavButton("Anchor");
        var page = new MavContentPage("Root") { Child = anchor };
        var window = new Window
        {
            Width = 480,
            Height = 640,
            Content = new MavNavigationPage(page),
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var content = new Border();
        var closed = 0;
        var handle = MavOverlay.Show(
            anchor,
            content,
            onScrimTap: () => { },
            onClosed: () => closed++);

        Assert.Equal(0, closed);

        handle.Dispose();
        handle.Dispose();

        Assert.Equal(1, closed);
        Assert.Null(content.Parent);

        window.Close();
    }

    [AvaloniaFact]
    public void Externally_closed_indefinite_snackbar_clears_static_state_and_queue()
    {
        var anchor = new MavButton("Anchor");
        var page = new MavContentPage("Root") { Child = anchor };
        var window = new Window
        {
            Width = 480,
            Height = 640,
            Content = new MavNavigationPage(page),
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            MavSnackbar.ClearQueue();
            MavSnackbar.Show("pending", duration: MavSnackbarDuration.Indefinite, anchor: anchor);
            Dispatcher.UIThread.RunJobs();

            var active = GetStaticField<IDisposable>(typeof(MavSnackbar), "_activeHandle");
            Assert.NotNull(active);

            // Simulate a top-level/page teardown that disposes the overlay handle
            // without going through MavSnackbar.Dismiss().
            active.Dispose();

            Assert.Null(GetStaticField<IDisposable>(typeof(MavSnackbar), "_activeHandle"));
            Assert.Equal(0, MavSnackbar.QueueCount);

            MavSnackbar.Show("next", duration: MavSnackbarDuration.Indefinite, anchor: anchor);
            Assert.Equal(0, MavSnackbar.QueueCount);
            MavSnackbar.Dismiss();
        }
        finally
        {
            MavSnackbar.Dismiss();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Closing_a_window_clears_an_active_snackbar()
    {
        var anchor = new MavButton("Anchor");
        var page = new MavContentPage("Root") { Child = anchor };
        var window = new Window
        {
            Width = 480,
            Height = 640,
            Content = new MavNavigationPage(page),
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        MavSnackbar.ClearQueue();
        MavSnackbar.Show("indefinite", duration: MavSnackbarDuration.Indefinite, anchor: anchor);
        Dispatcher.UIThread.RunJobs();
        Assert.NotNull(GetStaticField<IDisposable>(typeof(MavSnackbar), "_activeHandle"));

        window.Close();
        Dispatcher.UIThread.RunJobs();

        Assert.Null(GetStaticField<IDisposable>(typeof(MavSnackbar), "_activeHandle"));
        Assert.Equal(0, MavSnackbar.QueueCount);
    }

    [AvaloniaFact]
    public void Externally_closed_tooltip_clears_static_handle()
    {
        var anchor = new MavButton("Anchor");
        var page = new MavContentPage("Root") { Child = anchor };
        var window = new Window
        {
            Width = 480,
            Height = 640,
            Content = new MavNavigationPage(page),
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            MavTooltip.Show(anchor, "tip", durationMs: 0);
            Dispatcher.UIThread.RunJobs();

            var handle = GetStaticField<IDisposable>(typeof(MavTooltip), "_handle");
            Assert.NotNull(handle);

            handle.Dispose();

            Assert.Null(GetStaticField<IDisposable>(typeof(MavTooltip), "_handle"));
            Assert.Null(GetStaticField<DispatcherTimer>(typeof(MavTooltip), "_timer"));
        }
        finally
        {
            MavTooltip.Dismiss();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Detached_top_app_bar_does_not_stay_subscribed_to_scroll_viewer()
    {
        var (scroll, weak) = CreateDetachedAppBar();

        CollectGarbage();
        GC.KeepAlive(scroll);

        Assert.False(weak.IsAlive, "a detached MavTopAppBar was kept alive by ScrollViewer.ScrollChanged");
    }

    [AvaloniaFact]
    public void Detached_top_app_bar_without_scroll_is_released()
    {
        var weak = CreateDetachedPlainAppBar();

        CollectGarbage();

        Assert.False(weak.IsAlive, "a detached MavTopAppBar was kept alive by theme state");
    }

    [AvaloniaFact]
    public void Keyboard_host_tracks_the_last_focused_field_weakly()
    {
        var field = new TextBox();
        var host = new MavKeyboardAvoidingHost(field);
        var window = new Window { Width = 480, Height = 640, Content = host };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.True(field.Focus());
        Dispatcher.UIThread.RunJobs();

        var fieldInfo = typeof(MavKeyboardAvoidingHost).GetField(
            "_lastFocused",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(fieldInfo);

        var lastFocused = Assert.IsType<WeakReference<Control>>(fieldInfo.GetValue(host));
        Assert.True(lastFocused.TryGetTarget(out var target));
        Assert.Same(field, target);

        window.Close();
    }

    [AvaloniaFact]
    public void Removed_tab_item_is_not_retained_by_tabs()
    {
        var (tabs, weak) = CreateRemovedTabItem();

        CollectGarbage();
        GC.KeepAlive(tabs);

        Assert.False(weak.IsAlive, "MavTabs retained a removed MavTabItem through its event wiring");
    }

    [AvaloniaFact]
    public void Removed_button_is_not_retained_by_button_group()
    {
        var (group, weak) = CreateRemovedGroupButton();

        CollectGarbage();
        GC.KeepAlive(group);

        Assert.False(weak.IsAlive, "MavButtonGroup retained a removed MavButton through its event wiring");
    }

    [AvaloniaFact]
    public void Removed_radio_is_not_retained_by_radio_group()
    {
        var (group, weak) = CreateRemovedRadio();

        CollectGarbage();
        GC.KeepAlive(group);

        Assert.False(weak.IsAlive, "MavRadioGroup retained a removed MavRadioButton through its event wiring");
    }

    [AvaloniaFact]
    public void Removed_chip_is_not_retained_by_chip_group()
    {
        var (group, weak) = CreateRemovedChip();

        CollectGarbage();
        GC.KeepAlive(group);

        Assert.False(weak.IsAlive, "MavChipGroup retained a removed MavChip through its event wiring");
    }

    [AvaloniaFact]
    public void Cleared_tabs_release_their_items()
    {
        var (tabs, weak) = CreateClearedTabs();

        CollectGarbage();
        GC.KeepAlive(tabs);

        Assert.False(weak.IsAlive, "MavTabs retained items after Items.Clear()");
    }

    [AvaloniaFact]
    public void List_unsubscribes_from_ItemsSource_when_detached_and_resubscribes_on_return()
    {
        var source = new ObservableCollection<int>();
        var list = new MavList { ItemsSource = source };
        var window = new Window { Content = list };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        source.Add(1);
        Dispatcher.UIThread.RunJobs();
        Assert.Single(list.Children.OfType<MavListItem>());

        window.Content = null;
        Dispatcher.UIThread.RunJobs();

        source.Add(2);
        Dispatcher.UIThread.RunJobs();
        Assert.Single(list.Children.OfType<MavListItem>());

        window.Content = list;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, list.Children.OfType<MavListItem>().Count());

        source.Add(3);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(3, list.Children.OfType<MavListItem>().Count());

        window.Close();
    }

    [AvaloniaFact]
    public void DataTable_unsubscribes_from_ItemsSource_when_detached_and_resubscribes_on_return()
    {
        var source = new ObservableCollection<int>();
        var table = new MavDataTable { ItemsSource = source };
        table.AddColumn("Value", value => value, isNumeric: true);

        var window = new Window { Content = table };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        source.Add(1);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, RowCount(table));

        window.Content = null;
        Dispatcher.UIThread.RunJobs();

        source.Add(2);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, RowCount(table));

        window.Content = table;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, RowCount(table));

        source.Add(3);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(3, RowCount(table));

        window.Close();
    }

    [AvaloniaFact]
    public void Dismissables_bound_to_a_top_level_are_released_when_it_closes()
    {
        var window = new Window { Width = 320, Height = 240 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        MavOverlay.TrackTopLevel(window);

        var dismissed = false;
        var handle = MavOverlay.PushDismissable(() => dismissed = true);
        Assert.True(MavOverlay.HasActiveOverlay);

        window.Close();
        Dispatcher.UIThread.RunJobs();

        Assert.False(MavOverlay.HasActiveOverlay, "dismiss stack retained a handle after top-level close");
        Assert.False(dismissed, "closing the top level must not invoke the dismiss callback");
        handle.Dispose();
    }

    [AvaloniaFact]
    public void Dead_overlay_hosts_are_pruned_when_a_host_is_registered()
    {
        var liveOwner = new Border();
        var liveHost = new Panel();
        MavOverlay.RegisterHost(liveOwner, liveHost);
        var baseline = StaticListCount("Hosts");

        RegisterDeadHost();
        CollectGarbage();

        var nextOwner = new Border();
        var nextHost = new Panel();
        MavOverlay.RegisterHost(nextOwner, nextHost);

        Assert.Equal(baseline + 1, StaticListCount("Hosts"));
        MavOverlay.UnregisterHost(liveOwner);
        MavOverlay.UnregisterHost(nextOwner);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RegisterDeadHost()
    {
        MavOverlay.RegisterHost(new Border(), new Panel());
    }

    [AvaloniaFact]
    public void Theme_service_can_be_disposed_through_its_interface()
    {
        IMavThemeService service = new MavThemeService();
        service.Dispose();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDisposedThemeService()
    {
        var service = new MavThemeService();
        service.Dispose();
        return new WeakReference(service);
    }

    private static int StaticListCount(string fieldName)
    {
        var field = typeof(MavOverlay).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var list = Assert.IsAssignableFrom<IList>(field.GetValue(null));
        return list.Count;
    }

    private static T? GetStaticField<T>(Type type, string fieldName) where T : class
    {
        var field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return field.GetValue(null) as T;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (ScrollViewer Scroll, WeakReference AppBar) CreateDetachedAppBar()
    {
        var scroll = new ScrollViewer();
        var appBar = new MavTopAppBar("Title") { ScrollViewer = scroll };
        var window = new Window { Width = 480, Height = 640, Content = appBar };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var weak = new WeakReference(appBar);
        window.Content = null;
        Dispatcher.UIThread.RunJobs();
        Assert.False(appBar.IsAttachedToVisualTree());
        window.Close();
        Dispatcher.UIThread.RunJobs();

        return (scroll, weak);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDetachedPlainAppBar()
    {
        var appBar = new MavTopAppBar("Title");
        var window = new Window { Width = 480, Height = 640, Content = appBar };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var weak = new WeakReference(appBar);
        window.Content = null;
        Dispatcher.UIThread.RunJobs();
        Assert.False(appBar.IsAttachedToVisualTree());
        window.Close();
        Dispatcher.UIThread.RunJobs();

        return weak;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (MavTabs Tabs, WeakReference Item) CreateRemovedTabItem()
    {
        var tabs = new MavTabs();
        var item = new MavTabItem("Removed");
        tabs.Tabs.Add(item);
        tabs.Tabs.Remove(item);
        return (tabs, new WeakReference(item));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (MavButtonGroup Group, WeakReference Item) CreateRemovedGroupButton()
    {
        var group = new MavButtonGroup();
        var button = new MavButton("Removed");
        group.Children.Add(button);
        group.Children.Remove(button);
        return (group, new WeakReference(button));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (MavRadioGroup Group, WeakReference Item) CreateRemovedRadio()
    {
        var group = new MavRadioGroup();
        var radio = new MavRadioButton("Removed");
        group.Children.Add(radio);
        group.Children.Remove(radio);
        return (group, new WeakReference(radio));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (MavChipGroup Group, WeakReference Item) CreateRemovedChip()
    {
        var group = new MavChipGroup();
        var chip = new MavChip("Removed");
        group.Chips.Add(chip);
        group.Chips.Remove(chip);
        return (group, new WeakReference(chip));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (MavTabs Tabs, WeakReference Item) CreateClearedTabs()
    {
        var tabs = new MavTabs();
        var item = new MavTabItem("Cleared");
        tabs.Tabs.Add(item);
        tabs.Tabs.Clear();
        return (tabs, new WeakReference(item));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateTooltipOwner()
    {
        var owner = new Border();
        MavTooltip.SetText(owner, "Tooltip");
        return new WeakReference(owner);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateFlyoutTrigger()
    {
        var trigger = new Border();
        MavFlyout.SetMenu(trigger, new MavMenu());
        return new WeakReference(trigger);
    }

    private static int RowCount(MavDataTable table) =>
        table.GetVisualDescendants()
            .OfType<MavInkSurface>()
            .Count(surface => surface.Child is Grid);

    private static void CollectGarbage()
    {
        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        GC.Collect();
    }
}
