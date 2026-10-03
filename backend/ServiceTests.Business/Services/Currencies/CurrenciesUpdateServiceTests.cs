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
/// Verifies currency Update through the public service interface.
/// Uses production service and operation registrations.
/// Substitutes database access to isolate business rules.
/// Checks synchronization tracking and atomic commit boundaries.
/// Exercises invalid input without partial mutation.
/// Preserves unrelated currency and rate state.
/// Covers relevant identity, precision and reference rules.
/// Database behavior is checked separately against SQLite.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class CurrenciesUpdateServiceTests : CurrenciesServiceTestsBase
{

	[Test]
	public async Task Update_ChangesOnlyMetadataAndContentTracking()
	{
		Param.Code = " gbp ";
		Currency.ModificationType = ModificationType.Order;
		Currency.IsFavorite = true;
		await Service.Update(Currency.Id, Param);
		Assert.That((Currency.Code, Currency.Name, Currency.Symbol), Is.EqualTo(("GBP", "Euro", "EUR")));
		Assert.That((Currency.Order, Currency.IsFavorite, Currency.EditRevision), Is.EqualTo((1, true, (long?)7)));
		Assert.That(Currency.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Unit.CurrencyRateRepo.DidNotReceiveWithAnyArgs().Update(default!);
		Unit.CurrencyRepo.Received(1).Update(Currency);
		await Unit.Received(1).SaveChanges();
	}

	[Test]
	public void Update_RejectsCodeChangeWithoutMutation()
	{
		Assert.ThrowsAsync<InvalidCurrencyException>(async () => await Service.Update(Currency.Id, Param));
		Assert.That(Currency.Name, Is.EqualTo("GBP"));
		Assert.That(Currency.ModificationType, Is.EqualTo(ModificationType.None));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Update_RejectsMissingOrDeletedTarget(bool deleted)
	{
		if (deleted) { Currency.DeleteRevision = 0; }
		Assert.ThrowsAsync<CurrencyNotFoundException>(async () => await Service.Update(deleted ? Currency.Id : Guid.NewGuid(), Param));
		AssertNoWrites();
	}

	[Test]
	public async Task Update_AllowsBaseCurrencyMetadataEditing()
	{
		Param.Code = BaseCurrency.Code;
		await Service.Update(BaseCurrency.Id, Param);
		Assert.That(BaseCurrency.Name, Is.EqualTo("Euro"));
		Assert.That(Config.BaseCurrencyId, Is.EqualTo(BaseCurrency.Id));
	}
}
