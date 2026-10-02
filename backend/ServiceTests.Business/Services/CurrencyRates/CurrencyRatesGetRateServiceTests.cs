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
/// Verifies currency-rate GetRate through the service contract.
/// Uses production DI with substituted persistence boundaries.
/// Checks live references and detached read results.
/// Covers initial fallback and base-currency rules where applicable.
/// Missing or invalid stored data fails without invented values.
/// Reads do not update rates, transactions or configuration.
/// Date selectors retain their calendar-date meaning.
/// Cancellation is forwarded to the required repository calls.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class CurrencyRatesGetRateServiceTests : CurrencyRatesServiceTestsBase
{

	[Test]
	public async Task GetRate_BaseCurrencyReturnsOneWithoutRateQuery()
	{
		Config.BaseCurrencyId = Currency.Id;
		Assert.That(await Service.GetRate(Account.Id, Param.Date), Is.EqualTo(1m));
		await Unit.CurrencyRateRepo.DidNotReceiveWithAnyArgs().GetApplicableAsync(default, default, default);
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task GetRate_ReturnsApplicableStoredRateWithoutChangingEntries(bool initial)
	{
		CurrencyRate rate = CreateRate(initial ? AppValues.InitialDate : Param.Date.AddDays(-1), 1.2345m);
		Unit.CurrencyRateRepo.GetApplicableAsync(Currency.Id, Param.Date, Arg.Any<CancellationToken>()).Returns(rate);
		Assert.That(await Service.GetRate(Account.Id, Param.Date), Is.EqualTo(1.2345m));
		Assert.That(rate.EditRevision, Is.EqualTo(7));
		Assert.That(rate.ModificationType, Is.EqualTo(ModificationType.None));
		Unit.TransactionEntryRepo.DidNotReceiveWithAnyArgs().Update(default!);
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetRate_RejectsMissingOrDeletedAccount(bool deleted)
	{
		if (deleted) { Account.DeleteRevision = 0; }
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await Service.GetRate(deleted ? Account.Id : Guid.NewGuid(), Param.Date));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetRate_RejectsMissingOrDeletedCurrency(bool deleted)
	{
		if (deleted) { Currency.DeleteRevision = 0; }
		else { Account.CurrencyId = Guid.NewGuid(); }
		Assert.ThrowsAsync<CurrencyNotFoundException>(async () => await Service.GetRate(Account.Id, Param.Date));
		AssertNoWrites();
	}

	[Test]
	public void GetRate_MissingFallbackIsCriticalFailure()
	{
		Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.GetRate(Account.Id, Param.Date));
		AssertNoWrites();
	}

	[TestCase("0")]
	[TestCase("-1")]
	[TestCase("922337203685477.5808")]
	public void GetRate_InvalidStoredRateIsCriticalFailure(string value)
	{
		CurrencyRate rate = CreateRate(Param.Date, decimal.Parse(value, CultureInfo.InvariantCulture));
		Unit.CurrencyRateRepo.GetApplicableAsync(Currency.Id, Param.Date, Arg.Any<CancellationToken>()).Returns(rate);
		Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.GetRate(Account.Id, Param.Date));
		AssertNoWrites();
	}

	[Test]
	public async Task GetRate_ForwardsExactDateAndCancellation()
	{
		using CancellationTokenSource source = new();
		Unit.CurrencyRateRepo.GetApplicableAsync(Currency.Id, Param.Date, source.Token).Returns(CreateRate(Param.Date));
		await Service.GetRate(Account.Id, Param.Date, source.Token);
		await Unit.AccountRepo.Received(1).GetByIdAsync(Account.Id, source.Token);
		await Unit.CurrencyRepo.Received(1).GetByIdAsync(Currency.Id, source.Token);
		await Unit.CurrencyRateRepo.Received(1).GetApplicableAsync(Currency.Id, Param.Date, source.Token);
		AssertNoWrites();
	}
}
