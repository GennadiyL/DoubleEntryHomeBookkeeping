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
/// Verifies currency-rate Delete through the public service interface.
/// Retains the production configuration operation and DI registrations.
/// Uses substituted repositories to exercise rate lifecycle rules.
/// Checks precision, identity and synchronization behavior where applicable.
/// Protects the initial fallback date during maintenance.
/// Rejects invalid inputs without partial writes.
/// Confirms the single commit boundary and cancellation forwarding.
/// Stored transaction rates remain outside the operation.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class CurrencyRatesDeleteServiceTests : CurrencyRatesServiceTestsBase
{

	[TestCase(null, ModificationType.None)]
	[TestCase(7L, ModificationType.Content)]
	[TestCase(7L, ModificationType.Order)]
	public async Task Delete_ExcludesInitialAndPreservesFlagsWithOneCommit(long? revision, ModificationType flags)
	{
		CurrencyRate initial = CreateRate(AppValues.InitialDate);
		CurrencyRate first = CreateRate(AppValues.MinDate);
		CurrencyRate last = CreateRate(Param.Date);
		first.EditRevision = revision;
		first.ModificationType = flags;
		Unit.CurrencyRateRepo.GetByCurrencyAndDateRange(Currency.Id, AppValues.InitialDate, Param.Date, Arg.Any<CancellationToken>())
			.Returns(new List<CurrencyRate> { initial, first, last });

		await Service.Delete(Currency.Id, AppValues.InitialDate, Param.Date);

		Assert.That(initial.DeleteRevision, Is.Null);
		Assert.That(first.DeleteRevision, Is.Zero);
		Assert.That(last.DeleteRevision, Is.Zero);
		Assert.That(first.EditRevision, Is.EqualTo(revision));
		Assert.That(first.ModificationType, Is.EqualTo(flags));
		Unit.CurrencyRateRepo.DidNotReceive().Update(initial);
		Unit.CurrencyRateRepo.Received(1).Update(first);
		Unit.CurrencyRateRepo.Received(1).Update(last);
		await Unit.Received(1).SaveChanges();
	}

	[Test]
	public async Task Delete_EqualEndpointsSelectOneDay()
	{
		CurrencyRate rate = CreateRate(Param.Date);
		Unit.CurrencyRateRepo.GetByCurrencyAndDateRange(Currency.Id, Param.Date, Param.Date, Arg.Any<CancellationToken>())
			.Returns(new List<CurrencyRate> { rate });
		await Service.Delete(Currency.Id, Param.Date, Param.Date);
		Assert.That(rate.DeleteRevision, Is.Zero);
		await Unit.Received(1).SaveChanges();
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task Delete_NoOrdinaryMatchesDoesNotCommit(bool initial)
	{
		List<CurrencyRate> rates = initial ? [CreateRate(AppValues.InitialDate)] : [];
		Unit.CurrencyRateRepo.GetByCurrencyAndDateRange(Currency.Id, AppValues.InitialDate, Param.Date, Arg.Any<CancellationToken>()).Returns(rates);
		await Service.Delete(Currency.Id, AppValues.InitialDate, Param.Date);
		AssertNoWrites();
	}

	[Test]
	public void Delete_ReversedRangeRejectedBeforeQueries()
	{
		Unit.ClearReceivedCalls();
		Assert.ThrowsAsync<InvalidCurrencyException>(async () => await Service.Delete(Currency.Id, Param.Date, Param.Date.AddDays(-1)));
		Assert.That(Unit.ReceivedCalls(), Is.Empty);
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Delete_RejectsMissingOrDeletedCurrency(bool deleted)
	{
		if (deleted) { Currency.DeleteRevision = 0; }
		Assert.ThrowsAsync<CurrencyNotFoundException>(async () =>
			await Service.Delete(deleted ? Currency.Id : Guid.NewGuid(), Param.Date, Param.Date));
		AssertNoWrites();
	}

	[Test]
	public async Task Delete_ForwardsCancellation()
	{
		using CancellationTokenSource source = new();
		CurrencyRate rate = CreateRate(Param.Date);
		Unit.CurrencyRateRepo.GetByCurrencyAndDateRange(Currency.Id, Param.Date, Param.Date, source.Token).Returns(new List<CurrencyRate> { rate });
		await Service.Delete(Currency.Id, Param.Date, Param.Date, source.Token);
		await Unit.CurrencyRateRepo.Received(1).GetByCurrencyAndDateRange(Currency.Id, Param.Date, Param.Date, source.Token);
		await Unit.Received(1).SaveChanges(source.Token);
	}
}
