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
/// Verifies currency GetById through the public service contract.
/// Resolves the real service and shared operations using production DI.
/// Substitutes repositories to isolate the current catalog state.
/// Checks relevant ordering, visibility and synchronization behavior.
/// Keeps read operations detached and free of commits.
/// Covers no-op or invalid operations where applicable.
/// Ensures unrelated persisted values remain unchanged.
/// Currency selection works without network access.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class CurrenciesGetByIdServiceTests : CurrenciesServiceTestsBase
{

	[Test]
	public async Task GetById_ReturnsDetachedEditorProjection()
	{
		Currency.IsFavorite = true;
		CurrencyInfo info = await Service.GetById(Currency.Id);
		Assert.That((info.Id, info.Code, info.Name, info.Symbol, info.Order, info.IsFavorite),
			Is.EqualTo((Currency.Id, Currency.Code, Currency.Name, Currency.Symbol, Currency.Order, true)));
		info.Name = "Changed";
		Assert.That(Currency.Name, Is.EqualTo("GBP"));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetById_RejectsMissingOrDeletedCurrency(bool deleted)
	{
		if (deleted) { Currency.DeleteRevision = 0; }
		Assert.ThrowsAsync<CurrencyNotFoundException>(async () => await Service.GetById(deleted ? Currency.Id : Guid.NewGuid()));
		AssertNoWrites();
	}
}
