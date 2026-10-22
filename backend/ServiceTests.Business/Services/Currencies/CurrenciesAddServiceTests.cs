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
/// Verifies currency Add through the public service interface.
/// Uses production service and operation registrations.
/// Substitutes database access to isolate business rules.
/// Checks synchronization tracking and atomic commit boundaries.
/// Exercises invalid input without partial mutation.
/// Preserves unrelated currency and rate state.
/// Covers relevant identity, precision and reference rules.
/// Database behavior is checked separately against SQLite.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class CurrenciesAddServiceTests : CurrenciesServiceTestsBase
{

	[Test]
	public async Task Add_CreatesCurrencyAndInitialRateWithOneCommit()
	{
		Currency? saved = null;
		CurrencyRate? initial = null;
		Unit.CurrencyRepo.When(repo => repo.Add(Arg.Any<Currency>())).Do(call => saved = call.Arg<Currency>());
		Unit.CurrencyRateRepo.When(repo => repo.Add(Arg.Any<CurrencyRate>())).Do(call => initial = call.Arg<CurrencyRate>());

		Guid id = await Service.Add(" eur ", 1.23445m);

		Assert.That(saved, Is.Not.Null);
		Assert.That(saved!.Id, Is.EqualTo(id).And.EqualTo(CatalogId("EUR")));
		Assert.That((saved.Code, saved.Name, saved.Order, saved.IsFavorite), Is.EqualTo(("EUR", "EUR", 2, false)));
		Assert.That((saved.EditRevision, saved.DeleteRevision, saved.ModificationType), Is.EqualTo(((long?)null, (long?)null, ModificationType.None)));
		AvailableCurrencyInfo profile = (await Service.GetAvailableCurrencies()).Single(item => item.Code == "EUR");
		Assert.That((saved.EnglishName, saved.Symbol), Is.EqualTo((profile.EnglishName, profile.Symbol)));
		Assert.That(initial, Is.Not.Null);
		Assert.That(initial!.Id, Is.Not.EqualTo(Guid.Empty).And.Not.EqualTo(id));
		Assert.That(initial.Currency, Is.SameAs(saved));
		Assert.That(initial.CurrencyId, Is.EqualTo(id));
		Assert.That(initial.Date, Is.EqualTo(AppValues.InitialDate));
		Assert.That(initial.Rate, Is.EqualTo(1.2344m));
		Assert.That((initial.EditRevision, initial.DeleteRevision, initial.ModificationType), Is.EqualTo(((long?)null, (long?)null, ModificationType.None)));
		Assert.That(saved.Rates.Single(), Is.SameAs(initial));
		await Unit.Received(1).SaveChanges();
	}

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	[TestCase(3)]
	[TestCase(4)]
	public async Task Add_UsesConfiguredRatePrecision(int precision)
	{
		Config.RatePrecision = precision;
		await Service.Add(" eur ", 1.23456m);
		Unit.CurrencyRateRepo.Received(1).Add(Arg.Is<CurrencyRate>(rate =>
			rate.Rate == Math.Round(1.23456m, precision, MidpointRounding.ToEven)));
	}

	[TestCase("0")]
	[TestCase("-1")]
	[TestCase("0.00005")]
	[TestCase("922337203685477.5808")]
	[TestCase("79228162514264337593543950335")]
	public void Add_RejectsInvalidRateBeforeWrites(string value)
	{
		Assert.ThrowsAsync<InvalidCurrencyException>(async () => await Service.Add(" eur ", decimal.Parse(value, CultureInfo.InvariantCulture)));
		AssertNoWrites();
	}

	[Test]
	public async Task Add_AcceptsMaximumStorageRate()
	{
		await Service.Add(" eur ", AppValues.MaxDecimal);
		Unit.CurrencyRateRepo.Received(1).Add(Arg.Is<CurrencyRate>(rate => rate.Rate == AppValues.MaxDecimal));
	}

	[Test]
	public void Add_RejectsDuplicateActiveCode()
	{
		Assert.ThrowsAsync<InvalidCurrencyException>(async () => await Service.Add(" gBp ", 1m));
		AssertNoWrites();
	}

	[TestCase(0L, null)]
	[TestCase(0L, 0L)]
	[TestCase(0L, 7L)]
	[TestCase(9L, 7L)]
	public async Task Add_RestoresDeletedCurrencyAndInitialRateWithoutChangingIdentities(long deleteRevision, long? editRevision)
	{
		Currency.DeleteRevision = deleteRevision;
		Currency.EditRevision = editRevision;
		Currency.Name = "Pound";
		Currency.IsFavorite = true;
		Currency.Order = 8;
		Currency.ModificationType = ModificationType.Order;
		CurrencyRate initial = new()
		{
			Id = Guid.NewGuid(), CurrencyId = Currency.Id, Currency = Currency, Date = AppValues.InitialDate,
			Rate = 1m, Description = "Preserved", EditRevision = editRevision, DeleteRevision = deleteRevision
		};
		Guid rateId = initial.Id;
		Unit.CurrencyRateRepo.GetByCurrencyAndDate(Currency.Id, AppValues.InitialDate, Arg.Any<CancellationToken>()).Returns(initial);

		Guid id = await Service.Add(" gBp ", 1.25m);

		Assert.That(id, Is.EqualTo(Currency.Id).And.EqualTo(CatalogId("GBP")));
		Assert.That((Currency.Name, Currency.IsFavorite, Currency.Order, Currency.DeleteRevision, Currency.EditRevision),
			Is.EqualTo(("Pound", true, 1, (long?)null, editRevision)));
		Assert.That(Currency.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Assert.That((initial.Id, initial.Rate, initial.Description, initial.DeleteRevision, initial.EditRevision, initial.ModificationType),
			Is.EqualTo((rateId, 1.25m, "Preserved", (long?)null, editRevision, ModificationType.Content)));
		Unit.CurrencyRepo.DidNotReceiveWithAnyArgs().Add(default!);
		Unit.CurrencyRepo.Received(1).Update(Currency);
		Unit.CurrencyRateRepo.DidNotReceiveWithAnyArgs().Add(default!);
		Unit.CurrencyRateRepo.Received(1).Update(initial);
		await Unit.Received(1).SaveChanges();
	}

	[Test]
	public async Task Add_RestoresCurrencyWithoutModifyingUnchangedLiveInitialRate()
	{
		Currency.DeleteRevision = 0;
		CurrencyRate initial = new()
		{
			Id = Guid.NewGuid(), CurrencyId = Currency.Id, Currency = Currency, Date = AppValues.InitialDate,
			Rate = 1.25m, EditRevision = 4
		};
		Unit.CurrencyRateRepo.GetByCurrencyAndDate(Currency.Id, AppValues.InitialDate, Arg.Any<CancellationToken>()).Returns(initial);
		await Service.Add("GBP", 1.25m);
		Assert.That(Currency.DeleteRevision, Is.Null);
		Unit.CurrencyRateRepo.DidNotReceiveWithAnyArgs().Update(default!);
		Unit.CurrencyRateRepo.DidNotReceiveWithAnyArgs().Add(default!);
	}

	[Test]
	public async Task Add_UsesSamePermanentIdentityAfterPhysicalRemoval()
	{
		Guid originalId = Currency.Id;
		Currencies.Remove(Currency);
		Guid recreatedId = await Service.Add("GBP", 1.25m);
		Assert.That(recreatedId, Is.EqualTo(originalId));
		Unit.CurrencyRepo.Received(1).Add(Arg.Is<Currency>(currency => currency.Id == originalId && currency.EditRevision == null));
	}

	[Test]
	public void Add_InvalidRestorationRateLeavesDeletedRowsUntouched()
	{
		Currency.DeleteRevision = 9;
		Assert.ThrowsAsync<InvalidCurrencyException>(async () => await Service.Add("GBP", 0m));
		Assert.That(Currency.DeleteRevision, Is.EqualTo(9));
		AssertNoWrites();
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase(" ")]
	[TestCase("INVALID")]
	public void Add_RejectsUnavailableCode(string? code)
	{

		Assert.ThrowsAsync<InvalidCurrencyCodeException>(async () => await Service.Add(code!, 1m));
		AssertNoWrites();
	}

	[Test]
	public async Task Add_EmptyCatalogStartsAtZeroAndIgnoresDeletedOrders()
	{
		Currencies.Clear();
		AddCurrency("JPY", int.MaxValue).DeleteRevision = 0;
		await Service.Add(" eur ", 1m);
		Unit.CurrencyRepo.Received(1).Add(Arg.Is<Currency>(currency => currency.Order == 0));
	}

	[Test]
	public void Add_RejectsOrderOverflow()
	{
		Currency.Order = int.MaxValue;
		Assert.ThrowsAsync<InvalidCurrencyException>(async () => await Service.Add(" eur ", 1m));
		AssertNoWrites();
	}

	[Test]
	public async Task Add_ForwardsCancellation()
	{
		using CancellationTokenSource source = new();
		await Service.Add(" eur ", 1m, source.Token);
		await Unit.CurrencyRepo.Received(1).GetAll(source.Token);
		await Unit.SystemConfigRepo.Received(1).GetAll(source.Token);
		await Unit.Received(1).SaveChanges(source.Token);
	}
}
