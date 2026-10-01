using Business.Contracts.Utils.Ordering;
using Business.Models.Entities;
using NUnit.Framework;

namespace UnitTests.Business.Utils.Ordering;

/// <summary>
/// Verifies the shared zero-based collection ordering helpers.
/// Exercises moves from each position to every valid destination.
/// Covers a member already occupying index zero without treating it as unassigned.
/// Checks normalization of gaps without changing relative order.
/// Verifies invalid requests leave existing positions unchanged.
/// Covers empty and single-member collections.
/// Confirms the empty maximum permits appending the first member at zero.
/// Persistence and synchronization tracking are covered by service tests.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class OrderingExtensionsTests
{
	[TestCase(0, 0, "ABC")]
	[TestCase(0, 1, "BAC")]
	[TestCase(0, 2, "BCA")]
	[TestCase(1, 0, "BAC")]
	[TestCase(1, 1, "ABC")]
	[TestCase(1, 2, "ACB")]
	[TestCase(2, 0, "CAB")]
	[TestCase(2, 1, "ACB")]
	[TestCase(2, 2, "ABC")]
	public void SetOrder_AllPositions_PreservesRequestedSequence(int from, int to, string expected)
	{
		List<Category> items = [
			new() { Name = "A", Order = 0 },
			new() { Name = "B", Order = 1 },
			new() { Name = "C", Order = 2 }
		];

		items.SetOrder(items[from], to);

		Assert.That(string.Concat(items.OrderBy(item => item.Order).Select(item => item.Name)), Is.EqualTo(expected));
		Assert.That(items.Select(item => item.Order).Order(), Is.EqualTo(Enumerable.Range(0, 3)));
	}

	[Test]
	public void Reorder_Gaps_NormalizesFromZero()
	{
		Category first = new() { Order = -5 };
		Category second = new() { Order = 20 };
		List<Category> items = [second, first];

		items.Reorder();

		Assert.That(first.Order, Is.Zero);
		Assert.That(second.Order, Is.EqualTo(1));
	}

	[Test]
	public void EmptyCollection_NormalizesAndAppendsAtZero()
	{
		List<Category> items = [];

		items.Reorder();

		Assert.That(items, Is.Empty);
		Assert.That(items.GetMaxOrder() + 1, Is.Zero);
	}

	[Test]
	public void SetOrder_SingleMember_NormalizesToZero()
	{
		Category item = new() { Order = 12 };
		List<Category> items = [item];

		items.SetOrder(item, 0);

		Assert.That(item.Order, Is.Zero);
		Assert.That(items.GetMaxOrder(), Is.Zero);
	}

	[TestCase(-1)]
	[TestCase(2)]
	public void SetOrder_InvalidIndex_DoesNotMutate(int order)
	{
		List<Category> items = [new() { Order = 5 }, new() { Order = 8 }];

		Assert.Throws<ArgumentOutOfRangeException>(() => items.SetOrder(items[0], order));

		Assert.That((items[0].Order, items[1].Order), Is.EqualTo((5, 8)));
	}

	[Test]
	public void SetOrder_MissingMember_DoesNotMutate()
	{
		List<Category> items = [new() { Order = 5 }];

		Assert.Throws<ArgumentException>(() => items.SetOrder(new Category(), 0));

		Assert.That(items[0].Order, Is.EqualTo(5));
	}
}
