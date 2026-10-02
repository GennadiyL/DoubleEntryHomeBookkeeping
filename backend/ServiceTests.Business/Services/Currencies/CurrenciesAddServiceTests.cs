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

		Guid id = await Service.Add(Param, 1.23445m);

		Assert.That(saved, Is.Not.Null);
		Assert.That(saved!.Id, Is.EqualTo(id).And.Not.EqualTo(Guid.Empty));
		Assert.That((saved.Code, saved.Name, saved.Symbol, saved.Order, saved.IsFavorite), Is.EqualTo(("EUR", "Euro", "EUR", 2, false)));
		Assert.That((saved.EditRevision, saved.DeleteRevision, saved.ModificationType), Is.EqualTo(((long?)null, (long?)null, ModificationType.None)));
		Assert.That(initial, Is.Not.Null);
		Assert.That(initial!.Id, Is.Not.EqualTo(Guid.Empty).And.Not.EqualTo(id));
		Assert.That(initial.Currency, Is.SameAs(saved));
		Assert.That(initial.CurrencyId, Is.EqualTo(id));
		Assert.That(initial.Date, Is.EqualTo(AppValues.InitialDate));
		Assert.That(initial.Rate, Is.EqualTo(1.2344m));
		Assert.That((initial.EditRevision, initial.DeleteRevision, initial.ModificationType), Is.EqualTo(((long?)null, (long?)null, ModificationType.None)));
		Assert.That(saved.Rates.Single(), Is.SameAs(initial));
		await Unit.Received(1).SaveChangesAsync();
	}

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	[TestCase(3)]
	[TestCase(4)]
	public async Task Add_UsesConfiguredRatePrecision(int precision)
	{
		Config.RatePrecision = precision;
		await Service.Add(Param, 1.23456m);
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
		Assert.ThrowsAsync<InvalidCurrencyException>(async () => await Service.Add(Param, decimal.Parse(value, CultureInfo.InvariantCulture)));
		AssertNoWrites();
	}

	[Test]
	public async Task Add_AcceptsMaximumStorageRate()
	{
		await Service.Add(Param, AppValues.MaxDecimal);
		Unit.CurrencyRateRepo.Received(1).Add(Arg.Is<CurrencyRate>(rate => rate.Rate == AppValues.MaxDecimal));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Add_RejectsDuplicateCodeIncludingDeletedCurrency(bool deleted)
	{
		Param.Code = " gBp ";
		if (deleted) { Currency.DeleteRevision = 0; }
		Assert.ThrowsAsync<InvalidCurrencyException>(async () => await Service.Add(Param, 1m));
		AssertNoWrites();
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase(" ")]
	[TestCase("INVALID")]
	public void Add_RejectsUnavailableCode(string? code)
	{
		Param.Code = code!;
		Assert.ThrowsAsync<InvalidCurrencyCodeException>(async () => await Service.Add(Param, 1m));
		AssertNoWrites();
	}

	[TestCase(true)]
	[TestCase(false)]
	public void Add_RejectsBlankMetadata(bool name)
	{
		if (name) { Param.Name = " "; }
		else { Param.Symbol = " "; }
		Assert.ThrowsAsync<InvalidCurrencyException>(async () => await Service.Add(Param, 1m));
		AssertNoWrites();
	}

	[Test]
	public async Task Add_EmptyCatalogStartsAtZeroAndIgnoresDeletedOrders()
	{
		Currencies.Clear();
		AddCurrency("JPY", int.MaxValue).DeleteRevision = 0;
		await Service.Add(Param, 1m);
		Unit.CurrencyRepo.Received(1).Add(Arg.Is<Currency>(currency => currency.Order == 0));
	}

	[Test]
	public void Add_RejectsOrderOverflow()
	{
		Currency.Order = int.MaxValue;
		Assert.ThrowsAsync<InvalidCurrencyException>(async () => await Service.Add(Param, 1m));
		AssertNoWrites();
	}

	[Test]
	public async Task Add_ForwardsCancellation()
	{
		using CancellationTokenSource source = new();
		await Service.Add(Param, 1m, source.Token);
		await Unit.CurrencyRepo.Received(1).GetAllAsync(source.Token);
		await Unit.SystemConfigRepo.Received(1).GetAllAsync(source.Token);
		await Unit.Received(1).SaveChangesAsync(source.Token);
	}
}
