using NUnit.Framework;
using WinUi.Controls.TreeGridControl;

namespace UnitTests.WinUi.Controls;

/// <summary>
/// Checks name search independently of a window or dispatcher.
/// Whole-tree traversal includes descendants of collapsed groups.
/// Forward and backward navigation wrap in the supplied tree order.
/// Matching uses names rather than additional cell values.
/// Missing and blank queries do not produce a selection.
/// A single matching row remains reachable after wrapping.
/// Changes to names and ordering take effect on the next request.
/// Search leaves expansion, star and checkbox values untouched.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class TreeGridSearchTests
{
	[Test]
	public void Search_IncludesCollapsedBranchesAndWrapsInBothDirections()
	{
		TreeGridNode first = new() { Name = "My bank savings", IsChecked = true, IsStarred = true };
		TreeGridNode last = new() { Name = "BANK account" };
		TreeGridNode group = new() { Name = "Bank group", IsGroup = true, Children = { first } };
		TreeGridNode[] roots = { group, last };

		Assert.Multiple(() =>
		{
			Assert.That(TreeGridSearch.Find(roots, null, "bank", false), Is.SameAs(group));
			Assert.That(TreeGridSearch.Find(roots, group, "bank", false), Is.SameAs(first));
			Assert.That(TreeGridSearch.Find(roots, first, "bank", false), Is.SameAs(last));
			Assert.That(TreeGridSearch.Find(roots, last, "bank", false), Is.SameAs(group));
			Assert.That(TreeGridSearch.Find(roots, null, "bank", true), Is.SameAs(last));
			Assert.That(TreeGridSearch.Find(roots, group, "bank", true), Is.SameAs(last));
			Assert.That(TreeGridSearch.Find(roots, last, "bank", true), Is.SameAs(first));
			Assert.That(group.IsExpanded, Is.False);
			Assert.That(first.IsChecked, Is.True);
			Assert.That(first.IsStarred, Is.True);
		});
	}

	[TestCase("")]
	[TestCase(" ")]
	[TestCase("missing")]
	public void Search_NoMatchOrEmptyQuery_ReturnsNoRow(string query)
	{
		TreeGridNode row = new() { Name = "Account", Data = "missing" };
		Assert.That(TreeGridSearch.Find(new[] { row }, row, query, false), Is.Null);
		Assert.That(TreeGridSearch.Find(Array.Empty<TreeGridNode>(), null, query, true), Is.Null);
	}

	[Test]
	public void Search_UsesCurrentNamesAndOrder_AndWrapsToSingleMatch()
	{
		TreeGridNode first = new() { Name = "Cash" };
		TreeGridNode second = new() { Name = "Cash" };
		TreeGridNode group = new() { IsGroup = true, Children = { first, second } };
		TreeGridNode[] roots = { group };
		group.Children.Move(1, 0);
		Assert.That(TreeGridSearch.Find(roots, null, "cash", false), Is.SameAs(second));
		second.Name = "Savings";
		Assert.That(TreeGridSearch.Find(roots, first, "cash", false), Is.SameAs(first));
		Assert.That(TreeGridSearch.Find(roots, new TreeGridNode(), "cash", true), Is.SameAs(first));
	}
}
