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
using Md3.Avalonia.Controls;
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;
using Xunit;

namespace Md3.Avalonia.Tests;

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

        Assert.False(weak.IsAlive, "a disposed MdThemeService was kept alive by MdTheme.Changed");
    }

    [AvaloniaFact]
    public void Closing_a_window_releases_its_overlay_handles_and_root()
    {
        var handlesBefore = StaticListCount("OverlayHandles");
        var rootsBefore = StaticListCount("OverlayRoots");

        var anchor = new MdButton("Anchor");
        var page = new MdContentPage("Root") { Content = anchor };
        var navigation = new MdNavigationPage(page);
        var window = new Window { Width = 480, Height = 640, Content = navigation };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        MdOverlay.Show(anchor, new Border { Width = 100, Height = 100 }, onScrimTap: () => { });
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(handlesBefore + 1, StaticListCount("OverlayHandles"));
        Assert.Equal(rootsBefore + 1, StaticListCount("OverlayRoots"));

        window.Close();
        Dispatcher.UIThread.RunJobs();

        Assert.False(MdOverlay.HasActiveOverlay);
        Assert.Equal(handlesBefore, StaticListCount("OverlayHandles"));
        Assert.Equal(rootsBefore, StaticListCount("OverlayRoots"));
    }

    [AvaloniaFact]
    public void Closing_an_overlay_releases_content_and_invokes_onClosed_once()
    {
        var anchor = new MdButton("Anchor");
        var page = new MdContentPage("Root") { Content = anchor };
        var window = new Window
        {
            Width = 480,
            Height = 640,
            Content = new MdNavigationPage(page),
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var content = new Border();
        var closed = 0;
        var handle = MdOverlay.Show(
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
        var anchor = new MdButton("Anchor");
        var page = new MdContentPage("Root") { Content = anchor };
        var window = new Window
        {
            Width = 480,
            Height = 640,
            Content = new MdNavigationPage(page),
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            MdSnackbar.ClearQueue();
            MdSnackbar.Show("pending", duration: MdSnackbarDuration.Indefinite, anchor: anchor);
            Dispatcher.UIThread.RunJobs();

            var active = GetStaticField<IDisposable>(typeof(MdSnackbar), "_activeHandle");
            Assert.NotNull(active);

            // Simulate a top-level/page teardown that disposes the overlay handle
            // without going through MdSnackbar.Dismiss().
            active.Dispose();

            Assert.Null(GetStaticField<IDisposable>(typeof(MdSnackbar), "_activeHandle"));
            Assert.Equal(0, MdSnackbar.QueueCount);

            MdSnackbar.Show("next", duration: MdSnackbarDuration.Indefinite, anchor: anchor);
            Assert.Equal(0, MdSnackbar.QueueCount);
            MdSnackbar.Dismiss();
        }
        finally
        {
            MdSnackbar.Dismiss();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Closing_a_window_clears_an_active_snackbar()
    {
        var anchor = new MdButton("Anchor");
        var page = new MdContentPage("Root") { Content = anchor };
        var window = new Window
        {
            Width = 480,
            Height = 640,
            Content = new MdNavigationPage(page),
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        MdSnackbar.ClearQueue();
        MdSnackbar.Show("indefinite", duration: MdSnackbarDuration.Indefinite, anchor: anchor);
        Dispatcher.UIThread.RunJobs();
        Assert.NotNull(GetStaticField<IDisposable>(typeof(MdSnackbar), "_activeHandle"));

        window.Close();
        Dispatcher.UIThread.RunJobs();

        Assert.Null(GetStaticField<IDisposable>(typeof(MdSnackbar), "_activeHandle"));
        Assert.Equal(0, MdSnackbar.QueueCount);
    }

    [AvaloniaFact]
    public void Externally_closed_tooltip_clears_static_handle()
    {
        var anchor = new MdButton("Anchor");
        var page = new MdContentPage("Root") { Content = anchor };
        var window = new Window
        {
            Width = 480,
            Height = 640,
            Content = new MdNavigationPage(page),
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        try
        {
            MdTooltip.Show(anchor, "tip", durationMs: 0);
            Dispatcher.UIThread.RunJobs();

            var handle = GetStaticField<IDisposable>(typeof(MdTooltip), "_handle");
            Assert.NotNull(handle);

            handle.Dispose();

            Assert.Null(GetStaticField<IDisposable>(typeof(MdTooltip), "_handle"));
            Assert.Null(GetStaticField<DispatcherTimer>(typeof(MdTooltip), "_timer"));
        }
        finally
        {
            MdTooltip.Dismiss();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Detached_top_app_bar_does_not_stay_subscribed_to_scroll_viewer()
    {
        var (scroll, weak) = CreateDetachedAppBar();

        CollectGarbage();
        GC.KeepAlive(scroll);

        Assert.False(weak.IsAlive, "a detached MdTopAppBar was kept alive by ScrollViewer.ScrollChanged");
    }

    [AvaloniaFact]
    public void Detached_top_app_bar_without_scroll_is_released()
    {
        var weak = CreateDetachedPlainAppBar();

        CollectGarbage();

        Assert.False(weak.IsAlive, "a detached MdTopAppBar was kept alive by theme state");
    }

    [AvaloniaFact]
    public void Keyboard_host_tracks_the_last_focused_field_weakly()
    {
        var field = new TextBox();
        var host = new MdKeyboardAvoidingHost(field);
        var window = new Window { Width = 480, Height = 640, Content = host };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.True(field.Focus());
        Dispatcher.UIThread.RunJobs();

        var fieldInfo = typeof(MdKeyboardAvoidingHost).GetField(
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

        Assert.False(weak.IsAlive, "MdTabs retained a removed MdTabItem through its event wiring");
    }

    [AvaloniaFact]
    public void Removed_button_is_not_retained_by_button_group()
    {
        var (group, weak) = CreateRemovedGroupButton();

        CollectGarbage();
        GC.KeepAlive(group);

        Assert.False(weak.IsAlive, "MdButtonGroup retained a removed MdButton through its event wiring");
    }

    [AvaloniaFact]
    public void Removed_radio_is_not_retained_by_radio_group()
    {
        var (group, weak) = CreateRemovedRadio();

        CollectGarbage();
        GC.KeepAlive(group);

        Assert.False(weak.IsAlive, "MdRadioGroup retained a removed MdRadioButton through its event wiring");
    }

    [AvaloniaFact]
    public void Removed_chip_is_not_retained_by_chip_group()
    {
        var (group, weak) = CreateRemovedChip();

        CollectGarbage();
        GC.KeepAlive(group);

        Assert.False(weak.IsAlive, "MdChipGroup retained a removed MdChip through its event wiring");
    }

    [AvaloniaFact]
    public void Cleared_tabs_release_their_items()
    {
        var (tabs, weak) = CreateClearedTabs();

        CollectGarbage();
        GC.KeepAlive(tabs);

        Assert.False(weak.IsAlive, "MdTabs retained items after Items.Clear()");
    }

    [AvaloniaFact]
    public void List_unsubscribes_from_ItemsSource_when_detached_and_resubscribes_on_return()
    {
        var source = new ObservableCollection<int>();
        var list = new MdList { ItemsSource = source };
        var window = new Window { Content = list };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        source.Add(1);
        Dispatcher.UIThread.RunJobs();
        Assert.Single(list.Children.OfType<MdListItem>());

        window.Content = null;
        Dispatcher.UIThread.RunJobs();

        source.Add(2);
        Dispatcher.UIThread.RunJobs();
        Assert.Single(list.Children.OfType<MdListItem>());

        window.Content = list;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(2, list.Children.OfType<MdListItem>().Count());

        source.Add(3);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(3, list.Children.OfType<MdListItem>().Count());

        window.Close();
    }

    [AvaloniaFact]
    public void DataTable_unsubscribes_from_ItemsSource_when_detached_and_resubscribes_on_return()
    {
        var source = new ObservableCollection<int>();
        var table = new MdDataTable { ItemsSource = source };
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
        MdOverlay.TrackTopLevel(window);

        var dismissed = false;
        var handle = MdOverlay.PushDismissable(() => dismissed = true);
        Assert.True(MdOverlay.HasActiveOverlay);

        window.Close();
        Dispatcher.UIThread.RunJobs();

        Assert.False(MdOverlay.HasActiveOverlay, "dismiss stack retained a handle after top-level close");
        Assert.False(dismissed, "closing the top level must not invoke the dismiss callback");
        handle.Dispose();
    }

    [AvaloniaFact]
    public void Dead_overlay_hosts_are_pruned_when_a_host_is_registered()
    {
        var liveOwner = new Border();
        var liveHost = new Panel();
        MdOverlay.RegisterHost(liveOwner, liveHost);
        var baseline = StaticListCount("Hosts");

        RegisterDeadHost();
        CollectGarbage();

        var nextOwner = new Border();
        var nextHost = new Panel();
        MdOverlay.RegisterHost(nextOwner, nextHost);

        Assert.Equal(baseline + 1, StaticListCount("Hosts"));
        MdOverlay.UnregisterHost(liveOwner);
        MdOverlay.UnregisterHost(nextOwner);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RegisterDeadHost()
    {
        MdOverlay.RegisterHost(new Border(), new Panel());
    }

    [AvaloniaFact]
    public void Theme_service_can_be_disposed_through_its_interface()
    {
        IMdThemeService service = new MdThemeService();
        service.Dispose();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateDisposedThemeService()
    {
        var service = new MdThemeService();
        service.Dispose();
        return new WeakReference(service);
    }

    private static int StaticListCount(string fieldName)
    {
        var field = typeof(MdOverlay).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
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
        var appBar = new MdTopAppBar("Title") { ScrollViewer = scroll };
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
        var appBar = new MdTopAppBar("Title");
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
    private static (MdTabs Tabs, WeakReference Item) CreateRemovedTabItem()
    {
        var tabs = new MdTabs();
        var item = new MdTabItem("Removed");
        tabs.Tabs.Add(item);
        tabs.Tabs.Remove(item);
        return (tabs, new WeakReference(item));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (MdButtonGroup Group, WeakReference Item) CreateRemovedGroupButton()
    {
        var group = new MdButtonGroup();
        var button = new MdButton("Removed");
        group.Children.Add(button);
        group.Children.Remove(button);
        return (group, new WeakReference(button));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (MdRadioGroup Group, WeakReference Item) CreateRemovedRadio()
    {
        var group = new MdRadioGroup();
        var radio = new MdRadioButton("Removed");
        group.Children.Add(radio);
        group.Children.Remove(radio);
        return (group, new WeakReference(radio));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (MdChipGroup Group, WeakReference Item) CreateRemovedChip()
    {
        var group = new MdChipGroup();
        var chip = new MdChip("Removed");
        group.Chips.Add(chip);
        group.Chips.Remove(chip);
        return (group, new WeakReference(chip));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (MdTabs Tabs, WeakReference Item) CreateClearedTabs()
    {
        var tabs = new MdTabs();
        var item = new MdTabItem("Cleared");
        tabs.Tabs.Add(item);
        tabs.Tabs.Clear();
        return (tabs, new WeakReference(item));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateTooltipOwner()
    {
        var owner = new Border();
        MdTooltip.SetText(owner, "Tooltip");
        return new WeakReference(owner);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateFlyoutTrigger()
    {
        var trigger = new Border();
        MdFlyout.SetMenu(trigger, new MdMenu());
        return new WeakReference(trigger);
    }

    private static int RowCount(MdDataTable table) =>
        table.GetVisualDescendants()
            .OfType<MdInkSurface>()
            .Count(surface => surface.Content is Grid);

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
