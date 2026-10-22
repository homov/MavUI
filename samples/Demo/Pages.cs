using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Demo;

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

        return new MdScrollViewer
        {
            Padding = new Thickness(24),
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = stack,
        };
    }

    public static MdLabel Headline(string text) => new()
    {
        Text = text,
        Role = MdTypeRole.HeadlineMedium,
        TextWrapping = TextWrapping.Wrap,
    };

    public static MdLabel Body(string text) => new()
    {
        Text = text,
        Role = MdTypeRole.BodyLarge,
        Secondary = true,
        TextWrapping = TextWrapping.Wrap,
    };
}

public sealed class HomePage : MdContentPage
{
    public HomePage() : base("Md3 Navigation")
    {
        Subtitle = "Custom stack built from Material controls";
        AddAction(MdIcons.DarkMode, ToggleTheme);

        var openControls = new MdButton(
            "Open controls",
            MdButtonVariant.Filled,
            MdIcons.Tune,
            MdButtonSize.Medium);
        openControls.Clicked += (_, _) => Navigation?.Push(new ControlsPage());

        var openList = new MdButton(
            "Open list",
            MdButtonVariant.Tonal,
            MdIcons.Inbox,
            MdButtonSize.Medium);
        openList.Clicked += (_, _) => Navigation?.Push(new InboxPage());

        var openOverlays = new MdButton(
            "Overlays & pickers",
            MdButtonVariant.Tonal,
            MdIcons.Layers,
            MdButtonSize.Medium);
        openOverlays.Clicked += (_, _) => Navigation?.Push(new OverlaysPage());

        var showDialog = new MdButton(
            "Show dialog",
            MdButtonVariant.Outlined,
            MdIcons.Chat,
            MdButtonSize.Medium);
        showDialog.Clicked += (_, _) => ShowDialog();

        var showSheet = new MdButton(
            "Show sheet",
            MdButtonVariant.Outlined,
            MdIcons.MenuOpen,
            MdButtonSize.Medium);
        showSheet.Clicked += (_, _) => ShowSheet();

        var showSnackbar = new MdButton(
            "Show snackbar",
            MdButtonVariant.Text,
            MdIcons.Notifications,
            MdButtonSize.Medium);
        showSnackbar.Clicked += (_, _) =>
            MdSnackbar.Show("Saved with MdSnackbar", "Undo", () => { }, anchor: this);

        var darkMode = new MdSwitch(MdTheme.Current.IsDark, showIcons: true)
        {
            OnIcon = MdIcons.DarkMode,
            OffIcon = MdIcons.LightMode,
        };
        darkMode.Toggled += (_, on) =>
            MdTheme.Mode = on ? MdThemeMode.Dark : MdThemeMode.Light;

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
                    "This demo uses MdNavigationPage. The stack, app bar, actions, " +
                    "bottom bar and floating action button are all Md3 controls."),
            },
        };

        var hero = new MdCard(MdCardVariant.Filled, heroText);

        var themeGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
        };
        var themeIcon = new MdIcon(MdIcons.Palette, 24)
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
                new MdLabel { Text = "Dark theme", Role = MdTypeRole.TitleMedium },
                new MdLabel
                {
                    Text = "Toggle the global MdTheme palette",
                    Role = MdTypeRole.BodyMedium,
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

        var themeCard = new MdCard(MdCardVariant.Outlined, themeGrid);

        Content = PageLayout.Scroll(
            hero,
            new MdLabel { Text = "Try the stack", Role = MdTypeRole.TitleLarge },
            actions,
            themeCard,
            new MdLabel
            {
                Text = "The app bar back arrow is supplied by MdNavigationPage. " +
                       "The page below keeps its own scroll position and state.",
                Role = MdTypeRole.BodyMedium,
                Secondary = true,
                TextWrapping = TextWrapping.Wrap,
            });

        BottomBar = BuildBottomBar();
    }

    private void ToggleTheme()
    {
        MdTheme.ToggleDark();
        MdSnackbar.Show(
            MdTheme.Current.IsDark ? "Dark theme enabled" : "Light theme enabled",
            anchor: this);
    }

    private void ShowDialog()
    {
        var dialog = new MdDialog
        {
            Icon = MdIcons.Info,
            Title = "Material dialog",
            Message = "MdDialog is displayed in the overlay, above the custom navigation stack.",
            ConfirmText = "Continue",
            DismissText = "Cancel",
        };
        dialog.Confirmed += (_, _) => MdSnackbar.Show("Dialog confirmed", anchor: this);
        dialog.Show(this);
    }

    private void ShowSheet()
    {
        var sheet = new MdBottomSheet
        {
            Title = "Quick actions",
            Body = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new MdButton("Create item", MdButtonVariant.Filled, MdIcons.Add),
                    new MdButton("Archive", MdButtonVariant.Text, MdIcons.Archive),
                    new MdButton("Delete", MdButtonVariant.Text, MdIcons.Delete),
                },
            },
        };
        sheet.Show(this);
    }

    private static MdBottomAppBar BuildBottomBar()
    {
        var bar = new MdBottomAppBar(MdIcons.Add, () =>
            MdSnackbar.Show("Bottom app bar FAB clicked"));
        bar.AddAction(MdIcons.Search, () => MdSnackbar.Show("Search action"));
        bar.AddAction(MdIcons.Settings, () => MdSnackbar.Show("Settings action"));
        return bar;
    }
}

