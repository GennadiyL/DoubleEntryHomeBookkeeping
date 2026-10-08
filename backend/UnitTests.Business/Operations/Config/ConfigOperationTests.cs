using Business.Impl.Operations.Config;
using Business.Contracts.Services.Configs;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using Business.Models.Enums;
using DataAccess.Contracts;
using NSubstitute;
using NUnit.Framework;

namespace UnitTests.Business.Operations.Config;

/// <summary>
/// Verifies read-only handling of the balancing-account selection.
/// Exercises the shared operation with substituted repositories.
/// Live accounts remain selected in returned configuration.
/// Missing and deleted accounts produce an absent returned selection.
/// Stored configuration values and synchronization flags stay unchanged.
/// Combined reads use the same account existence rules.
/// Cancellation reaches the selected account lookup.
/// No configuration update or commit is permitted.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class ConfigOperationTests
{
	[TestCase("live", false)]
	[TestCase("missing", false)]
	[TestCase("deleted", false)]
	[TestCase("null", false)]
	[TestCase("live", true)]
	[TestCase("missing", true)]
	[TestCase("deleted", true)]
	[TestCase("null", true)]
	public async Task Read_ResolvesSelectionWithoutMutatingConfiguration(string state, bool combined)
	{
		IAppUnitOfWork unit = Substitute.For<IAppUnitOfWork>();
		Guid? selectedId = state == "null" ? null : Guid.NewGuid();
		SystemConfig stored = new()
		{
			Id = Guid.NewGuid(), BaseCurrencyId = Guid.NewGuid(), BalancingAccountId = selectedId,
			MasterDatasetKey = "dataset", AmountPrecision = 3, RatePrecision = 4,
			EditRevision = 7, ModificationType = ModificationType.Order
		};
		unit.SystemConfigRepo.GetAll(Arg.Any<CancellationToken>()).Returns(new List<SystemConfig> { stored });
		unit.LocalConfigRepo.GetAll(Arg.Any<CancellationToken>()).Returns(new List<LocalConfig> { new() });
		if (state is "live" or "deleted")
		{
			unit.AccountRepo.GetById(selectedId!.Value, Arg.Any<CancellationToken>())
				.Returns(new Account { Id = selectedId.Value, DeleteRevision = state == "deleted" ? 0L : null });
		}
		ConfigOperation operation = new(unit);
		using CancellationTokenSource source = new();
		Guid? returnedId;
		if (combined)
		{
			returnedId = (await operation.GetConfiguration(source.Token)).BalancingAccountId;
		}
		else
		{
			SystemConfig result = await operation.GetSystemConfig(source.Token);
			returnedId = result.BalancingAccountId;
			Assert.That(result, Is.Not.SameAs(stored));
			Assert.That((result.Id, result.MasterDatasetKey, result.BaseCurrencyId, result.AmountPrecision,
				result.RatePrecision, result.EditRevision, result.DeleteRevision, result.ModificationType),
				Is.EqualTo((stored.Id, stored.MasterDatasetKey, stored.BaseCurrencyId, stored.AmountPrecision,
				stored.RatePrecision, stored.EditRevision, stored.DeleteRevision, stored.ModificationType)));
		}
		Assert.That(returnedId, Is.EqualTo(state == "live" ? selectedId : null));
		Assert.That(stored.BalancingAccountId, Is.EqualTo(selectedId));
		Assert.That(stored.EditRevision, Is.EqualTo(7));
		Assert.That(stored.ModificationType, Is.EqualTo(ModificationType.Order));
		if (selectedId.HasValue)
		{
			await unit.AccountRepo.Received(1).GetById(selectedId.Value, source.Token);
		}
		else
		{
			await unit.AccountRepo.DidNotReceiveWithAnyArgs().GetById(default, default);
		}
		unit.SystemConfigRepo.DidNotReceiveWithAnyArgs().Update(default!);
		unit.SystemConfigRepo.DidNotReceiveWithAnyArgs().Add(default!);
		await unit.DidNotReceiveWithAnyArgs().SaveChanges(default);
	}
	[Test]
	public async Task CombinedRead_ContainsCurrentLocalNamingSettings()
	{
		IAppUnitOfWork unit = Substitute.For<IAppUnitOfWork>();
		unit.SystemConfigRepo.GetAll().Returns(new List<SystemConfig> { new() { BaseCurrencyId = Guid.NewGuid() } });
		LocalConfig local = new() { AccountNameSeparator = "|", AccountNameOrder = AccountNameOrder.ProjectCategoryCorrespondent, AccountNameAddCurrency = true };
		unit.LocalConfigRepo.GetAll().Returns(new List<LocalConfig> { local });
		ConfigOperation operation = new(unit);
		ConfigurationInfo result = await operation.GetConfiguration();
		Assert.That((result.AccountNameOrder, result.AccountNameSeparator, result.AccountNameAddCurrency),
			Is.EqualTo((local.AccountNameOrder, "|", true)));
		local.AccountNameAddCurrency = false;
		Assert.That(result.AccountNameAddCurrency, Is.True);
		Assert.That((await operation.GetConfiguration()).AccountNameAddCurrency, Is.False);
		await unit.DidNotReceiveWithAnyArgs().SaveChanges(default);
	}
}
