using Business.Impl.Operations.Config;
using Business.Contracts.Services.Configs;
using Business.Models.Entities.Config;
using Business.Models.Enums;
using DataAccess.Contracts;
using NSubstitute;
using NUnit.Framework;

namespace UnitTests.Business.Operations.Config;

/// <summary>
/// Verifies read-only access to Master-owned System configuration.
/// Uses substituted repositories and the production configuration operation.
/// Reads preserve the stored identity, initialization settings and revision.
/// Returned values are detached from the stored singleton.
/// Balancing-account lookup is unnecessary because the account identity is fixed.
/// Invalid revision metadata is rejected without writing defaults.
/// Local naming settings remain part of the combined configuration result.
/// No configuration read may update a repository or commit changes.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class ConfigOperationTests
{
	[Test]
	public async Task SystemRead_PreservesRevisionAndDoesNotReadBalancingAccount()
	{
		IAppUnitOfWork unit = Substitute.For<IAppUnitOfWork>();
		SystemConfig stored = new()
		{
			Id = Guid.NewGuid(), BaseCurrencyId = Guid.NewGuid(), MasterDatasetKey = "dataset",
			AmountPrecision = 3, RatePrecision = 4, CurrentRevision = long.MaxValue
		};
		unit.SystemConfigRepo.GetAll(Arg.Any<CancellationToken>()).Returns(new List<SystemConfig> { stored });
		ConfigOperation operation = new(unit);
		using CancellationTokenSource cancellation = new();
		SystemConfig result = await operation.GetSystemConfig(cancellation.Token);
		Assert.That(result, Is.Not.SameAs(stored));
		Assert.That((result.Id, result.MasterDatasetKey, result.BaseCurrencyId, result.AmountPrecision,
			result.RatePrecision, result.CurrentRevision),
			Is.EqualTo((stored.Id, stored.MasterDatasetKey, stored.BaseCurrencyId, stored.AmountPrecision,
			stored.RatePrecision, stored.CurrentRevision)));
		result.CurrentRevision = 0;
		Assert.That(stored.CurrentRevision, Is.EqualTo(long.MaxValue));
		await unit.SystemConfigRepo.Received(1).GetAll(cancellation.Token);
		await unit.AccountRepo.DidNotReceiveWithAnyArgs().GetById(default, default);
		unit.SystemConfigRepo.DidNotReceiveWithAnyArgs().Update(default!);
		await unit.DidNotReceiveWithAnyArgs().SaveChanges(default);
	}

	[Test]
	public void SystemRead_RejectsNegativeRevision()
	{
		IAppUnitOfWork unit = Substitute.For<IAppUnitOfWork>();
		unit.SystemConfigRepo.GetAll().Returns(new List<SystemConfig> { new() { BaseCurrencyId = Guid.NewGuid(), CurrentRevision = -1 } });
		Assert.ThrowsAsync<InvalidOperationException>(async () => await new ConfigOperation(unit).GetSystemConfig());
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
