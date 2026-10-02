using System.Globalization;
using Business.Contracts.Services.Templates;
using Business.Models.Entities;
using Business.Models.Enums;
using Business.Models.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace ServiceTests.Business.Services.Templates;

/// <summary>
/// Verifies the template MoveToAnotherGroup operation through its public contract.
/// Resolves the service through the production business registrations.
/// Replaces repositories with isolated test substitutes.
/// Checks required lifecycle and catalog state changes.
/// Preserves unrelated synchronization and aggregate entry state.
/// Covers rejected requests and unchanged operations.
/// Ensures successful changes commit once.
/// Invalid and unsupported operations leave persistence untouched.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class TemplatesMoveToAnotherGroupServiceTests : TemplatesServiceTestsBase
{

	[Test]
	public async Task MoveToAnotherGroup_NormalizesBothGroupsAndKeepsEntries()
	{
		Template sourceSibling = AddSibling("Source sibling", 4);
		TemplateGroup destination = new() { Id = Guid.NewGuid(), Name = "Destination" };
		Template destinationSibling = new() { Id = Guid.NewGuid(), Name = "Destination sibling", Order = 7 };
		destination.Elements.Add(destinationSibling);
		GroupRepository.GetWithContentsByIdAsync(destination.Id, Arg.Any<CancellationToken>()).Returns(destination);
		Template.Entries = Entries;

		await Service.MoveToAnotherGroup(Template.Id, destination.Id);

		Assert.That(Template.GroupId, Is.EqualTo(destination.Id));
		Assert.That(Template.Group, Is.SameAs(destination));
		Assert.That(Template.Order, Is.EqualTo(1));
		Assert.That(Template.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Assert.That(sourceSibling.Order, Is.Zero);
		Assert.That(destinationSibling.Order, Is.Zero);
		Assert.That(Template.Entries, Is.SameAs(Entries));
		EntryRepository.DidNotReceiveWithAnyArgs().RemoveRange(default!);
		await UnitOfWork.Received(1).SaveChangesAsync();
	}

	[Test]
	public async Task MoveToAnotherGroup_SameGroupDoesNotCommit()
	{
		await Service.MoveToAnotherGroup(Template.Id, Group.Id);
		AssertNoWrites();
	}

	[Test]
	public void MoveToAnotherGroup_RejectsDuplicateNameBeforeChanges()
	{
		TemplateGroup destination = new() { Id = Guid.NewGuid(), Name = "Destination" };
		destination.Elements.Add(new Template { Name = " existing " });
		GroupRepository.GetWithContentsByIdAsync(destination.Id, Arg.Any<CancellationToken>()).Returns(destination);
		Assert.ThrowsAsync<InvalidElementException>(async () => await Service.MoveToAnotherGroup(Template.Id, destination.Id));
		Assert.That(Template.GroupId, Is.EqualTo(Group.Id));
		AssertNoWrites();
	}

	[Test]
	public void MoveToAnotherGroup_RejectsDeletedDestination()
	{
		TemplateGroup destination = new() { Id = Guid.NewGuid(), Name = "Destination", DeleteRevision = 0 };
		GroupRepository.GetWithContentsByIdAsync(destination.Id, Arg.Any<CancellationToken>()).Returns(destination);
		Assert.ThrowsAsync<InvalidElementException>(async () => await Service.MoveToAnotherGroup(Template.Id, destination.Id));
		AssertNoWrites();
	}
}