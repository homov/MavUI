using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mav.Controls;
using Xunit;

namespace Mav.Tests;

public class TreeViewTests
{
    private static (Window Window, MavTreeView Tree) CreateHost(MavTreeView tree)
    {
        var scroll = new MavScrollViewer { Height = 260, Content = tree };
        var window = new Window { Width = 420, Height = 300, Content = scroll };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, tree);
    }

    private static HashSet<Mav.Primitives.MavInkSurface> Pills(MavTreeView tree) =>
        tree.GetVisualDescendants()
            .OfType<Mav.Primitives.MavInkSurface>()
            .Where(surface => surface.Content is Grid)
            .ToHashSet();

    private static Mav.Primitives.MavInkSurface PillFor(MavTreeView tree, string text) =>
        Pills(tree).Single(surface => surface
            .GetVisualDescendants()
            .OfType<TextBlock>()
            .Any(block => block.Text == text));

    private static string[] Labels(MavTreeView tree) =>
        tree.GetVisualDescendants()
            .OfType<TextBlock>()
            .Select(block => block.Text)
            .Where(text => !string.IsNullOrEmpty(text))
            .Select(text => text!)
            .ToArray();

    [AvaloniaFact]
    public void Expanding_a_node_reuses_unaffected_rows_and_adds_children()
    {
        var root = new MavTreeNode("Root");
        var child = new MavTreeNode("Child", isExpanded: false);
        child.Add("Grand");
        root.Add(child);

        var tree = new MavTreeView();
        tree.Add(root);
        var (window, _) = CreateHost(tree);

        Assert.Contains("Child", Labels(tree));
        Assert.DoesNotContain("Grand", Labels(tree));
        var rootPill = PillFor(tree, "Root");
        var before = Pills(tree);

        child.IsExpanded = true;
        Dispatcher.UIThread.RunJobs();

        Assert.Contains("Grand", Labels(tree));
        Assert.Same(rootPill, PillFor(tree, "Root"));
        Assert.Contains(rootPill, Pills(tree));
        Assert.Equal(before.Count + 1, Pills(tree).Count);

        window.Close();
    }

    [AvaloniaFact]
    public void Collapsing_a_node_only_removes_its_descendant_rows()
    {
        var root = new MavTreeNode("Root");
        var child = new MavTreeNode("Child", isExpanded: true);
        child.Add("Grand");
        root.Add(child);

        var tree = new MavTreeView();
        tree.Add(root);
        var (window, _) = CreateHost(tree);
        var rootPill = PillFor(tree, "Root");

        child.IsExpanded = false;
        Dispatcher.UIThread.RunJobs();

        Assert.DoesNotContain("Grand", Labels(tree));
        Assert.Same(rootPill, PillFor(tree, "Root"));
        Assert.Single(Pills(tree), pill => ReferenceEquals(pill, rootPill));

        window.Close();
    }

    [AvaloniaFact]
    public void Filtering_keeps_matching_branches_and_reuses_their_rows()
    {
        var roots = Enumerable.Range(0, 20)
            .Select(i =>
            {
                var root = new MavTreeNode($"Folder {i}");
                root.Add(new MavTreeNode($"Leaf {i}"));
                return root;
            })
            .ToList();

        var tree = new MavTreeView();
        foreach (var root in roots)
            tree.Add(root);
        var (window, _) = CreateHost(tree);

        var matchingRoot = PillFor(tree, "Folder 7");
        tree.FilterText = "7";
        Dispatcher.UIThread.RunJobs();

        var labels = Labels(tree);
        Assert.Contains("Folder 7", labels);
        Assert.Contains("Leaf 7", labels);
        Assert.DoesNotContain("Folder 6", labels);
        Assert.Same(matchingRoot, PillFor(tree, "Folder 7"));

        tree.FilterText = string.Empty;
        Dispatcher.UIThread.RunJobs();
        Assert.Contains("Folder 0", Labels(tree));
        Assert.Contains("Leaf 19", Labels(tree));

        window.Close();
    }

    [AvaloniaFact]
    public void Selection_and_text_updates_rebuild_only_affected_rows()
    {
        var roots = Enumerable.Range(0, 10).Select(i => new MavTreeNode($"Node {i}")).ToList();
        var tree = new MavTreeView();
        foreach (var root in roots)
            tree.Add(root);
        var (window, _) = CreateHost(tree);

        var untouched = PillFor(tree, "Node 0");
        var selected = PillFor(tree, "Node 5");
        var before = Pills(tree);

        tree.SelectedNode = roots[5];
        Dispatcher.UIThread.RunJobs();

        var afterSelection = Pills(tree);
        Assert.Same(untouched, PillFor(tree, "Node 0"));
        Assert.Contains(untouched, afterSelection);
        Assert.Equal(before.Count, afterSelection.Count);

        var selectedPill = PillFor(tree, "Node 5");
        roots[2].Text = "Renamed";
        Dispatcher.UIThread.RunJobs();

        Assert.DoesNotContain("Node 2", Labels(tree));
        Assert.Contains("Renamed", Labels(tree));
        Assert.Same(untouched, PillFor(tree, "Node 0"));
        Assert.Same(selectedPill, PillFor(tree, "Node 5"));

        window.Close();
    }

    [AvaloniaFact]
    public void Adding_a_child_after_load_subscribes_to_the_new_subtree()
    {
        var root = new MavTreeNode("Root");
        var tree = new MavTreeView();
        tree.Add(root);
        var (window, _) = CreateHost(tree);

        Assert.DoesNotContain("Child", Labels(tree));

        var child = root.Add("Child");
        Dispatcher.UIThread.RunJobs();
        Assert.Contains("Child", Labels(tree));

        child.Text = "Renamed";
        Dispatcher.UIThread.RunJobs();
        Assert.DoesNotContain("Child", Labels(tree));
        Assert.Contains("Renamed", Labels(tree));

        window.Close();
    }

    [AvaloniaFact]
    public void Selecting_a_node_preserves_the_tree_scroll_offset()
    {
        var tree = new MavTreeView();
        for (var i = 0; i < 200; i++)
            tree.Add($"Node {i}");

        var scroll = new MavScrollViewer { Height = 260, Content = tree };
        var window = new Window { Width = 420, Height = 300, Content = scroll };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        scroll.Offset = new Vector(0, 2000);
        Dispatcher.UIThread.RunJobs();
        var offset = scroll.Offset;

        tree.SelectedNode = tree.Roots[100];
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(offset.Y, scroll.Offset.Y);

        window.Close();
    }
}
