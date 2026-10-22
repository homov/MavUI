using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Demo;

internal static class PageLayout
{
    public static ScrollViewer Scroll(params Control[] children)
    {
        var stack = new StackPanel
        {
            Spacing = 16,
            MaxWidth = 820,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
        };

        foreach (var child in children)
            stack.Children.Add(child);

        return new MavScrollViewer
        {
            Padding = new Thickness(24),
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = stack,
        };
    }

    public static MavLabel Headline(string text) => new()
    {
        Text = text,
        Role = MavTypeRole.HeadlineMedium,
        TextWrapping = TextWrapping.Wrap,
    };

    public static MavLabel Body(string text) => new()
    {
        Text = text,
        Role = MavTypeRole.BodyLarge,
        Secondary = true,
        TextWrapping = TextWrapping.Wrap,
    };
}

public sealed class HomePage : MavContentPage
{
    public HomePage() : base("Mav Navigation")
    {
        Subtitle = "Custom stack built from Material controls";
        AddAction(MavIcons.DarkMode, ToggleTheme);

        var openControls = new MavButton(
            "Open controls",
            MavButtonVariant.Filled,
            MavIcons.Tune,
            MavButtonSize.Medium);
        openControls.Clicked += (_, _) => Navigation?.Push(new ControlsPage());

        var openList = new MavButton(
            "Open list",
            MavButtonVariant.Tonal,
            MavIcons.Inbox,
            MavButtonSize.Medium);
        openList.Clicked += (_, _) => Navigation?.Push(new InboxPage());

        var openOverlays = new MavButton(
            "Overlays & pickers",
            MavButtonVariant.Tonal,
            MavIcons.Layers,
            MavButtonSize.Medium);
        openOverlays.Clicked += (_, _) => Navigation?.Push(new OverlaysPage());

        var showDialog = new MavButton(
            "Show dialog",
            MavButtonVariant.Outlined,
            MavIcons.Chat,
            MavButtonSize.Medium);
        showDialog.Clicked += (_, _) => ShowDialog();

        var showSheet = new MavButton(
            "Show sheet",
            MavButtonVariant.Outlined,
            MavIcons.MenuOpen,
            MavButtonSize.Medium);
        showSheet.Clicked += (_, _) => ShowSheet();

        var showSnackbar = new MavButton(
            "Show snackbar",
            MavButtonVariant.Text,
            MavIcons.Notifications,
            MavButtonSize.Medium);
        showSnackbar.Clicked += (_, _) =>
            MavSnackbar.Show("Saved with MavSnackbar", "Undo", () => { }, anchor: this);

        var darkMode = new MavSwitch(MavTheme.Current.IsDark, showIcons: true)
        {
            OnIcon = MavIcons.DarkMode,
            OffIcon = MavIcons.LightMode,
        };
        darkMode.Toggled += (_, on) =>
            MavTheme.Mode = on ? MavThemeMode.Dark : MavThemeMode.Light;

        var actions = new WrapPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left,
        };
        void AddStackButton(Control control)
        {
            control.Margin = new Thickness(0, 0, 12, 12);
            actions.Children.Add(control);
        }

        AddStackButton(openControls);
        AddStackButton(openList);
        AddStackButton(openOverlays);
        AddStackButton(showDialog);
        AddStackButton(showSheet);
        AddStackButton(showSnackbar);

