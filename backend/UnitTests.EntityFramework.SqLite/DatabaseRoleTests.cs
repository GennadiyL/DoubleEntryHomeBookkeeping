using Shared.Contracts.Config;
using Tests.Common.Mocks.Shared;
using Business.Models.Constants;
using Business.Models.Entities.Synchronizations;
using System.Reflection;
using Business.Models.Entities.Config;
using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
using DataAccess.Core.Entities;
using DataAccess.EntityFramework;
using DataAccess.EntityFramework.Contracts;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
/// Factory roles share implementation while retaining independent contexts.
/// Disposal is covered by the dedicated factory tests.
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
	private string _directory = null!;

	[SetUp]
	public async Task SetUp()
	{
		_directory = Directory.CreateTempSubdirectory("dehb-roles-").FullName;
		Directory.CreateDirectory(Path.Combine(_directory, StorageValues.MasterDirectoryName("master")));
		_local = new SqliteConnection($"Data Source={Path.Combine(_directory, StorageValues.LocalFileName)};Pooling=False");
		_master = new SqliteConnection($"Data Source={Path.Combine(_directory, StorageValues.MasterDirectoryName("master"), StorageValues.MasterFileName)};Pooling=False");
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
			[StorageValues.LocalDirectoryConfigurationKey] = _directory,
		}).Build();
	}

	[TearDown]
	public void TearDown()
	{
		_master?.Dispose();
		_local?.Dispose();
		Directory.Delete(_directory, true);
	}

	[Test]
	public async Task FactoryRoles_KeepRepositoriesAndSavesIndependent()
	{
		using ServiceProvider provider = BuildProvider();
		using IServiceScope scope = provider.CreateScope();
		ILocalUnitOfWork local = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateLocal();
		IMasterUnitOfWork master = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateMaster("master");
		foreach (PropertyInfo property in typeof(IAppUnitOfWork).GetProperties())
		{
			object localRepository = property.GetValue(local)!;
			object masterRepository = property.GetValue(master)!;
			Assert.That(localRepository, Is.Not.SameAs(masterRepository), property.Name);
			Assert.That(property.GetValue(local), Is.SameAs(localRepository), property.Name);
			Assert.That(property.GetValue(master), Is.SameAs(masterRepository), property.Name);
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
	public async Task Rollback_DoesNotUndoOtherDatabaseCommit(bool rollbackMaster)
	{
		using ServiceProvider provider = BuildProvider();
		using IServiceScope scope = provider.CreateScope();
		ILocalUnitOfWork local = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateLocal();
		IMasterUnitOfWork master = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateMaster("master");
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

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public async Task Save_AllowsAnotherDetachedUpdateInSameScope(bool useMaster, bool synchronous)
	{
		using ServiceProvider provider = BuildProvider();
		using IServiceScope scope = provider.CreateScope();
		IAppUnitOfWork unit = useMaster
			? scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateMaster("master")
			: scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateLocal();
		AppDbContext context = useMaster
			? GetContext(scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateMaster("master"))
			: GetContext(scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateLocal());
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
		ILocalUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateLocal();
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
		ILocalUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateLocal();
		AppDbContext context = GetContext(scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateLocal());
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
		ILocalUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateLocal();
		AppDbContext context = GetContext(scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateLocal());
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
		ILocalUnitOfWork local = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateLocal();
		IMasterUnitOfWork master = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateMaster("master");
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
		local = retryScope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateLocal();
		Assert.That((await local.LocalJsonRepo.GetById(EntitiesIds.LocalJsonId))!.Json, Is.EqualTo(json));
		Assert.That(await ReadRevision(_local), Is.Zero);

		stored.Json = "{\"batches\":[]}";
		local.LocalJsonRepo.Update(stored);
		await local.SaveChanges();
		Assert.That((await local.LocalJsonRepo.GetById(EntitiesIds.LocalJsonId))!.Json, Is.EqualTo(stored.Json));
	}

	[Test]
	public async Task CumulativeCommand_UsesLocalFactoryTransaction()
	{
		using ServiceProvider provider = BuildProvider();
		using IServiceScope scope = provider.CreateScope();
		ILocalUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateLocal();
		Guid accountId = (await unit.AccountRepo.GetAll()).Single(account => account.Name == "Cash UAH").Id;
		foreach (SqliteConnection connection in new[] { _local, _master })
		{
			await using SqliteCommand reset = connection.CreateCommand();
			reset.CommandText = "UPDATE TransactionEntry SET CumulativeAmount = 0;";
			await reset.ExecuteNonQueryAsync();
		}
		ICumulativeAmountCommand command = scope.ServiceProvider.GetRequiredService<ICumulativeAmountCommand>();
		await using IUnitOfWorkTransaction transaction = await unit.BeginTransaction();
		await command.Recalculate(accountId);
		await unit.RollbackTransaction(transaction);
		Assert.That(await ReadCumulativeCount(_local), Is.Zero);
		using IServiceScope next = provider.CreateScope();
		await next.ServiceProvider.GetRequiredService<ICumulativeAmountCommand>().Recalculate(accountId);
		Assert.That(await ReadCumulativeCount(_local), Is.GreaterThan(0));
		Assert.That(await ReadCumulativeCount(_master), Is.Zero);
	}

	private ServiceProvider BuildProvider()
	{
		ServiceCollection services = new();
		services.AddSharedModule();
		services.AddSingleton(_configuration);
		services.AddSingleton<IConfigurationService>(new MockConfigurationService(_directory));
		services.AddEntityFrameworkSqLiteModule();
		return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
	}

	private static AppDbContext GetContext(IAppUnitOfWork unit) =>
		(AppDbContext)((IEntityFrameworkUnitOfWork)unit).Database.GetService<ICurrentDbContext>().Context;

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
