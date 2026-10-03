using System.Globalization;
using Business.Contracts.Services.Templates;
using Business.Models.Entities;
using Business.Models.Enums;
using Business.Models.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace ServiceTests.Business.Services.Templates;

/// <summary>
/// Verifies the template SetFavoriteStatus operation through its public contract.
/// Resolves the service through the production business registrations.
/// Replaces repositories with isolated test substitutes.
/// Checks required lifecycle and catalog state changes.
/// Preserves unrelated synchronization and aggregate entry state.
/// Covers rejected requests and unchanged operations.
/// Ensures successful changes commit once.
/// Invalid and unsupported operations leave persistence untouched.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class TemplatesSetFavoriteStatusServiceTests : TemplatesServiceTestsBase
{

	[Test]
	public async Task SetFavoriteStatus_ChangesContentAndPreservesOrder()
	{
		Template.ModificationType = ModificationType.Order;
		await Service.SetFavoriteStatus(Template.Id, true);
		Assert.That(Template.IsFavorite, Is.True);
		Assert.That(Template.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Assert.That(Template.EditRevision, Is.EqualTo(7));
		Repository.Received(1).Update(Template);
		await UnitOfWork.Received(1).SaveChanges();
	}

	[Test]
	public async Task SetFavoriteStatus_UnchangedDoesNotCommit()
	{
		await Service.SetFavoriteStatus(Template.Id, false);
		AssertNoWrites();
	}

	[Test]
	public void SetFavoriteStatus_RejectsDeletedTemplate()
	{
		Template.DeleteRevision = 0;
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await Service.SetFavoriteStatus(Template.Id, true));
		AssertNoWrites();
	}
}