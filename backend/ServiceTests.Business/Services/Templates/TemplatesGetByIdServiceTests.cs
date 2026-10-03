using System.Globalization;
using Business.Contracts.Services.Templates;
using Business.Models.Entities;
using Business.Models.Enums;
using Business.Models.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace ServiceTests.Business.Services.Templates;

/// <summary>
/// Verifies template GetById through the public service contract.
/// Uses production service composition and substituted repositories.
/// Builds detached projections from explicitly loaded entries.
/// Checks copied values and persisted entry ordering.
/// Covers missing or deleted source data.
/// Confirms previews neither mutate nor save aggregates.
/// Forwards cancellation to the required repository reads.
/// Editor changes to results cannot alter their source values.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class TemplatesGetByIdServiceTests : TemplatesServiceTestsBase
{

	[Test]
	public async Task GetById_ReturnsDetachedEditorValuesInPositionOrder()
	{
		Entries[0].Position = 1;
		Entries.Add(new TemplateEntry { Id = Guid.NewGuid(), Template = Template, TemplateId = Template.Id,
			Account = Account, AccountId = Account.Id, Amount = -3m, Position = 0 });

		TemplateInfo result = await Service.GetById(Template.Id);

		Assert.That(result.Id, Is.EqualTo(Template.Id));
		Assert.That(result.GroupName, Is.EqualTo(Group.Name));
		Assert.That(result.Name, Is.EqualTo(Template.Name));
		Assert.That(result.Description, Is.EqualTo(Template.Description));
		Assert.That(result.Entries.Select(entry => entry.Amount), Is.EqualTo(new[] { -3m, 10m }));
		Assert.That(result.Entries.All(entry => entry.AccountName == "Cash" &&
			entry.CurrencyId == Currency.Id && entry.CurrencyName == "Dollar"), Is.True);
		result.Entries[0].Amount = 99;
		Assert.That(Entries[1].Amount, Is.EqualTo(-3m));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void GetById_RejectsMissingOrDeletedTemplate(bool deleted)
	{
		if (deleted) { Template.DeleteRevision = 0; }
		Assert.ThrowsAsync<ElementNotFoundException>(async () =>
			await Service.GetById(deleted ? Template.Id : Guid.NewGuid()));
		AssertNoWrites();
	}

	[Test]
	public void GetById_RejectsDeletedCurrency()
	{
		Currency.DeleteRevision = 0;
		Assert.ThrowsAsync<CurrencyNotFoundException>(async () => await Service.GetById(Template.Id));
		AssertNoWrites();
	}

	[Test]
	public async Task GetById_ForwardsCancellation()
	{
		using CancellationTokenSource source = new();
		await Service.GetById(Template.Id, source.Token);
		await Repository.Received(1).GetById(Template.Id, source.Token);
		await EntryRepository.Received(1).GetByTemplateId(Template.Id, source.Token);
		await UnitOfWork.AccountRepo.Received(1).GetById(Account.Id, source.Token);
		await UnitOfWork.CurrencyRepo.Received(1).GetById(Currency.Id, source.Token);
		AssertNoWrites();
	}
}