        var heroText = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                PageLayout.Headline("Material navigation, not Avalonia pages"),
                PageLayout.Body(
                    "This demo uses MavNavigationPage. The stack, app bar, actions, " +
                    "bottom bar and floating action button are all Mav controls."),
            },
        };

        var hero = new MavCard(MavCardVariant.Filled, heroText);

        var themeGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
        };
        var themeIcon = new MavIcon(MavIcons.Palette, 24)
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 16, 0),
        };
        var themeText = new StackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new MavLabel { Text = "Dark theme", Role = MavTypeRole.TitleMedium },
                new MavLabel
                {
                    Text = "Toggle the global MavTheme palette",
                    Role = MavTypeRole.BodyMedium,
                    Secondary = true,
                    TextWrapping = TextWrapping.Wrap,
                },
            },
        };
        darkMode.VerticalAlignment = VerticalAlignment.Center;
        darkMode.Margin = new Thickness(16, 0, 0, 0);
        themeGrid.Add(themeIcon, 0);
        themeGrid.Add(themeText, 1);
        themeGrid.Add(darkMode, 2);

        var themeCard = new MavCard(MavCardVariant.Outlined, themeGrid);

        Content = PageLayout.Scroll(
            hero,
            new MavLabel { Text = "Try the stack", Role = MavTypeRole.TitleLarge },
            actions,
            themeCard,
            new MavLabel
            {
                Text = "The app bar back arrow is supplied by MavNavigationPage. " +
                       "The page below keeps its own scroll position and state.",
                Role = MavTypeRole.BodyMedium,
                Secondary = true,
                TextWrapping = TextWrapping.Wrap,
            });

        BottomBar = BuildBottomBar();
    }

    private void ToggleTheme()
    {
        MavTheme.ToggleDark();
        MavSnackbar.Show(
            MavTheme.Current.IsDark ? "Dark theme enabled" : "Light theme enabled",
            anchor: this);
    }

    private void ShowDialog()
    {
        var dialog = new MavDialog
        {
            Icon = MavIcons.Info,
            Title = "Material dialog",
            Message = "MavDialog is displayed in the overlay, above the custom navigation stack.",
            ConfirmText = "Continue",
            DismissText = "Cancel",
        };
        dialog.Confirmed += (_, _) => MavSnackbar.Show("Dialog confirmed", anchor: this);
        dialog.Show(this);
    }

    private void ShowSheet()
    {
        var sheet = new MavBottomSheet
        {
            Title = "Quick actions",
            Body = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new MavButton("Create item", MavButtonVariant.Filled, MavIcons.Add),
                    new MavButton("Archive", MavButtonVariant.Text, MavIcons.Archive),
                    new MavButton("Delete", MavButtonVariant.Text, MavIcons.Delete),
                },
            },
        };
        sheet.Show(this);
    }

    private static MavBottomAppBar BuildBottomBar()
    {
        var bar = new MavBottomAppBar(MavIcons.Add, () =>
            MavSnackbar.Show("Bottom app bar FAB clicked"));
        bar.AddAction(MavIcons.Search, () => MavSnackbar.Show("Search action"));
        bar.AddAction(MavIcons.Settings, () => MavSnackbar.Show("Settings action"));
        return bar;
    }
}

