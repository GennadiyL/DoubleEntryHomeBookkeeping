using System.Collections.ObjectModel;
using NUnit.Framework;
using WinUi.Controls.TreeGridControl;

namespace UnitTests.WinUi.Controls;

/// <summary>
/// Exercises drop planning without a window or native drag loop.
/// Reorder indices refer to complete same-kind sibling collections.
/// Move and merge requests retain their source and destination identities.
/// Tests protect roots, hierarchy cycles and popup merge restrictions.
/// Filtered rows cannot accidentally erase hidden siblings from the order.
/// Planning never mutates models before a host has saved successfully.
/// No-op drops and invalid target regions are rejected explicitly.
/// Every case uses local in-memory UI nodes and the real projection.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class TreeGridDropPlannerTests
{
	private TreeGridProjection _projection = null!;
	private TreeGridNode _root = null!;
	private TreeGridNode _group = null!;
	private TreeGridNode _other = null!;
	private TreeGridNode _first = null!;
	private TreeGridNode _hidden = null!;
	private TreeGridNode _last = null!;

	[SetUp]
	public void SetUp()
	{
		_first = new TreeGridNode { Name = "First" };
		_hidden = new TreeGridNode { Name = "Hidden" };
		_last = new TreeGridNode { Name = "Last" };
		_group = new TreeGridNode { Name = "Group", IsGroup = true, IsExpanded = true, Children = { _first, _hidden, _last } };
		_other = new TreeGridNode { Name = "Other", IsGroup = true };
		_root = new TreeGridNode { Name = "Root", IsGroup = true, IsExpanded = true, Children = { _group, _other } };
		_projection = new TreeGridProjection();
		_projection.Attach(new ObservableCollection<TreeGridNode> { _root });
	}

	[TearDown]
	public void TearDown() => _projection.Detach();

	[Test]
	public void ReorderAfter_UsesFinalIndexWithoutSource()
	{
		TreeGridDropRequest request = Plan(_first, _last, TreeGridDropPosition.After)!;
		Assert.Multiple(() =>
		{
			Assert.That(request.Operation, Is.EqualTo(TreeGridDropOperation.Reorder));
			Assert.That(request.InsertIndex, Is.EqualTo(2));
			Assert.That(request.DestinationParent, Is.SameAs(_group));
			Assert.That(_group.Children, Is.EqualTo(new[] { _first, _hidden, _last }));
		});
	}

	[Test]
	public void FilteredBefore_InsertsAfterPrecedingVisibleSibling()
	{
		TreeGridNode source = new() { Name = "Source" };
		_group.Children.Add(source);
		TreeGridDropRequest request = TreeGridDropPlanner.Create(source, _last, TreeGridDropPosition.Before,
			false, false, new[] { _root, _group, _first, _last, source })!;
		Assert.That(request.InsertIndex, Is.EqualTo(1));
	}

	[Test]
	public void BeforeFirstVisible_InsertsBeforeHiddenSiblings()
	{
		TreeGridDropRequest request = TreeGridDropPlanner.Create(_last, _hidden, TreeGridDropPosition.Before,
			false, false, new[] { _root, _group, _hidden, _last })!;
		Assert.That(request.InsertIndex, Is.Zero);
	}

	[Test]
	public void ReorderGroup_UsesGroupSequence()
	{
		TreeGridDropRequest request = Plan(_other, _group, TreeGridDropPosition.Before)!;
		Assert.That(request.Operation, Is.EqualTo(TreeGridDropOperation.Reorder));
		Assert.That(request.InsertIndex, Is.Zero);
		Assert.That(request.DestinationParent, Is.SameAs(_root));
	}

	[Test]
	public void ElementOnGroup_RequestsMoveWithoutChangingModels()
	{
		TreeGridDropRequest request = Plan(_first, _other, TreeGridDropPosition.Inside)!;
		Assert.That(request.Operation, Is.EqualTo(TreeGridDropOperation.Move));
		Assert.That(request.DestinationParent, Is.SameAs(_other));
		Assert.That(_first.Parent, Is.SameAs(_group));
		Assert.That(_other.Children, Is.Empty);
	}

	[Test]
	public void GroupOnGroup_RequestsParentChange()
	{
		Assert.That(Plan(_group, _other, TreeGridDropPosition.Inside)!.Operation, Is.EqualTo(TreeGridDropOperation.Move));
	}

	[Test]
	public void ShiftMerge_RequiresExplicitPermission()
	{
		Assert.That(Plan(_group, _other, TreeGridDropPosition.Inside, true, false), Is.Null);
		Assert.That(Plan(_group, _other, TreeGridDropPosition.Inside, true, true)!.Operation, Is.EqualTo(TreeGridDropOperation.Merge));
	}

	[Test]
	public void MergeIntoAncestor_IsAllowed_WhileMoveIntoCurrentParentIsNoOp()
	{
		Assert.That(Plan(_group, _root, TreeGridDropPosition.Inside, true, true)!.Operation, Is.EqualTo(TreeGridDropOperation.Merge));
		Assert.That(Plan(_group, _root, TreeGridDropPosition.Inside), Is.Null);
	}

	[Test]
	public void RootSelfAndDescendantDrops_AreRejected()
	{
		TreeGridNode nested = new() { IsGroup = true };
		_group.Children.Add(nested);
		Assert.Multiple(() =>
		{
			Assert.That(Plan(_root, _other, TreeGridDropPosition.Inside), Is.Null);
			Assert.That(Plan(_group, _group, TreeGridDropPosition.Inside), Is.Null);
			Assert.That(Plan(_group, nested, TreeGridDropPosition.Inside), Is.Null);
			Assert.That(Plan(_group, nested, TreeGridDropPosition.Inside, true, true), Is.Null);
		});
	}

	[Test]
	public void CrossParentAndMixedKindReorders_AreRejected()
	{
		TreeGridNode leaf = new();
		_other.Children.Add(leaf);
		Assert.That(Plan(_first, leaf, TreeGridDropPosition.Before), Is.Null);
		Assert.That(Plan(_first, _other, TreeGridDropPosition.After), Is.Null);
	}

	[Test]
	public void ElementCenterAndUnchangedPosition_AreRejected()
	{
		Assert.That(Plan(_first, _last, TreeGridDropPosition.Inside), Is.Null);
		Assert.That(Plan(_first, _hidden, TreeGridDropPosition.Before), Is.Null);
	}

	[Test]
	public void ShiftOnElement_DoesNotRequestMerge()
	{
		Assert.That(Plan(_first, _other, TreeGridDropPosition.Inside, true, false)!.Operation, Is.EqualTo(TreeGridDropOperation.Move));
	}

	private TreeGridDropRequest? Plan(TreeGridNode source, TreeGridNode target, TreeGridDropPosition position, bool shift = false, bool allowMerge = false) =>
		TreeGridDropPlanner.Create(source, target, position, shift, allowMerge, _projection.VisibleRows);
}
