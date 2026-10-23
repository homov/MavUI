using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Theming;
using static Mav.ControlsGalleryMarkup.Pages.PageParts;

namespace Mav.ControlsGalleryMarkup.Pages;

/// <summary>Lists, a data table, trees and timelines. Mirrors the AXAML Collections page.</summary>
public static class CollectionsPage
{
    public static Control Build() =>
        Page(
            Section(
                "Collections.Lists.Title", "Lists",
                "Collections.Lists.Body", "Subheaders, dividers, leading/trailing content and swipe actions.",
                Card(
                    "MavList, MavListItem, MavListSubheader and MavSwipeAction",
                    "Collections.Lists.CardBody", "A vertical list with dividers, two-line rows, trailing text, selection and swipe actions.",
                    "list.AddItem(new MavListItem(...)) · ShowDividers · LeadingSwipeAction/TrailingSwipeAction",
                    ListDemo())),
            Section(
                "Collections.Table.Title", "Data table",
                "Collections.Table.Body", "Sorting, filtering, selection and pagination; narrow hosts automatically switch to cards.",
                Card(
                    "MavDataTable and MavDataColumn",
                    "Collections.Table.CardBody", "Sort by clicking a header, filter with FilterText, select rows and page through data. Under 600 dp the same table becomes cards.",
                    "table.AddColumn(header, valueSelector, isNumeric) · Layout=Auto · ShowPagination · FilterText",
                    new MavDataTable()
                        .IsSelectable(true)
                        .ShowPagination(true)
                        .PageSize(4)
                        .AlternatingRowBackground(true)
                        .AddColumn("Name", row => ((PersonRow)row).Name)
                        .AddColumn("Role", row => ((PersonRow)row).Role)
                        .AddColumn("Score", row => ((PersonRow)row).Score, true)
                        .ItemsSource(People()))),
            Section(
                "Collections.Trees.Title", "Trees and timelines",
                "Collections.Trees.Body", "Hierarchical data with checkboxes and a vertical activity timeline.",
                Card(
                    "MavTreeView, MavTreeNode and MavTimeline",
                    "Collections.Tree.CardBody", "Expandable tree with cascading checkboxes and a vertical timeline with statuses.",
                    "MavTreeView.ShowCheckBoxes · MavTreeNode.Add(...) · MavTimeline.Add(...)",
                    Adaptive(300,
                        new MavCard(MavCardVariant.Outlined, Tree(
                                new MavTreeNode("Mav")
                                    .Add(new MavTreeNode("Controls"))
                                    .Add(new MavTreeNode("Theming"))
                                    .Add(new MavTreeNode("Demo")),
                                new MavTreeNode("Demos")
                                    .Add(new MavTreeNode("Desktop"))
                                    .Add(new MavTreeNode("Browser"))
                                    .Add(new MavTreeNode("Android"))))
                            .Margin(new Thickness(0, 0, 8, 8)),
                        new MavCard(MavCardVariant.Outlined, Timeline(
                                new MavTimelineItem("Created", "Initial page stack", "09:00", MavIcons.AddCircle, MavTimelineStatus.Completed),
                                new MavTimelineItem("Reviewed", "Overlay + accessibility pass", "11:30", MavIcons.RateReview, MavTimelineStatus.Completed),
                                new MavTimelineItem("Shipped", "NuGet package", "14:00", MavIcons.Publish, MavTimelineStatus.Active),
                                new MavTimelineItem("Next", "Write more tests", "—", MavIcons.Pending, MavTimelineStatus.Pending)))
                            .Margin(new Thickness(0, 0, 8, 8))))));

    /// <summary>The list with a swipe-enabled row. The swipe actions update the status line below it.</summary>
    private static Control ListDemo()
    {
        var status = new MavLabel(
                L("Collections.Lists.Hint", "Swipe a row left or right on touch, or drag with the pointer."),
                MavTypeRole.BodySmall)
            .Secondary(true)
            .TextWrapping(TextWrapping.Wrap);

        var swipeRow = new MavListItem("Swipe me", "Leading archive / trailing delete", MavIcons.Swipe, MavIcons.ChevronRight)
            .LeadingSwipeAction(new MavSwipeAction("Archive", MavIcons.Archive,
                () => status.Text = "Leading action triggered: Archive"))
            .TrailingSwipeAction(new MavSwipeAction("Delete", MavIcons.Delete,
                () => status.Text = "Trailing action triggered: Delete"));

        var list = new MavList()
            .ShowDividers(true)
            .AddSubheader("Today")
            .AddItem(new MavListItem("Design review", "10:30 – 11:15", MavIcons.Event, MavIcons.ChevronRight))
            .AddItem(new MavListItem("Release planning", "12:00 – 13:00", MavIcons.Groups, null, "2h"))
            .AddItem(swipeRow)
            .AddDivider()
            .AddItem(new MavListItem("Selected row", "IsSelected + IsInteractive", MavIcons.CheckCircle)
                .IsSelected(true)
                .IsInteractive(true));

        return Stack(8,
            new MavCard(MavCardVariant.Outlined, list),
            status);
    }

    private static PersonRow[] People() =>
    [
        new("Ada Lovelace", "Admin", 98),
        new("Grace Hopper", "Editor", 95),
        new("Alan Turing", "Admin", 92),
        new("Katherine Johnson", "Viewer", 90),
        new("Linus Torvalds", "Editor", 88),
        new("Margaret Hamilton", "Admin", 96),
        new("Barbara Liskov", "Viewer", 94),
    ];

    /// <summary>MavTreeView.Add returns nothing, so the roots are added in a loop.</summary>
    private static MavTreeView Tree(params MavTreeNode[] roots)
    {
        var view = new MavTreeView().ShowCheckBoxes(true).CascadeCheckBoxes(true);
        foreach (var root in roots)
            view.Add(root);
        return view;
    }

    /// <summary>MavTimeline.Add returns nothing, so the items are added in a loop.</summary>
    private static MavTimeline Timeline(params MavTimelineItem[] items)
    {
        var timeline = new MavTimeline();
        foreach (var item in items)
            timeline.Add(item);
        return timeline;
    }

    private static StackPanel Stack(double spacing, params Control[] items) =>
        new StackPanel().Spacing(spacing).Children(items);
}
