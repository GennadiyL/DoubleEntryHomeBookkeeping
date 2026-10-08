using System.Collections.ObjectModel;
using NUnit.Framework;
using WinUi.Controls.TreeGridControl;

namespace UnitTests.WinUi.Controls;

/// <summary>
/// Exercises hierarchy projection without creating a window or dispatcher.
/// Tests keep complete trees separate from their visible row sequences.
/// Nested collapse must preserve expansion and checkbox state.
/// Hidden structural changes must appear when their ancestors reopen.
/// Removed trees must no longer drive projection updates.
/// Rows retain reference identity across changes to their visible positions.
/// Invalid shared or cyclic hierarchies fail before rows are replaced.
/// All cases operate entirely in memory on the local machine.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class TreeGridProjectionTests
{
	[Test]
	public void CollapseAndReopen_PreservesNestedExpansionAndCheckState()
	{
		TreeGridNode leaf = new() { Name = "Leaf", IsChecked = true };
		TreeGridNode closed = new() { IsGroup = true, Children = { new TreeGridNode() } };
		TreeGridNode child = new() { IsGroup = true, IsExpanded = true, Children = { leaf, closed } };
		TreeGridNode root = new() { IsGroup = true, IsExpanded = true, Children = { child } };
		TreeGridProjection projection = new();
		projection.Attach(new ObservableCollection<TreeGridNode> { root });

		Assert.That(projection.VisibleRows, Is.EqualTo(new[] { root, child, leaf, closed }));
		root.IsExpanded = false;
		Assert.That(projection.VisibleRows, Is.EqualTo(new[] { root }));
		root.IsExpanded = true;

		Assert.Multiple(() =>
		{
			Assert.That(projection.VisibleRows, Is.EqualTo(new[] { root, child, leaf, closed }));
			Assert.That(child.IsExpanded, Is.True);
			Assert.That(closed.IsExpanded, Is.False);
			Assert.That(leaf.IsChecked, Is.True);
			Assert.That(leaf.Parent, Is.SameAs(child));
			Assert.That(leaf.Depth, Is.EqualTo(2));
		});
		projection.Detach();
	}

	[Test]
	public void HiddenChildrenAndReorder_AppearInSuppliedOrder()
	{
		TreeGridNode root = new() { IsGroup = true };
		TreeGridProjection projection = new();
		projection.Attach(new ObservableCollection<TreeGridNode> { root });
		TreeGridNode first = new() { Name = "First" };
		TreeGridNode second = new() { Name = "Second" };
		root.Children.Add(first);
		root.Children.Add(second);
		root.Children.Move(1, 0);

		Assert.That(projection.VisibleRows, Is.EqualTo(new[] { root }));
		root.IsExpanded = true;
		Assert.That(projection.VisibleRows, Is.EqualTo(new[] { root, second, first }));
		root.Children.Remove(second);
		Assert.That(projection.VisibleRows, Is.EqualTo(new[] { root, first }));
		projection.Detach();
	}

	[Test]
	public void SwitchingRoots_ReleasesOldSubscriptions()
	{
		TreeGridNode oldRoot = new() { IsGroup = true };
		TreeGridNode newRoot = new() { IsGroup = true, IsExpanded = true };
		TreeGridProjection projection = new();
		projection.Attach(new ObservableCollection<TreeGridNode> { oldRoot });
		projection.Attach(new ObservableCollection<TreeGridNode> { newRoot });
		int changes = 0;
		projection.Changed += (_, _) => changes++;

		oldRoot.IsExpanded = true;
		oldRoot.Children.Add(new TreeGridNode());
		Assert.That(changes, Is.Zero);
		newRoot.Children.Add(new TreeGridNode());
		Assert.That(changes, Is.EqualTo(1));
		projection.Detach();
		newRoot.Children.Add(new TreeGridNode());
		Assert.That(changes, Is.EqualTo(1));
	}

	[Test]
	public void DeepHierarchy_PreservesActualDepth()
	{
		TreeGridNode root = new() { IsGroup = true, IsExpanded = true };
		TreeGridNode last = root;
		for (int i = 0; i < 20; i++)
		{
			TreeGridNode child = new() { IsGroup = true, IsExpanded = true };
			last.Children.Add(child);
			last = child;
		}
		TreeGridProjection projection = new();
		projection.Attach(new ObservableCollection<TreeGridNode> { root });
		Assert.That(last.Depth, Is.EqualTo(20));
		Assert.That(projection.VisibleRows.Count, Is.EqualTo(21));
		projection.Detach();
	}

	[Test]
	public void InvalidHierarchy_RejectsCyclesSharedNodesAndLeafChildren()
	{
		TreeGridProjection projection = new();
		TreeGridNode root = new() { IsGroup = true };
		root.Children.Add(root);
		Assert.Throws<InvalidOperationException>(() => projection.Attach(new ObservableCollection<TreeGridNode> { root }));
		root.Children.Clear();
		Assert.Throws<InvalidOperationException>(() => projection.Attach(new ObservableCollection<TreeGridNode> { root, root }));
		TreeGridNode leaf = new() { Children = { new TreeGridNode() } };
		Assert.Throws<InvalidOperationException>(() => projection.Attach(new ObservableCollection<TreeGridNode> { leaf }));
		projection.Detach();
	}
}
