using System.Globalization;
using Business.Contracts.Services.Currencies;
using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Enums;
using Business.Models.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace ServiceTests.Business.Services.Currencies;

/// <summary>
/// Verifies currency SetOrder through the public service contract.
/// Resolves the real service and shared operations using production DI.
/// Substitutes repositories to isolate the current catalog state.
/// Checks relevant ordering, visibility and synchronization behavior.
/// Keeps read operations detached and free of commits.
/// Covers no-op or invalid operations where applicable.
/// Ensures unrelated persisted values remain unchanged.
/// Currency selection works without network access.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class CurrenciesSetOrderServiceTests : CurrenciesServiceTestsBase
{

	[Test]
	public async Task SetOrder_ReordersBaseAndForeignCurrenciesWithOrderFlagsOnly()
	{
		BaseCurrency.ModificationType = ModificationType.Content;
		Currency deleted = AddCurrency("JPY", 8);
		deleted.DeleteRevision = 0;
		await Service.SetOrder(Currency.Id, 0);
		Assert.That(Currency.Order, Is.Zero);
		Assert.That(BaseCurrency.Order, Is.EqualTo(1));
		Assert.That(Currency.ModificationType, Is.EqualTo(ModificationType.Order));
		Assert.That(BaseCurrency.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Assert.That(Currency.EditRevision, Is.EqualTo(7));
		Assert.That(deleted.Order, Is.EqualTo(8));
		await Unit.Received(1).SaveChangesAsync();
	}

	[TestCase(-1)]
	[TestCase(2)]
	public void SetOrder_RejectsInvalidPosition(int order)
	{
		Assert.ThrowsAsync<InvalidCurrencyException>(async () => await Service.SetOrder(Currency.Id, order));
		AssertNoWrites();
	}

	[Test]
	public async Task SetOrder_UnchangedDoesNotSave()
	{
		await Service.SetOrder(Currency.Id, 1);
		AssertNoWrites();
	}

	[Test]
	public async Task SetOrder_NormalizesGapsAndUsesGuidForTies()
	{
		BaseCurrency.Order = 8;
		Currency.Order = 8;
		Currency first = Currencies.OrderBy(item => item.Id.ToString("D"), StringComparer.Ordinal).First();
		Currency second = Currencies.Single(item => item.Id != first.Id);
		await Service.SetOrder(first.Id, 0);
		Assert.That(first.Order, Is.Zero);
		Assert.That(second.Order, Is.EqualTo(1));
		await Unit.Received(1).SaveChangesAsync();
	}

	[Test]
	public void SetOrder_RejectsDeletedTarget()
	{
		Currency.DeleteRevision = 0;
		Assert.ThrowsAsync<CurrencyNotFoundException>(async () => await Service.SetOrder(Currency.Id, 0));
		AssertNoWrites();
	}
}
