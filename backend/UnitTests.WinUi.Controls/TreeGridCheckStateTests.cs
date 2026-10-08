using System.Collections.ObjectModel;
using NUnit.Framework;
using WinUi.Controls.TreeGridControl;

namespace UnitTests.WinUi.Controls;

/// <summary>
/// Verifies cascading checkbox behavior independently of WinUI rendering.
/// User toggles affect complete subtrees, including hidden descendants.
/// Ancestor checks summarize all immediate children at each level.
/// Mixed and partially selected subtrees propagate the undefined state.
/// Single-child ancestors follow their child instead of becoming partial.
/// Rechecking the last excluded child restores fully checked ancestors.
/// Independent roots and siblings retain their existing selections.
/// These local tests exercise the helper used by mouse and keyboard input.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class TreeGridCheckStateTests
{
	[TestCase(false)]
	[TestCase(true)]
	[TestCase(null)]
	public void ToggleGroup_UpdatesCollapsedDescendants(bool? initial)
	{
		TreeGridNode leaf = new() { IsChecked = true };
		TreeGridNode group = new() { IsGroup = true, IsChecked = null, Children = { leaf } };
		TreeGridNode root = new() { IsGroup = true, IsChecked = initial, Children = { group } };

		TreeGridCheckState.Toggle(root);

		Assert.Multiple(() =>
		{
			Assert.That(root.IsChecked, Is.EqualTo(initial != true));
			Assert.That(group.IsChecked, Is.EqualTo(initial != true));
			Assert.That(leaf.IsChecked, Is.EqualTo(initial != true));
			Assert.That(group.IsExpanded, Is.False);
		});
	}

	[Test]
	public void UncheckChild_MakesAllAncestorsPartial_AndRecheckRestoresThem()
	{
		TreeGridNode first = new() { IsChecked = true };
		TreeGridNode second = new() { IsChecked = true };
		TreeGridNode group = new() { IsGroup = true, IsChecked = true, Children = { first, second } };
		TreeGridNode root = new() { IsGroup = true, IsChecked = true, Children = { group } };
		TreeGridNode independent = new() { IsChecked = true };
		TreeGridProjection projection = new();
		projection.Attach(new ObservableCollection<TreeGridNode> { root, independent });

		TreeGridCheckState.Toggle(first);
		Assert.Multiple(() =>
		{
			Assert.That(first.IsChecked, Is.False);
			Assert.That(second.IsChecked, Is.True);
			Assert.That(group.IsChecked, Is.Null);
			Assert.That(root.IsChecked, Is.Null);
			Assert.That(independent.IsChecked, Is.True);
		});
		TreeGridCheckState.Toggle(first);
		Assert.That(group.IsChecked, Is.True);
		Assert.That(root.IsChecked, Is.True);
		projection.Detach();
	}

	[Test]
	public void UncheckOnlyChild_UnchecksEntireAncestorChain()
	{
		TreeGridNode leaf = new() { IsChecked = true };
		TreeGridNode group = new() { IsGroup = true, IsChecked = true, Children = { leaf } };
		TreeGridNode root = new() { IsGroup = true, IsChecked = true, Children = { group } };
		TreeGridProjection projection = new();
		projection.Attach(new ObservableCollection<TreeGridNode> { root });

		TreeGridCheckState.Toggle(leaf);

		Assert.That(new[] { leaf.IsChecked, group.IsChecked, root.IsChecked }, Is.All.False);
		projection.Detach();
	}

	[Test]
	public void UncheckLastCheckedChild_UnchecksParentWithSeveralChildren()
	{
		TreeGridNode first = new() { IsChecked = true };
		TreeGridNode second = new();
		TreeGridNode root = new() { IsGroup = true, IsChecked = null, Children = { first, second } };
		TreeGridProjection projection = new();
		projection.Attach(new ObservableCollection<TreeGridNode> { root });

		TreeGridCheckState.Toggle(first);

		Assert.That(root.IsChecked, Is.False);
		Assert.That(second.IsChecked, Is.False);
		projection.Detach();
	}

	[Test]
	public void TogglePartialGroup_ChecksWholeSubtree_WithoutChangingSibling()
	{
		TreeGridNode first = new() { IsChecked = true };
		TreeGridNode second = new();
		TreeGridNode group = new() { IsGroup = true, IsChecked = null, Children = { first, second } };
		TreeGridNode sibling = new();
		TreeGridNode root = new() { IsGroup = true, IsChecked = null, Children = { group, sibling } };
		TreeGridProjection projection = new();
		projection.Attach(new ObservableCollection<TreeGridNode> { root });

		TreeGridCheckState.Toggle(group);
		Assert.That(new[] { group.IsChecked, first.IsChecked, second.IsChecked }, Is.All.True);
		Assert.That(sibling.IsChecked, Is.False);
		Assert.That(root.IsChecked, Is.Null);

		TreeGridCheckState.Toggle(group);
		Assert.That(new[] { group.IsChecked, first.IsChecked, second.IsChecked, root.IsChecked }, Is.All.False);
		projection.Detach();
	}
}