public sealed class OverlaysPage : MavContentPage
{
    public OverlaysPage() : base("Overlays & pickers")
    {
        Subtitle = "Adaptive menus, popovers, search, time and color pickers";

        var menuButton = new MavButton(
            "Menu",
            MavButtonVariant.Outlined,
            MavIcons.Menu,
            MavButtonSize.Medium);
        menuButton.Clicked += (_, _) =>
        {
            var menu = new MavMenu()
                .AddItem("Rename", MavIcons.Edit, onSelected: () => MavSnackbar.Show("Rename", anchor: this))
                .AddItem("Duplicate", MavIcons.ContentCopy, onSelected: () => MavSnackbar.Show("Duplicate", anchor: this))
                .AddDivider()
                .AddItem("Delete", MavIcons.Delete, onSelected: () => MavSnackbar.Show("Delete", anchor: this));
            menu.Show(menuButton);
        };

        var popoverButton = new MavButton(
            "Popover",
            MavButtonVariant.Outlined,
            MavIcons.FilterList,
            MavButtonSize.Medium);
        popoverButton.Clicked += (_, _) =>
        {
            var body = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new MavCheckBox("Archived"),
                    new MavCheckBox("Shared with me"),
                    new MavCheckBox("Has attachments"),
                },
            };
            var popover = new MavPopover(body, "Filters");
            popover.Show(popoverButton);
        };

        var searchButton = new MavButton(
            "Search view",
            MavButtonVariant.Outlined,
            MavIcons.Search,
            MavButtonSize.Medium);
        searchButton.Clicked += (_, _) =>
        {
            var search = new MavSearchView { Placeholder = "Search the demo" };
            search.Suggestions.Add(new MavSearchSuggestion("Material menu", "Anchored overlay", MavIcons.Menu));
            search.Suggestions.Add(new MavSearchSuggestion("Date picker", "Adaptive dialog", MavIcons.Event));
            search.Suggestions.Add(new MavSearchSuggestion("Snackbar", "Global FIFO", MavIcons.Notifications));
            search.Show(searchButton);
        };

        var timeButton = new MavButton(
            "Time picker",
            MavButtonVariant.Outlined,
            MavIcons.Schedule,
            MavButtonSize.Medium);
        timeButton.Clicked += (_, _) =>
        {
            var picker = new MavTimePicker { SelectedTime = new TimeSpan(9, 30, 0) };
            picker.TimeSelected += (_, time) =>
                MavSnackbar.Show($"Selected {time:hh\\:mm}", anchor: this);
            picker.Show(this);
        };

        var dateButton = new MavButton(
            "Date picker",
            MavButtonVariant.Outlined,
            MavIcons.CalendarMonth,
            MavButtonSize.Medium);
        dateButton.Clicked += (_, _) =>
        {
            var picker = new MavDatePicker { SelectedDate = DateTime.Today };
            picker.DateSelected += (_, date) =>
                MavSnackbar.Show($"Selected {date:dd MMM yyyy}", anchor: this);
            picker.Show(this);
        };

        var colorButton = new MavButton(
            "Color picker",
            MavButtonVariant.Outlined,
            MavIcons.Palette,
            MavButtonSize.Medium);
        colorButton.Clicked += async (_, _) =>
        {
            var color = await MavColorPicker.PickAsync(colorButton, MavTheme.SeedColor, "Accent color");
            if (color is { } value)
                MavSnackbar.Show($"Accent #{value.R:X2}{value.G:X2}{value.B:X2}", anchor: this);
        };

        var actions = new WrapPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left,
        };
        foreach (var button in new[] { menuButton, popoverButton, searchButton, timeButton, dateButton, colorButton })
        {
            button.Margin = new Thickness(0, 0, 12, 12);
            actions.Children.Add(button);
        }

        Content = PageLayout.Scroll(
            PageLayout.Headline("Transient UI on a phone"),
            PageLayout.Body(
                "Every surface below is drawn by Mav and clamped to the current " +
                "overlay, so it stays usable at 320 dp and in landscape."),
            actions,
            new MavCard(MavCardVariant.Outlined, new MavLabel
            {
                Text = "Use the system Back gesture while a menu or picker is open: " +
                       "the overlay closes first, then the page stack pops.",
                Role = MavTypeRole.BodyMedium,
                TextWrapping = TextWrapping.Wrap,
            }));
    }
}

public sealed class ControlsPage : MavContentPage
{
    private sealed record DemoRow(string Name, string Role, int Score);

