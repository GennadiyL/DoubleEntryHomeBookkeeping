using Shared.Impl;
using Shared.Contracts.Config;
using Tests.Common.Mocks.Shared;
using Business.Models.Entities.Config;
using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
using DataAccess.Core.Entities;
using DataAccess.EntityFramework.SqLite;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace UnitTests.EntityFramework.SqLite;

/// <summary>
/// Exercises the factory API against isolated SQLite database files.
/// Production schema scripts initialize each database used by the tests.
/// Cache tests distinguish roles, datasets, revisions and outer scopes.
/// Transaction tests check repository binding and database isolation.
/// Disposal tests cover synchronous and asynchronous scope ownership.
/// Snapshot writes use the same access mode as Local and Master writes.
/// No existing application database or external service is accessed.
/// Each test removes only its own temporary directory after disposal.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class UnitOfWorkFactoryTests
{
	private string _directory = null!;

	[SetUp]
	public async Task SetUp()
	{
		_directory = Path.Combine(Path.GetTempPath(), $"dehb-uow-tests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(_directory);
		foreach (string name in new[] { "Local.db", "Master_alpha.db", "Master_beta.db", "Master_alpha_11.db", "Master_alpha_12.db" })
		{
			await using SqliteConnection connection = new(new SqliteConnectionStringBuilder
			{
				DataSource = DatabasePath(name), Pooling = false
			}.ToString());
			await connection.OpenAsync();
			foreach (string script in new[] { "001-schema.sql", "002-initial-data.sql" })
			{
				await using SqliteCommand command = connection.CreateCommand();
				command.CommandText = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "SqlAssets", script));
				await command.ExecuteNonQueryAsync();
			}
		}
	}

	private string DatabasePath(string name)
	{
		string directory = name.StartsWith("Master_", StringComparison.Ordinal)
			? Path.Combine(_directory, $"Master_{name.Split('_')[1].Split('.')[0]}")
			: _directory;
		Directory.CreateDirectory(directory);
		string file = name.StartsWith("Master_", StringComparison.Ordinal)
			? name.Split('_').Length == 2 ? "master.db" : $"snapshot_{name.Split('_')[2]}"
			: name;
		return Path.Combine(directory, file);
	}

	[TearDown]
	public void TearDown()
	{
		Directory.Delete(_directory, true);
	}

	[Test]
	public async Task CacheSeparatesRolesDatasetsRevisionsAndScopes()
	{
		await using ServiceProvider provider = CreateProvider();
		await using AsyncServiceScope first = provider.CreateAsyncScope();
		await using AsyncServiceScope second = provider.CreateAsyncScope();
		IUnitOfWorkFactory factory = first.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
		ILocalUnitOfWork local = factory.CreateLocal();
		IMasterUnitOfWork master = factory.CreateMaster("alpha");
		ISnapshotUnitOfWork snapshot = factory.CreateSnapshot("alpha", 11);
		Assert.Multiple(() =>
		{
			Assert.That(first.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>(), Is.SameAs(factory));
			Assert.That(factory.CreateLocal(), Is.SameAs(local));
			Assert.That(factory.CreateMaster("alpha"), Is.SameAs(master));
			Assert.That(factory.CreateSnapshot("alpha", 11), Is.SameAs(snapshot));
			Assert.That(factory.CreateMaster("beta"), Is.Not.SameAs(master));
			Assert.That(factory.CreateSnapshot("alpha", 12), Is.Not.SameAs(snapshot));
			Assert.That(master, Is.Not.SameAs(local));
			Assert.That(snapshot, Is.Not.SameAs(master));
			Assert.That(master.MasterDatasetKey, Is.EqualTo("alpha"));
			Assert.That(snapshot.MasterDatasetKey, Is.EqualTo("alpha"));
			Assert.That(snapshot.Revision, Is.EqualTo(11));
			Assert.That(second.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateLocal(), Is.Not.SameAs(local));
		});
	}

	[Test]
	public async Task WritesAndTransactionsStayWithSelectedDatabase()
	{
		await using ServiceProvider provider = CreateProvider();
		await using (AsyncServiceScope scope = provider.CreateAsyncScope())
		{
			IUnitOfWorkFactory factory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
			ILocalUnitOfWork local = factory.CreateLocal();
			IMasterUnitOfWork master = factory.CreateMaster("alpha");
			await using IUnitOfWorkTransaction transaction = await master.BeginTransaction();
			await SetRevision(master, 25);
			await SetRevision(local, 7);
			await SetRevision(factory.CreateSnapshot("alpha", 11), 11);
			await master.RollbackTransaction(transaction);
		}
		await using AsyncServiceScope verify = provider.CreateAsyncScope();
		IUnitOfWorkFactory reader = verify.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
		Assert.That(await GetRevision(reader.CreateLocal()), Is.EqualTo(7));
		Assert.That(await GetRevision(reader.CreateMaster("alpha")), Is.EqualTo(0));
		Assert.That(await GetRevision(reader.CreateMaster("beta")), Is.EqualTo(0));
		Assert.That(await GetRevision(reader.CreateSnapshot("alpha", 11)), Is.EqualTo(11));
		Assert.That(await GetRevision(reader.CreateSnapshot("alpha", 12)), Is.EqualTo(0));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ScopeDisposalReleasesAllUnitsAndRejectsFactoryReuse(bool asynchronous)
	{
		await using ServiceProvider provider = CreateProvider();
		AsyncServiceScope scope = provider.CreateAsyncScope();
		IUnitOfWorkFactory factory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
		IAppUnitOfWork[] units = { factory.CreateLocal(), factory.CreateMaster("alpha"), factory.CreateSnapshot("alpha", 11) };
		foreach (IAppUnitOfWork unit in units) { await GetRevision(unit); }
		if (asynchronous) { await scope.DisposeAsync(); }
		else { scope.Dispose(); }
		Assert.Throws<ObjectDisposedException>(() => factory.CreateLocal());
		Assert.Throws<ObjectDisposedException>(() => factory.CreateMaster("alpha"));
		Assert.Throws<ObjectDisposedException>(() => factory.CreateSnapshot("alpha", 11));
		foreach (IAppUnitOfWork unit in units)
		{
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await unit.SaveChanges());
		}
		await scope.DisposeAsync();
	}

	[Test]
	public async Task MissingDatabaseIsNotCreatedAndUnsafeKeysAreRejected()
	{
		await using ServiceProvider provider = CreateProvider();
		await using AsyncServiceScope scope = provider.CreateAsyncScope();
		IUnitOfWorkFactory factory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
		Assert.Throws<ArgumentException>(() => factory.CreateMaster("../outside"));
		Assert.Throws<ArgumentException>(() => factory.CreateSnapshot("a/b", 1));
		Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateSnapshot("alpha", -1));
		Assert.ThrowsAsync<SqliteException>(async () => await GetRevision(factory.CreateMaster("missing")));
		Assert.That(File.Exists(Path.Combine(_directory, "Master_missing", "master.db")), Is.False);
	}

	[Test]
	public async Task SnapshotCommandCopiesCurrentRevisionBesideMasterAndKeepsMasterUsable()
	{
		await using ServiceProvider provider = CreateProvider();
		await using AsyncServiceScope scope = provider.CreateAsyncScope();
		IUnitOfWorkFactory factory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
		IMasterUnitOfWork master = factory.CreateMaster("alpha");
		await SetRevision(master, 25);

		await scope.ServiceProvider.GetRequiredService<ICreateSnapshotCommand>().CreateSnapshot("alpha");

		string snapshotPath = Path.Combine(_directory, "Master_alpha", "snapshot_25.db");
		Assert.That(File.Exists(snapshotPath), Is.True);
		await SetRevision(master, 26);
		Assert.That(await GetRevision(factory.CreateSnapshot("alpha", 25)), Is.EqualTo(25));
		Assert.That(await GetRevision(master), Is.EqualTo(26));
	}

	[Test]
	public async Task SnapshotCommandRejectsActiveMasterTransaction()
	{
		await using ServiceProvider provider = CreateProvider();
		await using AsyncServiceScope scope = provider.CreateAsyncScope();
		IMasterUnitOfWork master = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateMaster("alpha");
		await using IUnitOfWorkTransaction transaction = await master.BeginTransaction();
		await SetRevision(master, 25);

		ICreateSnapshotCommand command = scope.ServiceProvider.GetRequiredService<ICreateSnapshotCommand>();
		Assert.ThrowsAsync<InvalidOperationException>(() => command.CreateSnapshot("alpha"));
		Assert.That(File.Exists(Path.Combine(_directory, "Master_alpha", "snapshot_25.db")), Is.False);
	}

	[Test]
	public async Task FactoryDisposalClosesDatabaseBeforeTemporaryDirectoryCleanup()
	{
		ServiceCollection services = new();
		IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			["DataAccess:LocalDirectory"] = _directory
		}).Build();
		services.AddSingleton(configuration);
		services.AddSharedModule();
		services.AddEntityFrameworkSqLiteModule();
		using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		string path;
		await using (AsyncServiceScope scope = provider.CreateAsyncScope())
		{
			path = scope.ServiceProvider.GetRequiredService<IConfigurationService>().GetTempDirectory();
			string directory = Directory.CreateDirectory(Path.Combine(path, "Master_alpha")).FullName;
			File.Copy(DatabasePath("Master_alpha.db"), Path.Combine(directory, "master.db"));
			await scope.ServiceProvider.GetRequiredService<ICreateSnapshotCommand>().CreateSnapshot("alpha");
			Assert.That(File.Exists(Path.Combine(directory, "snapshot_0.db")), Is.True);
		}
		Assert.That(Directory.Exists(path), Is.False);
	}

	private ServiceProvider CreateProvider()
	{
		ServiceCollection services = new();
		IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			["DataAccess:LocalDirectory"] = _directory,
		}).Build();
		services.AddSingleton(configuration);
		services.AddSingleton<IConfigurationService>(new MockConfigurationService(_directory));
		services.AddEntityFrameworkSqLiteModule();
		return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
	}

	private static async Task<long> GetRevision(IAppUnitOfWork unit) =>
		(await unit.SystemConfigRepo.GetAll()).Single().CurrentRevision;

	private static async Task SetRevision(IAppUnitOfWork unit, long revision)
	{
		SystemConfig config = (await unit.SystemConfigRepo.GetAll()).Single();
		config.CurrentRevision = revision;
		unit.SystemConfigRepo.Update(config);
		await unit.SaveChanges();
	}
}

