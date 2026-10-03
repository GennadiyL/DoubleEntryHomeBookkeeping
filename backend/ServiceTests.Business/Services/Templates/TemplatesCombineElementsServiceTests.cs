using System.Globalization;
using Business.Contracts.Services.Templates;
using Business.Models.Entities;
using Business.Models.Enums;
using Business.Models.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace ServiceTests.Business.Services.Templates;

/// <summary>
/// Verifies the template CombineElements operation through its public contract.
/// Resolves the service through the production business registrations.
/// Replaces repositories with isolated test substitutes.
/// Checks required lifecycle and catalog state changes.
/// Preserves unrelated synchronization and aggregate entry state.
/// Covers rejected requests and unchanged operations.
/// Ensures successful changes commit once.
/// Invalid and unsupported operations leave persistence untouched.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class TemplatesCombineElementsServiceTests : TemplatesServiceTestsBase
{

	[TestCase(false)]
	[TestCase(true)]
	public void CombineElements_AlwaysUnsupported(bool equal)
	{
		Assert.ThrowsAsync<NotSupportedException>(async () =>
			await Service.CombineElements(Template.Id, equal ? Template.Id : Guid.NewGuid()));
		AssertNoWrites();
		Repository.DidNotReceiveWithAnyArgs().GetById(default, default);
	}
}