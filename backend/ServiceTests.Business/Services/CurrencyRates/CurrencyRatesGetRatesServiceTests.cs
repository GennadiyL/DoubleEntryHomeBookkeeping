using System.Globalization;
using Business.Contracts.Services.Currencies;
using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Enums;
using Business.Models.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace ServiceTests.Business.Services.CurrencyRates;

/// <summary>
/// Verifies currency-rate GetRates through the service contract.
/// Uses production DI with substituted persistence boundaries.
/// Checks live references and detached read results.
/// Covers initial fallback and base-currency rules where applicable.
/// Missing or invalid stored data fails without invented values.
/// Reads do not update rates, transactions or configuration.
/// Date selectors retain their calendar-date meaning.
/// Cancellation is forwarded to the required repository calls.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class CurrencyRatesGetRatesServiceTests : CurrencyRatesServiceTestsBase
{

	[Test]
	public async Task GetRates_OrdersLiveRatesDescendingAndIncludesInitial()
	{
		CurrencyRate initial = CreateRate(AppValues.InitialDate);
		CurrencyRate earlier = CreateRate(Param.Date.AddDays(-1), 3m);
		CurrencyRate latest = CreateRate(Param.Date, 4m);
		latest.Description = "Latest";
		CurrencyRate deleted = CreateRate(Param.Date.AddDays(1), 5m);
		deleted.DeleteRevision = 0;
		Unit.CurrencyRateRepo.GetByCurrencyIdAsync(Currency.Id, Arg.Any<CancellationToken>())
			.Returns(new List<CurrencyRate> { earlier, initial, deleted, latest });

		List<CurrencyRateInfo> result = await Service.GetRates(Currency.Id);

		Assert.That(result.Select(rate => rate.Date), Is.EqualTo(new[] { latest.Date, earlier.Date, initial.Date }));
		Assert.That(result[0].Description, Is.EqualTo("Latest"));
		Assert.That(result[0].CurrencyId, Is.EqualTo(Currency.Id));
		Assert.That(result[0].IsInitial, Is.False);
		Assert.That(result[2].IsInitial, Is.True);
		result[0].Rate = 99m;
		Assert.That(latest.Rate, Is.EqualTo(4m));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetRates_RejectsMissingOrDeletedCurrency(bool deleted)
	{
		if (deleted) { Currency.DeleteRevision = 0; }
		Assert.ThrowsAsync<CurrencyNotFoundException>(async () => await Service.GetRates(deleted ? Currency.Id : Guid.NewGuid()));
		AssertNoWrites();
	}

	[Test]
	public async Task GetRates_ForwardsCancellation()
	{
		using CancellationTokenSource source = new();
		Unit.CurrencyRateRepo.GetByCurrencyIdAsync(Currency.Id, source.Token).Returns(new List<CurrencyRate>());
		Assert.That(await Service.GetRates(Currency.Id, source.Token), Is.Empty);
		await Unit.CurrencyRateRepo.Received(1).GetByCurrencyIdAsync(Currency.Id, source.Token);
		AssertNoWrites();
	}
}