    public ControlsPage() : base("Controls")
    {
        Subtitle = "A few Mav controls inside a page";
        AddAction(MavIcons.Save, () => MavSnackbar.Show("Saved", anchor: this));

        var name = new MavTextField(
            "Name",
            MavTextFieldVariant.Outlined,
            placeholder: "Ada Lovelace",
            leadingIcon: MavIcons.Person);
        var email = new MavTextField(
            "Email",
            MavTextFieldVariant.Filled,
            placeholder: "ada@example.com",
            leadingIcon: MavIcons.Email);
        var notes = new MavTextArea("Notes", placeholder: "Write something…");

        var role = new MavSelect("Role", new[] { "Admin", "Editor", "Viewer" })
        {
            SelectedIndex = 0,
        };

        var dueDate = new MavDatePickerField
        {
            Label = "Due date",
            IsClearable = true,
        };

        var notify = new MavSwitch(true, showIcons: true)
        {
            OnIcon = MavIcons.Notifications,
            OffIcon = MavIcons.NotificationsOff,
        };

        var chips = new MavChipGroup(singleSelection: false);
        chips.AddChip("Design", MavChipKind.Filter, MavIcons.Palette, selected: true);
        chips.AddChip("Engineering", MavChipKind.Filter, MavIcons.Code);
        chips.AddChip("Research", MavChipKind.Filter, MavIcons.Science);

        var slider = new MavSlider
        {
            Minimum = 0,
            Maximum = 100,
            Value = 64,
            ShowValueIndicator = true,
        };

        var variants = new StackPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new MavButton("Filled", MavButtonVariant.Filled),
                new MavButton("Tonal", MavButtonVariant.Tonal),
                new MavButton("Elevated", MavButtonVariant.Elevated),
                new MavButton("Outlined", MavButtonVariant.Outlined),
                new MavButton("Text", MavButtonVariant.Text),
            },
        };

        var formCard = new MavCard(
            MavCardVariant.Outlined,
            new StackPanel
            {
                Spacing = 14,
                Children = { name, email, notes, role, dueDate, notify },
            });

        var table = new MavDataTable
        {
            ItemsSource = new[]
            {
                new DemoRow("Ada Lovelace", "Admin", 98),
                new DemoRow("Grace Hopper", "Editor", 95),
                new DemoRow("Alan Turing", "Admin", 92),
                new DemoRow("Katherine Johnson", "Viewer", 90),
                new DemoRow("Linus Torvalds", "Editor", 88),
                new DemoRow("Margaret Hamilton", "Admin", 96),
            },
            ShowPagination = false,
        };
        table.AddColumn("Name", r => ((DemoRow)r).Name);
        table.AddColumn("Role", r => ((DemoRow)r).Role);
        table.AddColumn("Score", r => ((DemoRow)r).Score, isNumeric: true);

        Content = PageLayout.Scroll(
            PageLayout.Headline("Form controls"),
            formCard,
            PageLayout.Headline("Selection"),
            chips,
            slider,
            PageLayout.Headline("Buttons"),
            variants,
            PageLayout.Headline("Data table"),
            table);

        BottomBar = new MavBottomAppBar(MavIcons.Add, () =>
            MavSnackbar.Show("Add from controls page", anchor: this));
    }
}

public sealed class InboxPage : MavContentPage
{
    public InboxPage() : base("Inbox")
    {
        Subtitle = "12 messages";
        AddAction(MavIcons.Search, () => MavSnackbar.Show("Search", anchor: this));
        AddAction(MavIcons.MoreVert, () => MavSnackbar.Show("More", anchor: this));

        var list = new MavList { ShowDividers = true };
        list.AddSubheader("Today");

        for (var i = 1; i <= 12; i++)
        {
            var index = i;
            var subject = $"Message {index}";
            var item = new MavListItem(
                subject,
                supportingText: index % 2 == 0
                    ? "A short supporting line for the list item"
                    : "Tap to open a detail page",
                leadingIcon: index % 2 == 0 ? MavIcons.Mail : MavIcons.Chat,
                trailingIcon: MavIcons.ChevronRight)
            {
                IsInteractive = true,
            };
            item.Clicked += (_, _) => Navigation?.Push(new DetailPage(subject, index));
            list.AddItem(item);
        }

        Content = new MavScrollViewer
        {
            Padding = new Thickness(16, 8, 16, 24),
            Content = list,
        };
    }
}

public sealed class DetailPage : MavContentPage
{
    public DetailPage(string subject, int index) : base(subject)
    {
        Subtitle = $"Message {index} of 12";
        AddAction(MavIcons.Reply, () => MavSnackbar.Show("Reply", anchor: this));
        AddAction(MavIcons.MoreVert, () => MavSnackbar.Show("More", anchor: this));

        Content = PageLayout.Scroll(
            new MavCard(
                MavCardVariant.Elevated,
                new StackPanel
                {
                    Spacing = 12,
                    Children =
                    {
                        new MavLabel { Text = "From: Ada Lovelace", Role = MavTypeRole.TitleMedium },
                        new MavLabel
                        {
                            Text = "This page was pushed onto the custom MavNavigationPage stack. " +
                                   "Press the back arrow in the app bar, or use the bottom bar action.",
                            Role = MavTypeRole.BodyLarge,
                            TextWrapping = TextWrapping.Wrap,
                        },
                        new MavDivider(),
                        new MavLabel
                        {
                            Text = "The header, actions and page content are fully Material. " +
                                   "No Avalonia NavigationPage or ContentPage is involved.",
                            Role = MavTypeRole.BodyMedium,
                            Secondary = true,
                            TextWrapping = TextWrapping.Wrap,
                        },
                    },
                }));

        BottomBar = new MavBottomAppBar(MavIcons.ArrowBack, () => Navigation?.Pop());
    }
}
