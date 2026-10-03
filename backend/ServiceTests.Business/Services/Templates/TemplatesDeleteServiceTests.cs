using System.Globalization;
using Business.Contracts.Services.Templates;
using Business.Models.Entities;
using Business.Models.Enums;
using Business.Models.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace ServiceTests.Business.Services.Templates;

/// <summary>
/// Verifies the template Delete operation through its public contract.
/// Resolves the service through the production business registrations.
/// Replaces repositories with isolated test substitutes.
/// Checks required lifecycle and catalog state changes.
/// Preserves unrelated synchronization and aggregate entry state.
/// Covers rejected requests and unchanged operations.
/// Ensures successful changes commit once.
/// Invalid and unsupported operations leave persistence untouched.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class TemplatesDeleteServiceTests : TemplatesServiceTestsBase
{

	[TestCase(null, ModificationType.None)]
	[TestCase(7L, ModificationType.Order)]
	[TestCase(7L, ModificationType.Content)]
	public async Task Delete_SoftDeletesPreservingFlagsAndEntries(long? revision, ModificationType flags)
	{
		Template.EditRevision = revision;
		Template.ModificationType = flags;
		Template.Entries = Entries;
		Template sibling = AddSibling("Sibling", 4);
		Template deleted = AddSibling("Deleted", 5);
		deleted.DeleteRevision = 0;

		await Service.Delete(Template.Id);

		Assert.That(Template.DeleteRevision, Is.Zero);
		Assert.That(Template.EditRevision, Is.EqualTo(revision));
		Assert.That(Template.ModificationType, Is.EqualTo(flags));
		Assert.That(Template.Entries, Is.SameAs(Entries));
		Assert.That(sibling.Order, Is.Zero);
		Assert.That(sibling.ModificationType, Is.EqualTo(ModificationType.Order));
		Assert.That(deleted.Order, Is.EqualTo(5));
		Repository.Received(1).Update(Template);
		Repository.Received(1).Update(sibling);
		EntryRepository.DidNotReceiveWithAnyArgs().RemoveRange(default!);
		await UnitOfWork.Received(1).SaveChanges();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Delete_RejectsMissingOrDeletedTarget(bool deleted)
	{
		if (deleted) { Template.DeleteRevision = 0; }
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await Service.Delete(deleted ? Template.Id : Guid.NewGuid()));
		AssertNoWrites();
	}

	[Test]
	public void Delete_MissingGroupLeavesTargetUnchanged()
	{
		Template.GroupId = Guid.NewGuid();
		Assert.ThrowsAsync<GroupNotFoundException>(async () => await Service.Delete(Template.Id));
		Assert.That(Template.DeleteRevision, Is.Null);
		AssertNoWrites();
	}
}