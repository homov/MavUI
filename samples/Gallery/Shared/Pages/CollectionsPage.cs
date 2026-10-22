using Avalonia.Controls;
using Avalonia;
using Avalonia.Layout;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Theming;

namespace Md3.Gallery;

internal static class CollectionsPage
{
    private sealed record Person(string Name, string Role, int Score);

    public static Control Build()
    {
        return GalleryUi.Page(
            GalleryUi.Section("Lists", "Subheaders, dividers, leading/trailing content and swipe actions.", Lists()),
            GalleryUi.Section("Data table", "Sorting, filtering, selection and pagination; narrow hosts automatically switch to cards.", Table()),
            GalleryUi.Section("Trees and timelines", "Hierarchical data with checkboxes and a vertical activity timeline.", TreeAndTimeline()));
    }

    private static Control Lists()
    {
        var status = new MdLabel
        {
            Text = "Swipe a row left or right on touch, or drag with the pointer.",
            Role = MdTypeRole.BodySmall,
            Secondary = true,
        };

        var list = new MdList { ShowDividers = true };
        list.AddSubheader("Today");
        list.AddItem(new MdListItem(
            "Design review",
            "10:30 – 11:15",
            MdIcons.Event,
            MdIcons.ChevronRight));
        list.AddItem(new MdListItem(
            "Release planning",
            "12:00 – 13:00",
            MdIcons.Groups,
            trailingText: "2h"));

        var swipe = new MdListItem(
            "Swipe me",
            "Leading archive / trailing delete",
            MdIcons.Swipe,
            MdIcons.ChevronRight)
        {
            LeadingSwipeAction = new MdSwipeAction(
                "Archive", MdIcons.Archive, () => status.Text = "Leading action triggered: Archive"),
            TrailingSwipeAction = new MdSwipeAction(
                "Delete", MdIcons.Delete, () => status.Text = "Trailing action triggered: Delete"),
        };
        list.AddItem(swipe);

        list.AddDivider();
        list.AddItem(new MdListItem("Selected row", "IsSelected + IsInteractive", MdIcons.CheckCircle)
        {
            IsSelected = true,
            IsInteractive = true,
        });

        return GalleryUi.Showcase(
            "MdList, MdListItem, MdListSubheader and MdSwipeAction",
            "A vertical list with dividers, two-line rows, trailing text, selection and swipe actions.",
            GalleryUi.Column(
                new MdCard(MdCardVariant.Outlined, list),
                status),
            "list.AddItem(new MdListItem(...)) · ShowDividers · LeadingSwipeAction/TrailingSwipeAction");
    }

    private static Control Table()
    {
        var people = new[]
        {
            new Person("Ada Lovelace", "Admin", 98),
            new Person("Grace Hopper", "Editor", 95),
            new Person("Alan Turing", "Admin", 92),
            new Person("Katherine Johnson", "Viewer", 90),
            new Person("Linus Torvalds", "Editor", 88),
            new Person("Margaret Hamilton", "Admin", 96),
            new Person("Barbara Liskov", "Viewer", 94),
        };

        var table = new MdDataTable
        {
            ItemsSource = people,
            IsSelectable = true,
            ShowPagination = true,
            PageSize = 4,
            AlternatingRowBackground = true,
        };
        table.AddColumn("Name", r => ((Person)r).Name);
        table.AddColumn("Role", r => ((Person)r).Role);
        table.AddColumn("Score", r => ((Person)r).Score, isNumeric: true);

        return GalleryUi.Showcase(
            "MdDataTable and MdDataColumn",
            "Sort by clicking a header, filter with FilterText, select rows and page through data. Under 600 dp the same table becomes cards.",
            table,
            "table.AddColumn(header, valueSelector, isNumeric) · Layout=Auto · ShowPagination · FilterText");
    }

    private static Control TreeAndTimeline()
    {
        var tree = new MdTreeView
        {
            ShowCheckBoxes = true,
            CascadeCheckBoxes = true,
        };
        var root = tree.Add("Md3.Avalonia");
        root.Add("Controls");
        root.Add("Theming");
        root.Add("Demo");
        var demos = tree.Add("Demos");
        demos.Add("Desktop");
        demos.Add("Browser");
        demos.Add("Android");

        var timeline = new MdTimeline();
        timeline.Add(new MdTimelineItem("Created", "Initial page stack", "09:00", MdIcons.AddCircle, MdTimelineStatus.Completed));
        timeline.Add(new MdTimelineItem("Reviewed", "Overlay + accessibility pass", "11:30", MdIcons.RateReview, MdTimelineStatus.Completed));
        timeline.Add(new MdTimelineItem("Shipped", "NuGet package", "14:00", MdIcons.Publish, MdTimelineStatus.Active));
        timeline.Add(new MdTimelineItem("Next", "Write more tests", "—", MdIcons.Pending, MdTimelineStatus.Pending));

        return GalleryUi.Showcase(
            "MdTreeView, MdTreeNode and MdTimeline",
            "Expandable tree with cascading checkboxes and a vertical timeline with statuses.",
            GalleryUi.Row(
                new MdCard(MdCardVariant.Outlined, tree) { Width = 320 },
                new MdCard(MdCardVariant.Outlined, timeline) { Width = 420 }),
            "MdTreeView.ShowCheckBoxes · MdTreeNode.Add(...) · MdTimeline.Add(...)");
    }
}
