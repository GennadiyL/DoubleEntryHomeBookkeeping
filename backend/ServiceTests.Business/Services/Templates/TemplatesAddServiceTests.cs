using System.Globalization;
using Business.Contracts.Services.Templates;
using Business.Models.Entities;
using Business.Models.Enums;
using Business.Models.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace ServiceTests.Business.Services.Templates;

/// <summary>
/// Verifies template Add through the public service interface.
/// Uses production composition with substituted persistence.
/// Covers aggregate entry handling and parent synchronization state.
/// Checks validation before any mutation or commit.
/// Exercises ordering, identities and detached repository inputs.
/// Confirms precision and reference rules where applicable.
/// Cancellation is forwarded to persistence operations.
/// Database mechanics are verified separately with SQLite.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class TemplatesAddServiceTests : TemplatesServiceTestsBase
{

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(3)]
	public async Task Add_PersistsWholeAggregateWithNewIdentities(int count)
	{
		Group.Elements.Clear();
		for (int index = 0; index < count; index++)
		{
			AddInput(index + 1.235m);
		}
		Template? saved = null;
		Repository.When(repository => repository.Add(Arg.Any<Template>())).Do(call => saved = call.Arg<Template>());

		Guid id = await Service.Add(Param);

		Assert.That(saved, Is.Not.Null);
		Assert.That(saved!.Id, Is.EqualTo(id).And.Not.EqualTo(Guid.Empty));
		Assert.That(saved.Name, Is.EqualTo("New"));
		Assert.That(saved.Description, Is.EqualTo("  Notes  "));
		Assert.That(saved.IsFavorite, Is.True);
		Assert.That(saved.Group, Is.SameAs(Group));
		Assert.That(saved.Order, Is.Zero);
		Assert.That(saved.EditRevision, Is.Null);
		Assert.That(saved.DeleteRevision, Is.Null);
		Assert.That(saved.ModificationType, Is.EqualTo(ModificationType.None));
		Assert.That(saved.Entries, Has.Count.EqualTo(count));
		Assert.That(saved.Entries.Select(entry => entry.Position), Is.EqualTo(Enumerable.Range(0, count)));
		Assert.That(saved.Entries.Select(entry => entry.Id).Distinct().Count(), Is.EqualTo(count));
		Assert.That(saved.Entries.All(entry => entry.Id != Guid.Empty && entry.TemplateId == id &&
			ReferenceEquals(entry.Template, saved) && entry.AccountId == Account.Id), Is.True);
		Assert.That(saved.Entries.Select(entry => entry.Amount), Is.EqualTo(Enumerable.Range(0, count).Select(index => index + 1.24m)));
		EntryRepository.Received(count).Add(Arg.Any<TemplateEntry>());
		await UnitOfWork.Received(1).SaveChangesAsync();
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("  ")]
	public void Add_RejectsBlankName(string? name)
	{
		Param.Name = name!;
		Assert.ThrowsAsync<InvalidElementException>(async () => await Service.Add(Param));
		AssertNoWrites();
	}

	[TestCase(null)]
	[TestCase(0L)]
	[TestCase(12L)]
	public void Add_RejectsDuplicateTrimmedName(long? deleteRevision)
	{
		Template.Name = "  nEW ";
		Template.DeleteRevision = deleteRevision;
		Assert.ThrowsAsync<InvalidElementException>(async () => await Service.Add(Param));
		AssertNoWrites();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Add_RejectsMissingOrDeletedGroup(bool deleted)
	{
		if (deleted) { Group.DeleteRevision = 0; }
		else { Param.GroupId = Guid.NewGuid(); }
		Assert.ThrowsAsync<InvalidElementException>(async () => await Service.Add(Param));
		AssertNoWrites();
	}

	[TestCase("1.225", "1.22")]
	[TestCase("-1.235", "-1.24")]
	[TestCase("0", "0")]
	public async Task Add_RoundsAmountsToEven(string input, string expected)
	{
		AddInput(decimal.Parse(input, CultureInfo.InvariantCulture));
		await Service.Add(Param);
		EntryRepository.Received(1).Add(Arg.Is<TemplateEntry>(entry =>
			entry.Amount == decimal.Parse(expected, CultureInfo.InvariantCulture)));
	}

	[TestCase("922337203685477.5808")]
	[TestCase("-922337203685477.5809")]
	[TestCase("79228162514264337593543950335")]
	public void Add_RejectsStorageOverflowBeforeWrites(string input)
	{
		Config.AmountPrecision = 4;
		AddInput(1m);
		AddInput(decimal.Parse(input, CultureInfo.InvariantCulture));
		Assert.ThrowsAsync<InvalidElementException>(async () => await Service.Add(Param));
		AssertNoWrites();
	}

	[TestCase("922337203685477.5807")]
	[TestCase("-922337203685477.5808")]
	public async Task Add_AcceptsStorageBoundary(string input)
	{
		Config.AmountPrecision = 4;
		AddInput(decimal.Parse(input, CultureInfo.InvariantCulture));
		await Service.Add(Param);
		await UnitOfWork.Received(1).SaveChangesAsync();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Add_RejectsMissingOrDeletedAccount(bool deleted)
	{
		AddInput(10m);
		if (deleted) { Account.DeleteRevision = 0; }
		else { Param.Entries[0].AccountId = Guid.NewGuid(); }
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await Service.Add(Param));
		AssertNoWrites();
	}

	[Test]
	public void Add_RejectsEmptyAccountAndNullEntry()
	{
		Param.Entries.Add(new TemplateEntryParam());
		Assert.ThrowsAsync<InvalidElementException>(async () => await Service.Add(Param));
		Param.Entries.Clear();
		Param.Entries.Add(null!);
		Assert.ThrowsAsync<InvalidElementException>(async () => await Service.Add(Param));
		AssertNoWrites();
	}

	[Test]
	public async Task Add_AppendsAfterLiveSiblings()
	{
		Template.Order = 4;
		AddSibling("Deleted", int.MaxValue).DeleteRevision = 0;
		await Service.Add(Param);
		Repository.Received(1).Add(Arg.Is<Template>(item => item.Order == 5));
	}

	[Test]
	public void Add_RejectsOrderOverflow()
	{
		Template.Order = int.MaxValue;
		Assert.ThrowsAsync<InvalidElementException>(async () => await Service.Add(Param));
		AssertNoWrites();
	}

	[TestCase(-1)]
	[TestCase(5)]
	public void Add_RejectsInvalidPrecision(int precision)
	{
		Config.AmountPrecision = precision;
		Assert.ThrowsAsync<InvalidOperationException>(async () => await Service.Add(Param));
		AssertNoWrites();
	}

	[Test]
	public async Task Add_ForwardsCancellation()
	{
		using CancellationTokenSource source = new();
		AddInput(1m);
		await Service.Add(Param, source.Token);
		await GroupRepository.Received(1).GetWithContentsByIdAsync(Group.Id, source.Token);
		await UnitOfWork.SystemConfigRepo.Received(1).GetAllAsync(source.Token);
		await UnitOfWork.AccountRepo.Received(1).GetByIdAsync(Account.Id, source.Token);
		await UnitOfWork.Received(1).SaveChangesAsync(source.Token);
	}
}