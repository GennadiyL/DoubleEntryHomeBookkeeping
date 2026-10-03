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
/// Verifies currency SetFavoriteStatus through the public service contract.
/// Resolves the real service and shared operations using production DI.
/// Substitutes repositories to isolate the current catalog state.
/// Checks relevant ordering, visibility and synchronization behavior.
/// Keeps read operations detached and free of commits.
/// Covers no-op or invalid operations where applicable.
/// Ensures unrelated persisted values remain unchanged.
/// Currency selection works without network access.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class CurrenciesSetFavoriteStatusServiceTests : CurrenciesServiceTestsBase
{

	[TestCase(false)]
	[TestCase(true)]
	public async Task SetFavoriteStatus_ChangesContentPreservingOrder(bool baseCurrency)
	{
		Currency target = baseCurrency ? BaseCurrency : Currency;
		target.ModificationType = ModificationType.Order;
		await Service.SetFavoriteStatus(target.Id, true);
		Assert.That(target.IsFavorite, Is.True);
		Assert.That(target.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Assert.That(target.EditRevision, Is.EqualTo(7));
		await Unit.Received(1).SaveChanges();
	}

	[Test]
	public async Task SetFavoriteStatus_UnchangedDoesNotSave()
	{
		await Service.SetFavoriteStatus(Currency.Id, false);
		AssertNoWrites();
	}

	[Test]
	public void SetFavoriteStatus_RejectsDeletedCurrency()
	{
		Currency.DeleteRevision = 0;
		Assert.ThrowsAsync<CurrencyNotFoundException>(async () => await Service.SetFavoriteStatus(Currency.Id, true));
		AssertNoWrites();
	}
}
