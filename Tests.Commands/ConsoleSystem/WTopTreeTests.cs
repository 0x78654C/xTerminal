using System.Collections;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Core.SystemTools;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public partial class WTopTests
{
    [Fact]
    public void TreeGroupsChildrenUnderParentsInSiblingSortOrder()
    {
        var ui = new ProcessListingUI(treeView: true);
        Publish(ui, Row(10, "z-root"), Row(100, "a-root"), Row(2, "a-child", parent: 10),
            Row(3, "b-child", parent: 10), Row(4, "grandchild", parent: 2));

        object tree = Call(ui, "SortedProcesses")!;
        Ids(tree).Should().Equal(100, 10, 2, 4, 3);
        Call(ui, "SortedProcesses").Should().BeSameAs(tree);
        Call(ui, "ProcessDisplayName", Row(2, "a-child", parent: 10), 40).Should().Be("├─ ▾ a-child");
        Call(ui, "ProcessDisplayName", Row(4, "grandchild", parent: 2), 40).Should().Be("│  └─   grandchild");
    }

    [Theory]
    [InlineData(ConsoleKey.C)]
    [InlineData(ConsoleKey.M)]
    public void TreeSortsSiblingsWithoutSeparatingParentsAndChildren(ConsoleKey key)
    {
        var ui = new ProcessListingUI(true);
        Publish(ui, Row(10, "root", cpu: 1, memory: 1),
            Row(20, "child", parent: 10, cpu: 10, memory: 10),
            Row(30, "sibling", parent: 10, cpu: 50, memory: 50),
            Row(40, "grandchild", parent: 20, cpu: 99, memory: 99));
        Key(ui, key);
        Ids(Call(ui, "SortedProcesses")!).Should().Equal(10, 30, 20, 40);
    }

    [Fact]
    public void TreeArrowKeysAndSpaceNavigateAndFoldBranches()
    {
        var ui = NestedTree();
        Key(ui, ConsoleKey.RightArrow);
        SelectedId(ui).Should().Be(2);
        Key(ui, ConsoleKey.RightArrow);
        SelectedId(ui).Should().Be(3);
        Key(ui, ConsoleKey.LeftArrow);
        SelectedId(ui).Should().Be(2);
        Key(ui, ConsoleKey.LeftArrow);
        Ids(Call(ui, "SortedProcesses")!).Should().Equal(1, 2, 4);
        Key(ui, ConsoleKey.Spacebar, ' ');
        Ids(Call(ui, "SortedProcesses")!).Should().Equal(1, 2, 3, 4);
        Key(ui, ConsoleKey.LeftArrow);
        Key(ui, ConsoleKey.LeftArrow);
        SelectedId(ui).Should().Be(1);
        Key(ui, ConsoleKey.LeftArrow);
        Ids(Call(ui, "SortedProcesses")!).Should().Equal(1);
        Key(ui, ConsoleKey.RightArrow);
        Ids(Call(ui, "SortedProcesses")!).Should().Equal(1, 2, 4);
        Key(ui, ConsoleKey.RightArrow);
        Key(ui, ConsoleKey.RightArrow);
        SelectedId(ui).Should().Be(2);
        Ids(Call(ui, "SortedProcesses")!).Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public void TreeCollapseSurvivesRefreshAndNewChildrenStayHidden()
    {
        var ui = NestedTree();
        Key(ui, ConsoleKey.LeftArrow);
        Publish(ui, Row(1, "root"), Row(2, "child", parent: 1), Row(5, "new child", parent: 1));
        Ids(Call(ui, "SortedProcesses")!).Should().Equal(1);
        SelectedId(ui).Should().Be(1);
        Key(ui, ConsoleKey.RightArrow);
        Ids(Call(ui, "SortedProcesses")!).Should().Equal(1, 2, 5);
    }

    [Fact]
    public void SwitchingViewsPreservesSelectionAndRevealsHiddenAncestors()
    {
        var ui = NestedTree();
        Key(ui, ConsoleKey.LeftArrow);
        Key(ui, ConsoleKey.T);
        Field<bool>(ui, "_treeView").Should().BeFalse();
        Call(ui, "JumpToProcess", "grandchild", false);
        SelectedId(ui).Should().Be(3);
        Key(ui, ConsoleKey.F5);
        Field<bool>(ui, "_treeView").Should().BeTrue();
        SelectedId(ui).Should().Be(3);
        Ids(Call(ui, "SortedProcesses")!).Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public void TreeSearchRevealsHiddenMatchesAndF3WrapsThroughAllProcesses()
    {
        var ui = new ProcessListingUI(true);
        Publish(ui, Row(1, "alpha"), Row(2, "worker", parent: 1), Row(3, "beta"), Row(4, "workerTwo", parent: 3));
        Call(ui, "SortedProcesses");
        Key(ui, ConsoleKey.LeftArrow);
        Key(ui, ConsoleKey.DownArrow);
        Key(ui, ConsoleKey.LeftArrow);
        Ids(Call(ui, "SortedProcesses")!).Should().Equal(1, 3);

        Call(ui, "JumpToProcess", "worker", false);
        SelectedId(ui).Should().Be(2);
        Ids(Call(ui, "SortedProcesses")!).Should().Equal(1, 2, 3);
        Key(ui, ConsoleKey.F3);
        SelectedId(ui).Should().Be(4);
        Key(ui, ConsoleKey.F3);
        SelectedId(ui).Should().Be(2);
    }

    [Fact]
    public void TreeKeepsOrphansCyclesAndReusedParentPidsVisibleExactlyOnce()
    {
        var ui = new ProcessListingUI(true);
        var olderChild = Row(2, "child", started: 100, parent: 1);
        Publish(ui, Row(1, "new parent", started: 300), olderChild, Row(3, "orphan", parent: 999),
            Row(4, "self", parent: 4), Row(5, "cycleA", parent: 6, started: 0),
            Row(6, "cycleB", parent: 5, started: 0), Row(7, "grandchild", parent: 2, started: 200));
        var ids = Ids(Call(ui, "SortedProcesses")!).ToList();
        ids.Should().HaveCount(7).And.OnlyHaveUniqueItems().And.BeEquivalentTo(Enumerable.Range(1, 7));
        Field<IDictionary>(ui, "_treeParents").Contains(Property<object>(olderChild, "Identity")).Should().BeFalse();
        Call(ui, "ProcessDisplayName", olderChild, 40).Should().Be("▾ child");
    }

    [Fact]
    public void ReusedPidDoesNotInheritCollapsedState()
    {
        var ui = new ProcessListingUI(true);
        Publish(ui, Row(1, "old", started: 100), Row(2, "child", started: 200, parent: 1));
        Call(ui, "SortedProcesses");
        Key(ui, ConsoleKey.LeftArrow);
        Publish(ui, Row(1, "replacement", started: 300), Row(2, "new child", started: 400, parent: 1));
        Ids(Call(ui, "SortedProcesses")!).Should().Equal(1, 2);
    }

    [Fact]
    public void DeepTreeUsesBoundedPrefixesAndKeepsProcessNamesVisible()
    {
        var ui = new ProcessListingUI(true);
        var rows = Enumerable.Range(1, 2000).Select(id => Row(id, "p" + id, started: id, parent: id - 1)).ToArray();
        Publish(ui, rows);
        Ids(Call(ui, "SortedProcesses")!).Should().Equal(Enumerable.Range(1, 2000));
        string name = (string)Call(ui, "ProcessDisplayName", rows[^1], 13)!;
        name.Length.Should().BeLessThanOrEqualTo(13);
        name.Should().Contain("p2000");
    }

    [Theory]
    [InlineData(60)]
    [InlineData(120)]
    public void TreeFrameShowsBranchStateAndTotalCountWhenCollapsed(int width)
    {
        var ui = NestedTree(started: 0);
        Key(ui, ConsoleKey.LeftArrow);
        string frame = (string)Call(ui, "BuildFrame", width, 24)!;
        frame = Regex.Replace(frame, "\x1b\\[[0-9;]*[A-Za-z]", "");
        frame.Should().Contain("Process tree").And.Contain("4 procs").And.Contain("▸ root").And.NotContain("grandchild");
    }

    [Fact]
    public void TreeKillTargetIsTheSelectedProcessAndNotItsParent()
    {
        var ui = NestedTree();
        Call(ui, "JumpToProcess", "3", false);
        var captured = Call(ui, "SelectedProcess")!;
        Property<int>(captured, "Id").Should().Be(3);
        Property<int>(captured, "ParentId").Should().Be(2);
        Publish(ui, Row(3, "replacement", started: 200));
        Call(ui, "SelectedProcess").Should().BeSameAs(captured);
    }

    [Fact]
    public void ParentSnapshotStructMatchesWindowsLayout()
    {
        var entry = typeof(ProcessListingUI).GetNestedType("ProcessEntry", Private)!;
        Marshal.SizeOf(entry).Should().Be(IntPtr.Size == 8 ? 568 : 556);
        Marshal.OffsetOf(entry, "ParentProcessId").ToInt32().Should().Be(IntPtr.Size == 8 ? 32 : 24);
    }

    private static ProcessListingUI NestedTree(long started = 100)
    {
        var ui = new ProcessListingUI(true);
        Publish(ui, Row(1, "root", started: started), Row(2, "child", started: started, parent: 1),
            Row(3, "grandchild", started: started, parent: 2), Row(4, "sibling", started: started, parent: 1));
        Call(ui, "SortedProcesses");
        return ui;
    }
}
