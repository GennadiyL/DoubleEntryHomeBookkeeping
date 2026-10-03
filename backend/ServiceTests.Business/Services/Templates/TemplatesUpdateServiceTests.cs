using System.Globalization;
using Business.Contracts.Services.Templates;
using Business.Models.Entities;
using Business.Models.Enums;
using Business.Models.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace ServiceTests.Business.Services.Templates;

/// <summary>
/// Verifies template Update through the public service interface.
/// Uses production composition with substituted persistence.
/// Covers aggregate entry handling and parent synchronization state.
/// Checks validation before any mutation or commit.
/// Exercises ordering, identities and detached repository inputs.
/// Confirms precision and reference rules where applicable.
/// Cancellation is forwarded to persistence operations.
/// Database mechanics are verified separately with SQLite.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class TemplatesUpdateServiceTests : TemplatesServiceTestsBase
{

	[TestCase(0)]
	[TestCase(2)]
	public async Task Update_ReplacesAllEntriesPreservingParentIdentityAndTracking(int count)
	{
		Template.ModificationType = ModificationType.Order;
		Template.Entries = Entries;
		Guid oldId = Entries[0].Id;
		for (int index = 0; index < count; index++) { AddInput(index - 1.225m); }

		await Service.Update(Template.Id, Param);

		Assert.That(Template.Name, Is.EqualTo("New"));
		Assert.That(Template.Description, Is.EqualTo("  Notes  "));
		Assert.That(Template.IsFavorite, Is.True);
		Assert.That(Template.EditRevision, Is.EqualTo(7));
		Assert.That(Template.DeleteRevision, Is.Null);
		Assert.That(Template.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Assert.That(Template.Entries, Has.Count.EqualTo(count));
		Assert.That(Template.Entries.All(entry => entry.Id != oldId && entry.Id != Guid.Empty &&
			entry.TemplateId == Template.Id && ReferenceEquals(entry.Template, Template)), Is.True);
		Assert.That(Template.Entries.Select(entry => entry.Position), Is.EqualTo(Enumerable.Range(0, count)));
		EntryRepository.Received(1).RemoveRange(Entries);
		EntryRepository.Received(count).Add(Arg.Any<TemplateEntry>());
		Repository.Received(1).Update(Template);
		await UnitOfWork.Received(1).SaveChanges();
	}

	[Test]
	public void Update_InvalidLastEntryLeavesAggregateUntouched()
	{
		Template.Entries = Entries;
		AddInput(10m);
		Param.Entries.Add(new TemplateEntryParam { AccountId = Guid.NewGuid(), Amount = 1m });
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await Service.Update(Template.Id, Param));
		Assert.That(Template.Name, Is.EqualTo("Existing"));
		Assert.That(Template.Entries, Is.SameAs(Entries));
		Assert.That(Template.ModificationType, Is.EqualTo(ModificationType.None));
		AssertNoWrites();
	}

	[Test]
	public void Update_RejectsGroupChangeAndDuplicateName()
	{
		Param.GroupId = Guid.NewGuid();
		Assert.ThrowsAsync<InvalidElementException>(async () => await Service.Update(Template.Id, Param));
		Param.GroupId = Group.Id;
		AddSibling(" new ", 1);
		Assert.ThrowsAsync<InvalidElementException>(async () => await Service.Update(Template.Id, Param));
		AssertNoWrites();
	}

	[Test]
	public async Task Update_AcceptsOwnName()
	{
		Param.Name = " existing ";
		await Service.Update(Template.Id, Param);
		Assert.That(Template.Name, Is.EqualTo("existing"));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Update_RejectsMissingOrDeletedTemplate(bool deleted)
	{
		Guid id = deleted ? Template.Id : Guid.NewGuid();
		if (deleted) { Template.DeleteRevision = 0; }
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await Service.Update(id, Param));
		AssertNoWrites();
	}

	[Test]
	public async Task Update_ForwardsCancellationToReadAndCommit()
	{
		using CancellationTokenSource source = new();
		await Service.Update(Template.Id, Param, source.Token);
		await Repository.Received(1).GetById(Template.Id, source.Token);
		await EntryRepository.Received(1).GetByTemplateId(Template.Id, source.Token);
		await UnitOfWork.Received(1).SaveChanges(source.Token);
	}
}