public sealed class OverlaysPage : MdContentPage
{
    public OverlaysPage() : base("Overlays & pickers")
    {
        Subtitle = "Adaptive menus, popovers, search, time and color pickers";

        var menuButton = new MdButton(
            "Menu",
            MdButtonVariant.Outlined,
            MdIcons.Menu,
            MdButtonSize.Medium);
        menuButton.Clicked += (_, _) =>
        {
            var menu = new MdMenu()
                .AddItem("Rename", MdIcons.Edit, onSelected: () => MdSnackbar.Show("Rename", anchor: this))
                .AddItem("Duplicate", MdIcons.ContentCopy, onSelected: () => MdSnackbar.Show("Duplicate", anchor: this))
                .AddDivider()
                .AddItem("Delete", MdIcons.Delete, onSelected: () => MdSnackbar.Show("Delete", anchor: this));
            menu.Show(menuButton);
        };

        var popoverButton = new MdButton(
            "Popover",
            MdButtonVariant.Outlined,
            MdIcons.FilterList,
            MdButtonSize.Medium);
        popoverButton.Clicked += (_, _) =>
        {
            var body = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new MdCheckBox("Archived"),
                    new MdCheckBox("Shared with me"),
                    new MdCheckBox("Has attachments"),
                },
            };
            var popover = new MdPopover(body, "Filters");
            popover.Show(popoverButton);
        };

        var searchButton = new MdButton(
            "Search view",
            MdButtonVariant.Outlined,
            MdIcons.Search,
            MdButtonSize.Medium);
        searchButton.Clicked += (_, _) =>
        {
            var search = new MdSearchView { Placeholder = "Search the demo" };
            search.Suggestions.Add(new MdSearchSuggestion("Material menu", "Anchored overlay", MdIcons.Menu));
            search.Suggestions.Add(new MdSearchSuggestion("Date picker", "Adaptive dialog", MdIcons.Event));
            search.Suggestions.Add(new MdSearchSuggestion("Snackbar", "Global FIFO", MdIcons.Notifications));
            search.Show(searchButton);
        };

        var timeButton = new MdButton(
            "Time picker",
            MdButtonVariant.Outlined,
            MdIcons.Schedule,
            MdButtonSize.Medium);
        timeButton.Clicked += (_, _) =>
        {
            var picker = new MdTimePicker { SelectedTime = new TimeSpan(9, 30, 0) };
            picker.TimeSelected += (_, time) =>
                MdSnackbar.Show($"Selected {time:hh\\:mm}", anchor: this);
            picker.Show(this);
        };

        var dateButton = new MdButton(
            "Date picker",
            MdButtonVariant.Outlined,
            MdIcons.CalendarMonth,
            MdButtonSize.Medium);
        dateButton.Clicked += (_, _) =>
        {
            var picker = new MdDatePicker { SelectedDate = DateTime.Today };
            picker.DateSelected += (_, date) =>
                MdSnackbar.Show($"Selected {date:dd MMM yyyy}", anchor: this);
            picker.Show(this);
        };

        var colorButton = new MdButton(
            "Color picker",
            MdButtonVariant.Outlined,
            MdIcons.Palette,
            MdButtonSize.Medium);
        colorButton.Clicked += async (_, _) =>
        {
            var color = await MdColorPicker.PickAsync(colorButton, MdTheme.SeedColor, "Accent color");
            if (color is { } value)
                MdSnackbar.Show($"Accent #{value.R:X2}{value.G:X2}{value.B:X2}", anchor: this);
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
                "Every surface below is drawn by Md3.Avalonia and clamped to the current " +
                "overlay, so it stays usable at 320 dp and in landscape."),
            actions,
            new MdCard(MdCardVariant.Outlined, new MdLabel
            {
                Text = "Use the system Back gesture while a menu or picker is open: " +
                       "the overlay closes first, then the page stack pops.",
                Role = MdTypeRole.BodyMedium,
                TextWrapping = TextWrapping.Wrap,
            }));
    }
}

public sealed class ControlsPage : MdContentPage
{
    private sealed record DemoRow(string Name, string Role, int Score);

