using Business.Models.Constants;
using Business.Models.Entities.Synchronizations;
using System.Reflection;
using Business.Models.Entities.Config;
using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
using DataAccess.Core.Entities;
using DataAccess.EntityFramework;
using DataAccess.EntityFramework.SqLite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shared.Impl;

namespace UnitTests.EntityFramework.SqLite;

/// <summary>
/// Verifies Local and Master persistence inside one dependency container.
/// Two isolated SQLite databases use the production schema and seed scripts.
/// Tests resolve both typed units of work in the same dependency scope.
/// Repository writes and transactions must remain bound to their own context.
/// Registration order must not change the legacy Local service aliases.
/// Each module must also function without registering the other module.
/// Cumulative SQL commands must share the selected role's transaction.
/// No application host or existing user database is opened.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "NUnit TearDown disposes the database lifetime connections.")]
[TestFixture(Category = "Local")]
public sealed class DatabaseRoleTests
{
	private SqliteConnection _local = null!;
	private SqliteConnection _master = null!;
	private IConfiguration _configuration = null!;

	[SetUp]
	public async Task SetUp()
	{
		_local = new SqliteConnection($"Data Source=local-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Pooling=False");
		_master = new SqliteConnection($"Data Source=master-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Pooling=False");
		foreach (SqliteConnection connection in new[] { _local, _master })
		{
			await connection.OpenAsync();
			foreach (string script in new[] { "001-schema.sql", "002-initial-data.sql", "003-test-data.sql" })
			{
				await using SqliteCommand command = connection.CreateCommand();
				command.CommandText = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "SqlAssets", script));
				await command.ExecuteNonQueryAsync();
			}
		}
		_configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			["ConnectionStrings:DehbSqLite"] = _local.ConnectionString,
			["ConnectionStrings:DehbMasterSqLite"] = _master.ConnectionString
		}).Build();
	}

	[TearDown]
	public void TearDown()
	{
		_master?.Dispose();
		_local?.Dispose();
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task BothModules_KeepRepositoriesAndSavesIndependent(bool masterFirst)
	{
		using ServiceProvider provider = BuildProvider(masterFirst);
		using IServiceScope scope = provider.CreateScope();
		ILocalAppUnitOfWork local = scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>();
		IMasterAppUnitOfWork master = scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>();
		Assert.That(scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>(), Is.SameAs(local));
		Assert.That(scope.ServiceProvider.GetRequiredService<AppDbContext>(),
			Is.SameAs(scope.ServiceProvider.GetRequiredService<LocalAppDbContext>()));
		Assert.That(scope.ServiceProvider.GetRequiredKeyedService<AppDbContext>(typeof(IMasterAppUnitOfWork)),
			Is.SameAs(scope.ServiceProvider.GetRequiredService<MasterAppDbContext>()));

		foreach (PropertyInfo property in typeof(IAppUnitOfWork).GetProperties())
		{
			object localRepository = property.GetValue(local)!;
			object masterRepository = property.GetValue(master)!;
			Assert.That(localRepository, Is.Not.SameAs(masterRepository), property.Name);
			Assert.That(property.GetValue(local), Is.SameAs(localRepository), property.Name);
			Assert.That(property.GetValue(master), Is.SameAs(masterRepository), property.Name);
			Assert.That(scope.ServiceProvider.GetRequiredService(property.PropertyType), Is.SameAs(localRepository), property.Name);
		}

		await UpdateRevision(local, 12);
		await UpdateRevision(master, 34);
		await local.SaveChanges();
		Assert.That(await ReadRevision(_local), Is.EqualTo(12));
		Assert.That(await ReadRevision(_master), Is.Zero);
		await master.SaveChanges();
		Assert.That(await ReadRevision(_master), Is.EqualTo(34));
		Assert.That(await ReadRevision(_local), Is.EqualTo(12));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task EachModule_WorksIndependently(bool masterOnly)
	{
		ServiceCollection services = new();
		services.AddSharedModule();
		if (masterOnly) { services.AddMasterDataAccessSqLiteModule(_configuration); }
		else { services.AddLocalDataAccessSqLiteModule(_configuration); }
		using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
		using IServiceScope scope = provider.CreateScope();
		IAppUnitOfWork unit = masterOnly
			? scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>()
			: scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>();
		await UpdateRevision(unit, 7);
		await unit.SaveChanges();
		Assert.That(await ReadRevision(masterOnly ? _master : _local), Is.EqualTo(7));
		Assert.That(await ReadRevision(masterOnly ? _local : _master), Is.Zero);
		if (masterOnly)
		{
			Assert.That(scope.ServiceProvider.GetService<ILocalAppUnitOfWork>(), Is.Null);
			Assert.That(scope.ServiceProvider.GetService<IAppUnitOfWork>(), Is.Null);
			Assert.That(scope.ServiceProvider.GetService<AppDbContext>(), Is.Null);
		}
		else
		{
			Assert.That(scope.ServiceProvider.GetService<IMasterAppUnitOfWork>(), Is.Null);
		}
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task Rollback_DoesNotUndoOtherDatabaseCommit(bool rollbackMaster)
	{
		using ServiceProvider provider = BuildProvider();
		using IServiceScope scope = provider.CreateScope();
		ILocalAppUnitOfWork local = scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>();
		IMasterAppUnitOfWork master = scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>();
		IAppUnitOfWork rolledBack = rollbackMaster ? master : local;
		IAppUnitOfWork committed = rollbackMaster ? local : master;
		await using IUnitOfWorkTransaction transaction = await rolledBack.BeginTransaction();
		await UpdateRevision(rolledBack, 12);
		await rolledBack.SaveChanges();
		await UpdateRevision(committed, 34);
		await committed.SaveChanges();
		Assert.ThrowsAsync<ArgumentException>(async () => await committed.CommitTransaction(transaction));
		await rolledBack.RollbackTransaction(transaction);

		Assert.That(await ReadRevision(rollbackMaster ? _master : _local), Is.Zero);
		Assert.That(await ReadRevision(rollbackMaster ? _local : _master), Is.EqualTo(34));
	}

	[Test]
	public void ScopeDisposal_ReleasesBothContextsAndNextScopeCreatesNewInstances()
	{
		using ServiceProvider provider = BuildProvider();
		LocalAppDbContext local;
		MasterAppDbContext master;
		using (IServiceScope scope = provider.CreateScope())
		{
			local = scope.ServiceProvider.GetRequiredService<LocalAppDbContext>();
			master = scope.ServiceProvider.GetRequiredService<MasterAppDbContext>();
			scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>();
			scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>();
		}
		Assert.ThrowsAsync<ObjectDisposedException>(async () => await local.SaveChangesAsync());
		Assert.ThrowsAsync<ObjectDisposedException>(async () => await master.SaveChangesAsync());
		using IServiceScope next = provider.CreateScope();
		Assert.That(next.ServiceProvider.GetRequiredService<LocalAppDbContext>(), Is.Not.SameAs(local));
		Assert.That(next.ServiceProvider.GetRequiredService<MasterAppDbContext>(), Is.Not.SameAs(master));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task CumulativeCommand_UsesSelectedDatabaseAndTransaction(bool useMaster)
	{
		using ServiceProvider provider = BuildProvider();
		using IServiceScope scope = provider.CreateScope();
		Type key = useMaster ? typeof(IMasterAppUnitOfWork) : typeof(ILocalAppUnitOfWork);
		IAppUnitOfWork unit = useMaster
			? scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>()
			: scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>();
		Guid accountId = (await unit.AccountRepo.GetAll()).Single(account => account.Name == "Cash UAH").Id;
		foreach (SqliteConnection connection in new[] { _local, _master })
		{
			await using SqliteCommand reset = connection.CreateCommand();
			reset.CommandText = "UPDATE TransactionEntry SET CumulativeAmount = 0;";
			await reset.ExecuteNonQueryAsync();
		}
		ICumulativeAmountCommand command = scope.ServiceProvider.GetRequiredKeyedService<ICumulativeAmountCommand>(key);
		if (!useMaster)
		{
			Assert.That(scope.ServiceProvider.GetRequiredService<ICumulativeAmountCommand>(), Is.SameAs(command));
		}
		await using IUnitOfWorkTransaction transaction = await unit.BeginTransaction();
		await command.Recalculate(accountId);
		Assert.That(await ReadCumulativeCount(useMaster ? _local : _master), Is.Zero);
		await unit.RollbackTransaction(transaction);
		Assert.That(await ReadCumulativeCount(useMaster ? _master : _local), Is.Zero);

		using IServiceScope next = provider.CreateScope();
		await next.ServiceProvider.GetRequiredKeyedService<ICumulativeAmountCommand>(key).Recalculate(accountId);
		Assert.That(await ReadCumulativeCount(useMaster ? _master : _local), Is.GreaterThan(0));
		Assert.That(await ReadCumulativeCount(useMaster ? _local : _master), Is.Zero);
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public async Task Save_AllowsAnotherDetachedUpdateInSameScope(bool useMaster, bool synchronous)
	{
		using ServiceProvider provider = BuildProvider();
		using IServiceScope scope = provider.CreateScope();
		IAppUnitOfWork unit = useMaster
			? scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>()
			: scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>();
		AppDbContext context = useMaster
			? scope.ServiceProvider.GetRequiredService<MasterAppDbContext>()
			: scope.ServiceProvider.GetRequiredService<LocalAppDbContext>();
		foreach (long revision in new long[] { 12, 34 })
		{
			await UpdateRevision(unit, revision);
			if (synchronous) { unit.SaveChangesSync(); }
			else { await unit.SaveChanges(); }
			Assert.That(context.ChangeTracker.Entries(), Is.Empty);
			Assert.That(await ReadRevision(useMaster ? _master : _local), Is.EqualTo(revision));
		}
	}

	[Test]
	public async Task Update_RejectsSecondDetachedInstanceBeforeSave()
	{
		using ServiceProvider provider = BuildProvider();
		using IServiceScope scope = provider.CreateScope();
		ILocalAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>();
		await UpdateRevision(unit, 12);
		Assert.ThrowsAsync<InvalidOperationException>(async () => await UpdateRevision(unit, 34));
		Assert.That(await ReadRevision(_local), Is.Zero);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task FailedSave_PreservesPendingChanges(bool synchronous)
	{
		using ServiceProvider provider = BuildProvider();
		using IServiceScope scope = provider.CreateScope();
		ILocalAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>();
		LocalAppDbContext context = scope.ServiceProvider.GetRequiredService<LocalAppDbContext>();
		SystemConfig config = (await unit.SystemConfigRepo.GetAll()).Single();
		config.BaseCurrencyId = Guid.NewGuid();
		config.CurrentRevision = 12;
		unit.SystemConfigRepo.Update(config);
		if (synchronous) { Assert.Throws<DbUpdateException>(() => unit.SaveChangesSync()); }
		else { Assert.ThrowsAsync<DbUpdateException>(async () => await unit.SaveChanges()); }
		Assert.That(context.ChangeTracker.Entries().Single().State, Is.EqualTo(EntityState.Modified));
		Assert.That(await ReadRevision(_local), Is.Zero);
	}

	[Test]
	public async Task CancelledSave_PreservesChangesForSubsequentSave()
	{
		using ServiceProvider provider = BuildProvider();
		using IServiceScope scope = provider.CreateScope();
		ILocalAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>();
		LocalAppDbContext context = scope.ServiceProvider.GetRequiredService<LocalAppDbContext>();
		await UpdateRevision(unit, 12);
		using CancellationTokenSource cancellation = new();
		await cancellation.CancelAsync();
		Assert.CatchAsync<OperationCanceledException>(async () => await unit.SaveChanges(cancellation.Token));
		Assert.That(context.ChangeTracker.Entries().Single().State, Is.EqualTo(EntityState.Modified));
		Assert.That(await ReadRevision(_local), Is.Zero);
		await unit.SaveChanges();
		Assert.That(await ReadRevision(_local), Is.EqualTo(12));
		Assert.That(context.ChangeTracker.Entries(), Is.Empty);
	}

	[Test]
	public async Task LocalJson_RoundTripsAndRollsBackWithOtherLocalChanges()
	{
		using ServiceProvider provider = BuildProvider();
		using IServiceScope scope = provider.CreateScope();
		ILocalAppUnitOfWork local = scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>();
		IMasterAppUnitOfWork master = scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>();
		const string json = "{\"batches\":[],\"label\":\"Żółć\"}";
		local.LocalJsonRepo.Add(new LocalJson { Json = json });
		await local.SaveChanges();
		LocalJson stored = (await local.LocalJsonRepo.GetById(EntitiesIds.LocalJsonId))!;
		Assert.That(stored.Json, Is.EqualTo(json));
		Assert.That(await master.LocalJsonRepo.GetById(EntitiesIds.LocalJsonId), Is.Null);

		await using (IUnitOfWorkTransaction transaction = await local.BeginTransaction())
		{
			stored.Json = "{\"batches\":[1]}";
			local.LocalJsonRepo.Update(stored);
			await UpdateRevision(local, 12);
			await local.SaveChanges();
			await local.RollbackTransaction(transaction);
		}
		using IServiceScope retryScope = provider.CreateScope();
		local = retryScope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>();
		Assert.That((await local.LocalJsonRepo.GetById(EntitiesIds.LocalJsonId))!.Json, Is.EqualTo(json));
		Assert.That(await ReadRevision(_local), Is.Zero);

		stored.Json = "{\"batches\":[]}";
		local.LocalJsonRepo.Update(stored);
		await local.SaveChanges();
		Assert.That((await local.LocalJsonRepo.GetById(EntitiesIds.LocalJsonId))!.Json, Is.EqualTo(stored.Json));
	}

	private ServiceProvider BuildProvider(bool masterFirst = false)
	{
		ServiceCollection services = new();
		services.AddSharedModule();
		if (masterFirst) { services.AddMasterDataAccessSqLiteModule(_configuration); }
		services.AddLocalDataAccessSqLiteModule(_configuration);
		if (!masterFirst) { services.AddMasterDataAccessSqLiteModule(_configuration); }
		return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
	}

	private static async Task UpdateRevision(IAppUnitOfWork unit, long revision)
	{
		SystemConfig config = (await unit.SystemConfigRepo.GetAll()).Single();
		config.CurrentRevision = revision;
		unit.SystemConfigRepo.Update(config);
	}

	private static async Task<long> ReadRevision(SqliteConnection connection)
	{
		await using SqliteCommand command = connection.CreateCommand();
		command.CommandText = "SELECT CurrentRevision FROM SystemConfig;";
		return (long)(await command.ExecuteScalarAsync())!;
	}

	private static async Task<long> ReadCumulativeCount(SqliteConnection connection)
	{
		await using SqliteCommand command = connection.CreateCommand();
		command.CommandText = "SELECT count(*) FROM TransactionEntry WHERE CumulativeAmount <> 0;";
		return (long)(await command.ExecuteScalarAsync())!;
	}
}
