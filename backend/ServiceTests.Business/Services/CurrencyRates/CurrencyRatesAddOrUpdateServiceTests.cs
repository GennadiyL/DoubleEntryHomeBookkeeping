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
/// Verifies currency-rate AddOrUpdate through the public service interface.
/// Retains the production configuration operation and DI registrations.
/// Uses substituted repositories to exercise rate lifecycle rules.
/// Checks precision, identity and synchronization behavior where applicable.
/// Protects the initial fallback date during maintenance.
/// Rejects invalid inputs without partial writes.
/// Confirms the single commit boundary and cancellation forwarding.
/// Stored transaction rates remain outside the operation.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class CurrencyRatesAddOrUpdateServiceTests : CurrencyRatesServiceTestsBase
{

	[TestCase(false)]
	[TestCase(true)]
	public async Task AddOrUpdate_NewDateInitializesTrackingAndReturnsIdentity(bool initial)
	{
		if (initial) { Param.Date = AppValues.InitialDate; }
		CurrencyRate? saved = null;
		Unit.CurrencyRateRepo.When(repo => repo.Add(Arg.Any<CurrencyRate>())).Do(call => saved = call.Arg<CurrencyRate>());
		Guid id = await Service.AddOrUpdate(Param);
		Assert.That(saved, Is.Not.Null);
		Assert.That(saved!.Id, Is.EqualTo(id).And.Not.EqualTo(Guid.Empty));
		Assert.That(saved.Currency, Is.SameAs(Currency));
		Assert.That(saved.CurrencyId, Is.EqualTo(Currency.Id));
		Assert.That(saved.Date, Is.EqualTo(Param.Date));
		Assert.That(saved.Rate, Is.EqualTo(1.2344m));
		Assert.That(saved.Description, Is.EqualTo(" Notes "));
		Assert.That(saved.EditRevision, Is.Null);
		Assert.That(saved.DeleteRevision, Is.Null);
		Assert.That(saved.ModificationType, Is.EqualTo(ModificationType.None));
		Unit.CurrencyRateRepo.DidNotReceiveWithAnyArgs().Update(default!);
		await Unit.Received(1).SaveChanges();
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AddOrUpdate_ExistingDatePreservesIdentityRevisionAndOtherFlags(bool initial)
	{
		if (initial) { Param.Date = AppValues.InitialDate; }
		CurrencyRate existing = CreateRate(Param.Date);
		existing.ModificationType = ModificationType.Order;
		Unit.CurrencyRateRepo.GetByCurrencyAndDate(Currency.Id, Param.Date, Arg.Any<CancellationToken>()).Returns(existing);
		Guid id = await Service.AddOrUpdate(Param);
		Assert.That(id, Is.EqualTo(existing.Id));
		Assert.That(existing.Date, Is.EqualTo(Param.Date));
		Assert.That(existing.Rate, Is.EqualTo(1.2344m));
		Assert.That(existing.Description, Is.EqualTo(Param.Description));
		Assert.That(existing.EditRevision, Is.EqualTo(7));
		Assert.That(existing.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Unit.CurrencyRateRepo.Received(1).Update(existing);
		Unit.CurrencyRateRepo.DidNotReceiveWithAnyArgs().Add(default!);
		await Unit.Received(1).SaveChanges();
	}

	[Test]
	public async Task AddOrUpdate_UnchangedNormalizedValuesDoNotCommit()
	{
		CurrencyRate existing = CreateRate(Param.Date, 1.2344m);
		existing.Description = Param.Description;
		Unit.CurrencyRateRepo.GetByCurrencyAndDate(Currency.Id, Param.Date, Arg.Any<CancellationToken>()).Returns(existing);
		Assert.That(await Service.AddOrUpdate(Param), Is.EqualTo(existing.Id));
		AssertNoWrites();
	}

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	[TestCase(3)]
	[TestCase(4)]
	public async Task AddOrUpdate_RoundsToConfiguredPrecision(int precision)
	{
		Config.RatePrecision = precision;
		await Service.AddOrUpdate(Param);
		Unit.CurrencyRateRepo.Received(1).Add(Arg.Is<CurrencyRate>(rate =>
			rate.Rate == Math.Round(Param.Rate, precision, MidpointRounding.ToEven)));
	}

	[TestCase("0")]
	[TestCase("-1")]
	[TestCase("0.00005")]
	[TestCase("922337203685477.5808")]
	[TestCase("79228162514264337593543950335")]
	public void AddOrUpdate_RejectsInvalidNumericValuesWithoutWrites(string input)
	{
		Param.Rate = decimal.Parse(input, CultureInfo.InvariantCulture);
		Assert.ThrowsAsync<InvalidCurrencyException>(async () => await Service.AddOrUpdate(Param));
		AssertNoWrites();
	}

	[Test]
	public async Task AddOrUpdate_AcceptsStorageMaximum()
	{
		Param.Rate = AppValues.MaxDecimal;
		await Service.AddOrUpdate(Param);
		Unit.CurrencyRateRepo.Received(1).Add(Arg.Is<CurrencyRate>(rate => rate.Rate == AppValues.MaxDecimal));
	}

	[Test]
	public void AddOrUpdate_RejectsBaseRateOtherThanOne()
	{
		Config.BaseCurrencyId = Currency.Id;
		Assert.ThrowsAsync<InvalidCurrencyException>(async () => await Service.AddOrUpdate(Param));
		AssertNoWrites();
	}

	[Test]
	public async Task AddOrUpdate_BaseRateOneAfterRoundingIsAllowed()
	{
		Config.BaseCurrencyId = Currency.Id;
		Param.Rate = 1.00004m;
		await Service.AddOrUpdate(Param);
		Unit.CurrencyRateRepo.Received(1).Add(Arg.Is<CurrencyRate>(rate => rate.Rate == 1m));
	}

	[TestCase(1, 1, 1)]
	[TestCase(1970, 1, 2)]
	[TestCase(2000, 12, 31)]
	public void AddOrUpdate_RejectsOrdinaryDateBeforeMinimum(int year, int month, int day)
	{
		Param.Date = new DateOnly(year, month, day);
		Assert.ThrowsAsync<InvalidCurrencyException>(async () => await Service.AddOrUpdate(Param));
		AssertNoWrites();
	}

	[Test]
	public async Task AddOrUpdate_AllowsMinimumOrdinaryDate()
	{
		Param.Date = AppValues.MinDate;
		await Service.AddOrUpdate(Param);
		Unit.CurrencyRateRepo.Received(1).Add(Arg.Is<CurrencyRate>(rate => rate.Date == AppValues.MinDate));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void AddOrUpdate_RejectsMissingOrDeletedCurrency(bool deleted)
	{
		if (deleted) { Currency.DeleteRevision = 0; }
		else { Param.CurrencyId = Guid.NewGuid(); }
		Assert.ThrowsAsync<CurrencyNotFoundException>(async () => await Service.AddOrUpdate(Param));
		AssertNoWrites();
	}

	[TestCase(0L)]
	[TestCase(8L)]
	public void AddOrUpdate_DoesNotRestoreDeletedRate(long revision)
	{
		CurrencyRate existing = CreateRate(Param.Date);
		existing.DeleteRevision = revision;
		Unit.CurrencyRateRepo.GetByCurrencyAndDate(Currency.Id, Param.Date, Arg.Any<CancellationToken>()).Returns(existing);
		Assert.ThrowsAsync<InvalidCurrencyException>(async () => await Service.AddOrUpdate(Param));
		Assert.That(existing.DeleteRevision, Is.EqualTo(revision));
		Assert.That(existing.Rate, Is.EqualTo(2m));
		Assert.That(existing.ModificationType, Is.EqualTo(ModificationType.None));
		AssertNoWrites();
	}

	[Test]
	public async Task AddOrUpdate_ForwardsCancellation()
	{
		using CancellationTokenSource source = new();
		await Service.AddOrUpdate(Param, source.Token);
		await Unit.CurrencyRepo.Received(1).GetById(Currency.Id, source.Token);
		await Unit.SystemConfigRepo.Received(1).GetAll(source.Token);
		await Unit.CurrencyRateRepo.Received(1).GetByCurrencyAndDate(Currency.Id, Param.Date, source.Token);
		await Unit.Received(1).SaveChanges(source.Token);
	}
}