    public ControlsPage() : base("Controls")
    {
        Subtitle = "A few Md3 controls inside a page";
        AddAction(MdIcons.Save, () => MdSnackbar.Show("Saved", anchor: this));

        var name = new MdTextField(
            "Name",
            MdTextFieldVariant.Outlined,
            placeholder: "Ada Lovelace",
            leadingIcon: MdIcons.Person);
        var email = new MdTextField(
            "Email",
            MdTextFieldVariant.Filled,
            placeholder: "ada@example.com",
            leadingIcon: MdIcons.Email);
        var notes = new MdTextArea("Notes", placeholder: "Write something…");

        var role = new MdSelect("Role", new[] { "Admin", "Editor", "Viewer" })
        {
            SelectedIndex = 0,
        };

        var dueDate = new MdDatePickerField
        {
            Label = "Due date",
            IsClearable = true,
        };

        var notify = new MdSwitch(true, showIcons: true)
        {
            OnIcon = MdIcons.Notifications,
            OffIcon = MdIcons.NotificationsOff,
        };

        var chips = new MdChipGroup(singleSelection: false);
        chips.AddChip("Design", MdChipKind.Filter, MdIcons.Palette, selected: true);
        chips.AddChip("Engineering", MdChipKind.Filter, MdIcons.Code);
        chips.AddChip("Research", MdChipKind.Filter, MdIcons.Science);

        var slider = new MdSlider
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
                new MdButton("Filled", MdButtonVariant.Filled),
                new MdButton("Tonal", MdButtonVariant.Tonal),
                new MdButton("Elevated", MdButtonVariant.Elevated),
                new MdButton("Outlined", MdButtonVariant.Outlined),
                new MdButton("Text", MdButtonVariant.Text),
            },
        };

        var formCard = new MdCard(
            MdCardVariant.Outlined,
            new StackPanel
            {
                Spacing = 14,
                Children = { name, email, notes, role, dueDate, notify },
            });

        var table = new MdDataTable
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

        BottomBar = new MdBottomAppBar(MdIcons.Add, () =>
            MdSnackbar.Show("Add from controls page", anchor: this));
    }
}

public sealed class InboxPage : MdContentPage
{
    public InboxPage() : base("Inbox")
    {
        Subtitle = "12 messages";
        AddAction(MdIcons.Search, () => MdSnackbar.Show("Search", anchor: this));
        AddAction(MdIcons.MoreVert, () => MdSnackbar.Show("More", anchor: this));

        var list = new MdList { ShowDividers = true };
        list.AddSubheader("Today");

        for (var i = 1; i <= 12; i++)
        {
            var index = i;
            var subject = $"Message {index}";
            var item = new MdListItem(
                subject,
                supportingText: index % 2 == 0
                    ? "A short supporting line for the list item"
                    : "Tap to open a detail page",
                leadingIcon: index % 2 == 0 ? MdIcons.Mail : MdIcons.Chat,
                trailingIcon: MdIcons.ChevronRight)
            {
                IsInteractive = true,
            };
            item.Clicked += (_, _) => Navigation?.Push(new DetailPage(subject, index));
            list.AddItem(item);
        }

        Content = new MdScrollViewer
        {
            Padding = new Thickness(16, 8, 16, 24),
            Content = list,
        };
    }
}

public sealed class DetailPage : MdContentPage
{
    public DetailPage(string subject, int index) : base(subject)
    {
        Subtitle = $"Message {index} of 12";
        AddAction(MdIcons.Reply, () => MdSnackbar.Show("Reply", anchor: this));
        AddAction(MdIcons.MoreVert, () => MdSnackbar.Show("More", anchor: this));

        Content = PageLayout.Scroll(
            new MdCard(
                MdCardVariant.Elevated,
                new StackPanel
                {
                    Spacing = 12,
                    Children =
                    {
                        new MdLabel { Text = "From: Ada Lovelace", Role = MdTypeRole.TitleMedium },
                        new MdLabel
                        {
                            Text = "This page was pushed onto the custom MdNavigationPage stack. " +
                                   "Press the back arrow in the app bar, or use the bottom bar action.",
                            Role = MdTypeRole.BodyLarge,
                            TextWrapping = TextWrapping.Wrap,
                        },
                        new MdDivider(),
                        new MdLabel
                        {
                            Text = "The header, actions and page content are fully Material. " +
                                   "No Avalonia NavigationPage or ContentPage is involved.",
                            Role = MdTypeRole.BodyMedium,
                            Secondary = true,
                            TextWrapping = TextWrapping.Wrap,
                        },
                    },
                }));

        BottomBar = new MdBottomAppBar(MdIcons.ArrowBack, () => Navigation?.Pop());
    }
}
