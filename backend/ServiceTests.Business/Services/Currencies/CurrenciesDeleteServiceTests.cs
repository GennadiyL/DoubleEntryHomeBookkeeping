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
/// Verifies currency Delete through the public service interface.
/// Uses production service and operation registrations.
/// Substitutes database access to isolate business rules.
/// Checks synchronization tracking and atomic commit boundaries.
/// Exercises invalid input without partial mutation.
/// Preserves unrelated currency and rate state.
/// Covers relevant identity, precision and reference rules.
/// Database behavior is checked separately against SQLite.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class CurrenciesDeleteServiceTests : CurrenciesServiceTestsBase
{

	[TestCase(null, ModificationType.None)]
	[TestCase(7L, ModificationType.Content)]
	[TestCase(7L, ModificationType.Order)]
	public async Task Delete_SoftDeletesPreservingFlagsAndRateHistory(long? revision, ModificationType flags)
	{
		Currency.EditRevision = revision;
		Currency.ModificationType = flags;
		CurrencyRate initial = new() { Id = Guid.NewGuid(), Currency = Currency, CurrencyId = Currency.Id, Date = AppValues.InitialDate, Rate = 1m };
		Currency.Rates.Add(initial);
		CurrencyRate dated = new()
		{
			Id = Guid.NewGuid(), CurrencyId = Currency.Id, Currency = Currency, Date = new DateOnly(2026, 1, 2),
			Rate = 2m, EditRevision = revision, DeleteRevision = 4
		};
		Unit.CurrencyRateRepo.GetAllByCurrencyId(Currency.Id, Arg.Any<CancellationToken>()).Returns(new List<CurrencyRate> { initial, dated });
		Currency survivor = AddCurrency("EUR", 9);
		survivor.ModificationType = ModificationType.Content;
		Currency deleted = AddCurrency("JPY", 12);
		deleted.DeleteRevision = 0;

		await Service.Delete(Currency.Id);

		Assert.That(Currency.DeleteRevision, Is.Zero);
		Assert.That(Currency.EditRevision, Is.EqualTo(revision));
		Assert.That(Currency.ModificationType, Is.EqualTo(flags));
		Assert.That(initial.DeleteRevision, Is.Zero);
		Assert.That(initial.ModificationType, Is.EqualTo(ModificationType.None));
		Assert.That((dated.DeleteRevision, dated.EditRevision, dated.Rate), Is.EqualTo(((long?)0, revision, 2m)));
		Assert.That(Currency.Rates.Single(), Is.SameAs(initial));
		Assert.That(survivor.Order, Is.EqualTo(1));
		Assert.That(survivor.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Assert.That(deleted.Order, Is.EqualTo(12));
		Unit.CurrencyRateRepo.Received(1).Update(initial);
		Unit.CurrencyRateRepo.Received(1).Update(dated);
		Unit.SystemConfigRepo.DidNotReceiveWithAnyArgs().Update(default!);
		await Unit.Received(1).SaveChanges();
	}

	[Test]
	public void Delete_RejectsBaseCurrency()
	{
		Assert.ThrowsAsync<InvalidCurrencyException>(async () => await Service.Delete(BaseCurrency.Id));
		Assert.That(BaseCurrency.DeleteRevision, Is.Null);
		AssertNoWrites();
	}

	[Test]
	public void Delete_RejectsReferencedCurrency()
	{
		Unit.AccountRepo.HasByCurrencyId(Currency.Id, Arg.Any<CancellationToken>()).Returns(true);
		Assert.ThrowsAsync<InvalidCurrencyException>(async () => await Service.Delete(Currency.Id));
		Assert.That(Currency.DeleteRevision, Is.Null);
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Delete_RejectsMissingOrDeletedCurrency(bool deleted)
	{
		if (deleted) { Currency.DeleteRevision = 0; }
		Assert.ThrowsAsync<CurrencyNotFoundException>(async () => await Service.Delete(deleted ? Currency.Id : Guid.NewGuid()));
		AssertNoWrites();
	}

	[Test]
	public async Task Delete_ForwardsCancellation()
	{
		using CancellationTokenSource source = new();
		await Service.Delete(Currency.Id, source.Token);
		await Unit.CurrencyRepo.Received(1).GetById(Currency.Id, source.Token);
		await Unit.AccountRepo.Received(1).HasByCurrencyId(Currency.Id, source.Token);
		await Unit.CurrencyRepo.Received(1).GetAll(source.Token);
		await Unit.Received(1).SaveChanges(source.Token);
	}
}
