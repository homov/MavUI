using Avalonia.Controls;
using Avalonia;
using Avalonia.Layout;
using Mav;
using Mav.Controls;
using Mav.Theming;

namespace Mav.Gallery;

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
        var status = new MavLabel
        {
            Text = "Swipe a row left or right on touch, or drag with the pointer.",
            Role = MavTypeRole.BodySmall,
            Secondary = true,
        };

        var list = new MavList { ShowDividers = true };
        list.AddSubheader("Today");
        list.AddItem(new MavListItem(
            "Design review",
            "10:30 – 11:15",
            MavIcons.Event,
            MavIcons.ChevronRight));
        list.AddItem(new MavListItem(
            "Release planning",
            "12:00 – 13:00",
            MavIcons.Groups,
            trailingText: "2h"));

        var swipe = new MavListItem(
            "Swipe me",
            "Leading archive / trailing delete",
            MavIcons.Swipe,
            MavIcons.ChevronRight)
        {
            LeadingSwipeAction = new MavSwipeAction(
                "Archive", MavIcons.Archive, () => status.Text = "Leading action triggered: Archive"),
            TrailingSwipeAction = new MavSwipeAction(
                "Delete", MavIcons.Delete, () => status.Text = "Trailing action triggered: Delete"),
        };
        list.AddItem(swipe);

        list.AddDivider();
        list.AddItem(new MavListItem("Selected row", "IsSelected + IsInteractive", MavIcons.CheckCircle)
        {
            IsSelected = true,
            IsInteractive = true,
        });

        return GalleryUi.Showcase(
            "MavList, MavListItem, MavListSubheader and MavSwipeAction",
            "A vertical list with dividers, two-line rows, trailing text, selection and swipe actions.",
            GalleryUi.Column(
                new MavCard(MavCardVariant.Outlined, list),
                status),
            "list.AddItem(new MavListItem(...)) · ShowDividers · LeadingSwipeAction/TrailingSwipeAction");
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

        var table = new MavDataTable
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
            "MavDataTable and MavDataColumn",
            "Sort by clicking a header, filter with FilterText, select rows and page through data. Under 600 dp the same table becomes cards.",
            table,
            "table.AddColumn(header, valueSelector, isNumeric) · Layout=Auto · ShowPagination · FilterText");
    }

    private static Control TreeAndTimeline()
    {
        var tree = new MavTreeView
        {
            ShowCheckBoxes = true,
            CascadeCheckBoxes = true,
        };
        var root = tree.Add("Mav");
        root.Add("Controls");
        root.Add("Theming");
        root.Add("Demo");
        var demos = tree.Add("Demos");
        demos.Add("Desktop");
        demos.Add("Browser");
        demos.Add("Android");

        var timeline = new MavTimeline();
        timeline.Add(new MavTimelineItem("Created", "Initial page stack", "09:00", MavIcons.AddCircle, MavTimelineStatus.Completed));
        timeline.Add(new MavTimelineItem("Reviewed", "Overlay + accessibility pass", "11:30", MavIcons.RateReview, MavTimelineStatus.Completed));
        timeline.Add(new MavTimelineItem("Shipped", "NuGet package", "14:00", MavIcons.Publish, MavTimelineStatus.Active));
        timeline.Add(new MavTimelineItem("Next", "Write more tests", "—", MavIcons.Pending, MavTimelineStatus.Pending));

        return GalleryUi.Showcase(
            "MavTreeView, MavTreeNode and MavTimeline",
            "Expandable tree with cascading checkboxes and a vertical timeline with statuses.",
            GalleryUi.Row(
                new MavCard(MavCardVariant.Outlined, tree) { Width = 320 },
                new MavCard(MavCardVariant.Outlined, timeline) { Width = 420 }),
            "MavTreeView.ShowCheckBoxes · MavTreeNode.Add(...) · MavTimeline.Add(...)");
    }
}
