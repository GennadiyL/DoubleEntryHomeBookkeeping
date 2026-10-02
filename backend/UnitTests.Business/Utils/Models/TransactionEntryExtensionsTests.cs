using Business.Contracts.Utils.Models;
using Business.Models.Entities;
using NUnit.Framework;

namespace UnitTests.Business.Utils.Models;

[TestFixture(Category = "Local")]
public sealed class TransactionEntryExtensionsTests
{
	[TestCase(0, 2)]
	[TestCase(1, 2.2)]
	[TestCase(2, 2.22)]
	[TestCase(3, 2.222)]
	[TestCase(4, 2.2222)]
	public void GetBaseAmount_UsesConfiguredPrecision(int precision, decimal expected)
	{
		TransactionEntry entry = CreateEntry(1.1111m, 2);
		Assert.That(entry.GetBaseAmount(precision), Is.EqualTo(expected));
		Assert.That(entry.Amount, Is.EqualTo(1.1111m));
		Assert.That(entry.Rate, Is.EqualTo(2));
	}

	[TestCase(1.005, 1.00)]
	[TestCase(1.015, 1.02)]
	[TestCase(-1.005, -1.00)]
	[TestCase(-1.015, -1.02)]
	public void GetBaseAmount_Midpoint_RoundsToEven(decimal amount, decimal expected)
	{
		Assert.That(CreateEntry(amount, 1).GetBaseAmount(2), Is.EqualTo(expected));
	}

	[Test]
	public void GetBaseAmount_RoundsEachEntryBeforeSumming()
	{
		int? amountPrecision = 2;
		decimal total = CreateEntry(1.005m, 1).GetBaseAmount(amountPrecision) +
			CreateEntry(1.005m, 1).GetBaseAmount(amountPrecision);
		Assert.That(total, Is.EqualTo(2m));
	}

	[TestCase(-1)]
	[TestCase(5)]
	public void GetBaseAmount_InvalidPrecision_Rejects(int precision)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			CreateEntry(1, 1).GetBaseAmount(precision));
	}

	[Test]
	public void GetBaseAmount_Overflow_ThrowsWithoutMutatingEntry()
	{
		TransactionEntry entry = CreateEntry(decimal.MaxValue, 2);
		Assert.Throws<OverflowException>(() => entry.GetBaseAmount(2));
		Assert.That(entry.Amount, Is.EqualTo(decimal.MaxValue));
		Assert.That(entry.Rate, Is.EqualTo(2));
	}

	[Test]
	public void GetBaseAmount_NullEntry_Rejects()
	{
		Assert.Throws<ArgumentNullException>(() =>
			TransactionEntryExtensions.GetBaseAmount(null!, 2));
	}

	[Test]
	public void GetBaseAmount_NullPrecision_Rejects()
	{
		Assert.Throws<ArgumentNullException>(() => CreateEntry(1, 1).GetBaseAmount(null));
	}

	private static TransactionEntry CreateEntry(decimal amount, decimal rate) =>
		new() { Transaction = new Transaction(), Account = new Account(), Amount = amount, Rate = rate };
}
