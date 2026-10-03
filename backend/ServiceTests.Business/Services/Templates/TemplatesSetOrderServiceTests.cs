using System.Globalization;
using Business.Contracts.Services.Templates;
using Business.Models.Entities;
using Business.Models.Enums;
using Business.Models.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace ServiceTests.Business.Services.Templates;

/// <summary>
/// Verifies the template SetOrder operation through its public contract.
/// Resolves the service through the production business registrations.
/// Replaces repositories with isolated test substitutes.
/// Checks required lifecycle and catalog state changes.
/// Preserves unrelated synchronization and aggregate entry state.
/// Covers rejected requests and unchanged operations.
/// Ensures successful changes commit once.
/// Invalid and unsupported operations leave persistence untouched.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class TemplatesSetOrderServiceTests : TemplatesServiceTestsBase
{

	[Test]
	public async Task SetOrder_NormalizesLiveSiblingsAndPreservesContentFlags()
	{
		Template.Order = 5;
		Template.ModificationType = ModificationType.Content;
		Template other = AddSibling("Other", 9);
		Template deleted = AddSibling("Deleted", 1);
		deleted.DeleteRevision = 0;

		await Service.SetOrder(other.Id, 0);

		Assert.That(other.Order, Is.Zero);
		Assert.That(Template.Order, Is.EqualTo(1));
		Assert.That(deleted.Order, Is.EqualTo(1));
		Assert.That(Template.ModificationType, Is.EqualTo(ModificationType.Content | ModificationType.Order));
		Assert.That(other.ModificationType, Is.EqualTo(ModificationType.Order));
		await UnitOfWork.Received(1).SaveChanges();
	}

	[Test]
	public async Task SetOrder_UnchangedOrderDoesNotCommit()
	{
		await Service.SetOrder(Template.Id, 0);
		AssertNoWrites();
	}

	[TestCase(-1)]
	[TestCase(1)]
	public void SetOrder_RejectsOutOfRange(int order)
	{
		Assert.ThrowsAsync<InvalidElementException>(async () => await Service.SetOrder(Template.Id, order));
		AssertNoWrites();
	}

	[Test]
	public async Task SetOrder_UsesCanonicalGuidForTies()
	{
		Template other = AddSibling("Other", 0);
		Template first = new[] { Template, other }.OrderBy(item => item.Id.ToString("D"), StringComparer.Ordinal).First();
		Template second = first == Template ? other : Template;

		await Service.SetOrder(first.Id, 0);

		Assert.That(first.Order, Is.Zero);
		Assert.That(second.Order, Is.EqualTo(1));
		Repository.Received(1).Update(second);
	}

	[Test]
	public void SetOrder_RejectsDeletedTemplate()
	{
		Template.DeleteRevision = 0;
		Assert.ThrowsAsync<ElementNotFoundException>(async () => await Service.SetOrder(Template.Id, 0));
		AssertNoWrites();
	}
}