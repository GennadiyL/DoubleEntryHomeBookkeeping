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
/// Verifies currency GetAvailableCurrencies through the public service contract.
/// Resolves the real service and shared operations using production DI.
/// Substitutes repositories to isolate the current catalog state.
/// Checks relevant ordering, visibility and synchronization behavior.
/// Keeps read operations detached and free of commits.
/// Covers no-op or invalid operations where applicable.
/// Ensures unrelated persisted values remain unchanged.
/// Currency selection works without network access.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class CurrenciesGetAvailableCurrenciesServiceTests : CurrenciesServiceTestsBase
{

	[Test]
	public async Task GetAvailableCurrencies_ReturnsOfflineDistinctProfiles()
	{
		Unit.ClearReceivedCalls();
		List<AvailableCurrencyInfo> infos = await Service.GetAvailableCurrencies();
		Assert.That(infos.Select(info => info.Code), Is.Unique);
		Assert.That(infos.Single(info => info.Code == "USD").EnglishName, Is.Not.Empty);
		Assert.That(infos.Single(info => info.Code == "EUR").Symbol, Is.Not.Empty);
		Assert.That(Unit.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void GetAvailableCurrencies_HonorsCancellation()
	{
		using CancellationTokenSource source = new();
		source.Cancel();
		Assert.CatchAsync<OperationCanceledException>(async () => await Service.GetAvailableCurrencies(source.Token));
		AssertNoWrites();
	}
}
