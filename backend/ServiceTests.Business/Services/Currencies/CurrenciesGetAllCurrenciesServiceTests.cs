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
/// Verifies currency GetAllCurrencies through the public service contract.
/// Resolves the real service and shared operations using production DI.
/// Substitutes repositories to isolate the current catalog state.
/// Checks relevant ordering, visibility and synchronization behavior.
/// Keeps read operations detached and free of commits.
/// Covers no-op or invalid operations where applicable.
/// Ensures unrelated persisted values remain unchanged.
/// Currency selection works without network access.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class CurrenciesGetAllCurrenciesServiceTests : CurrenciesServiceTestsBase
{

	[Test]
	public async Task GetAllCurrencies_ReturnsLiveCurrenciesIncludingBaseInOrder()
	{
		BaseCurrency.Order = 7;
		Currency.Order = 3;
		AddCurrency("JPY", 0).DeleteRevision = 0;
		List<CurrencyInfo> infos = await Service.GetAllCurrencies();
		Assert.That(infos.Select(info => info.Id), Is.EqualTo(new[] { Currency.Id, BaseCurrency.Id }));
		AssertNoWrites();
	}

	[Test]
	public async Task GetAllCurrencies_EmptyCatalogReturnsEmpty()
	{
		Currencies.Clear();
		Assert.That(await Service.GetAllCurrencies(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public async Task GetAllCurrencies_ForwardsCancellation()
	{
		using CancellationTokenSource source = new();
		await Service.GetAllCurrencies(source.Token);
		await Unit.CurrencyRepo.Received(1).GetAllAsync(source.Token);
		AssertNoWrites();
	}
}
