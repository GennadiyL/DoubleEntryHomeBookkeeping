using System.Globalization;
using Business.Contracts.Services.Templates;
using Business.Models.Entities;
using Business.Models.Enums;
using Business.Models.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace ServiceTests.Business.Services.Templates;

/// <summary>
/// Verifies template ApplyTemplate through the public service contract.
/// Uses production service composition and substituted repositories.
/// Builds detached projections from explicitly loaded entries.
/// Checks copied values and persisted entry ordering.
/// Covers missing or deleted source data.
/// Confirms previews neither mutate nor save aggregates.
/// Forwards cancellation to the required repository reads.
/// Editor changes to results cannot alter their source values.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class TemplatesApplyTemplateServiceTests : TemplatesServiceTestsBase
{

	[Test]
	public async Task ApplyTemplate_ReturnsUnsavedTransactionWithBaseRateOne()
	{
		ApplyTemplateInfo result = await Service.ApplyTemplate(Template.Id);

		Assert.That(result.DateTime, Is.EqualTo(Now));
		Assert.That(result.DateTime.Kind, Is.EqualTo(DateTimeKind.Utc));
		Assert.That(result.Description, Is.EqualTo(Template.Description));
		Assert.That(result.Entries, Has.Count.EqualTo(1));
		Assert.That(result.Entries[0].AccountId, Is.EqualTo(Account.Id));
		Assert.That(result.Entries[0].AccountName, Is.EqualTo(Account.Name));
		Assert.That(result.Entries[0].Amount, Is.EqualTo(10m));
		Assert.That(result.Entries[0].Rate, Is.EqualTo(1m));
		await UnitOfWork.CurrencyRateRepo.DidNotReceiveWithAnyArgs().GetApplicable(default, default, default);
		AssertNoWrites();
	}

	[Test]
	public async Task ApplyTemplate_UsesApplicableRateAndDeviceLocalDate()
	{
		Config.BaseCurrencyId = Guid.NewGuid();
		DateOnly date = DateOnly.FromDateTime(Now.ToLocalTime());
		CurrencyRate rate = new() { Id = Guid.NewGuid(), CurrencyId = Currency.Id, Currency = Currency,
			Date = date.AddDays(-1), Rate = 1.2345m };
		UnitOfWork.CurrencyRateRepo.GetApplicable(Currency.Id, date, Arg.Any<CancellationToken>()).Returns(rate);
		Entries[0].Position = 1;
		Entries.Add(new TemplateEntry { Template = Template, Account = Account, AccountId = Account.Id,
			Amount = -4m, Position = 0 });

		using CancellationTokenSource source = new();
		ApplyTemplateInfo result = await Service.ApplyTemplate(Template.Id, source.Token);

		Assert.That(result.Entries.Select(entry => entry.Amount), Is.EqualTo(new[] { -4m, 10m }));
		Assert.That(result.Entries.All(entry => entry.Rate == rate.Rate), Is.True);
		await UnitOfWork.CurrencyRateRepo.Received(2).GetApplicable(Currency.Id, date, source.Token);
		AssertNoWrites();
	}

	[Test]
	public async Task ApplyTemplate_EmptyTemplateRemainsEmpty()
	{
		Entries.Clear();
		ApplyTemplateInfo result = await Service.ApplyTemplate(Template.Id);
		Assert.That(result.Entries, Is.Empty);
		AssertNoWrites();
	}

	[Test]
	public void ApplyTemplate_MissingRateFailsWithoutSaving()
	{
		Config.BaseCurrencyId = Guid.NewGuid();
		Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.ApplyTemplate(Template.Id));
		AssertNoWrites();
	}

	[TestCase(0)]
	[TestCase(-1)]
	public void ApplyTemplate_InvalidRateFailsWithoutSaving(int value)
	{
		Config.BaseCurrencyId = Guid.NewGuid();
		UnitOfWork.CurrencyRateRepo.GetApplicable(Currency.Id, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
			.Returns(new CurrencyRate { Currency = Currency, CurrencyId = Currency.Id, Rate = value });
		Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.ApplyTemplate(Template.Id));
		AssertNoWrites();
	}

	[Test]
	public void ApplyTemplate_RejectsDeletedTemplate()
	{
		Template.DeleteRevision = 0;
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await Service.ApplyTemplate(Template.Id));
		AssertNoWrites();
	}
}