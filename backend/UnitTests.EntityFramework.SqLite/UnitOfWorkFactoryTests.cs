using Business.Models.Constants;
using Business.Models.Entities.Synchronizations;
using Shared.Contracts;
using NSubstitute;
using DataAccess.EntityFramework.Contracts;
using Microsoft.EntityFrameworkCore;
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
		foreach (string path in new[]
		{
			DatabasePath(),
			DatabasePath("alpha"),
			DatabasePath("beta"),
			DatabasePath("alpha", 11),
			DatabasePath("alpha", 12)
		})
		{
			await using SqliteConnection connection = new(new SqliteConnectionStringBuilder
			{
				DataSource = path, Pooling = false
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

	private string DatabasePath(string? masterDatasetKey = null, long? revision = null)
	{
		string directory = masterDatasetKey is null
			? _directory
			: Path.Combine(_directory, StorageValues.MasterDirectoryName(masterDatasetKey));
		Directory.CreateDirectory(directory);
		string file = masterDatasetKey is null
			? StorageValues.LocalFileName
			: revision.HasValue ? StorageValues.SnapshotFileName(revision.Value) : StorageValues.MasterFileName;
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
		Assert.That(File.Exists(Path.Combine(_directory, StorageValues.MasterDirectoryName("missing"), StorageValues.MasterFileName)), Is.False);
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

		string snapshotPath = Path.Combine(_directory, StorageValues.MasterDirectoryName("alpha"), StorageValues.SnapshotFileName(25));
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
		Assert.That(File.Exists(Path.Combine(_directory, StorageValues.MasterDirectoryName("alpha"), StorageValues.SnapshotFileName(25))), Is.False);
	}

	[Test]
	public async Task FactoryDisposalClosesDatabaseBeforeTemporaryDirectoryCleanup()
	{
		ServiceCollection services = new();
		IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			[StorageValues.LocalDirectoryConfigurationKey] = _directory
		}).Build();
		services.AddSingleton(configuration);
		services.AddSharedModule();
		services.AddEntityFrameworkSqLiteModule();
		using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		string path;
		await using (AsyncServiceScope scope = provider.CreateAsyncScope())
		{
			path = scope.ServiceProvider.GetRequiredService<IConfigurationService>().GetTempDirectory();
			string directory = Directory.CreateDirectory(Path.Combine(path, StorageValues.MasterDirectoryName("alpha"))).FullName;
			File.Copy(DatabasePath("alpha"), Path.Combine(directory, StorageValues.MasterFileName));
			await scope.ServiceProvider.GetRequiredService<ICreateSnapshotCommand>().CreateSnapshot("alpha");
			Assert.That(File.Exists(Path.Combine(directory, StorageValues.SnapshotFileName(0))), Is.True);
		}
		Assert.That(Directory.Exists(path), Is.False);
	}

	[TestCase("Local")]
	[TestCase("Candidate")]
	[TestCase("Master")]
	[TestCase("Download")]
	[TestCase("Snapshot")]
	public async Task CloseReleasesFileAndRecreatesOnlySelectedUnit(string role)
	{
		File.Copy(DatabasePath(), Path.Combine(_directory, StorageValues.CandidateFileName));
		await using ServiceProvider provider = CreateProvider();
		await using AsyncServiceScope scope = provider.CreateAsyncScope();
		IUnitOfWorkFactory factory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
		string downloadDirectory = Path.Combine(_directory, "Download_test");
		Directory.CreateDirectory(downloadDirectory);
		File.Copy(DatabasePath("alpha"), Path.Combine(downloadDirectory, StorageValues.MasterFileName));
		Func<IAppUnitOfWork> create = role switch
		{
			"Local" => () => factory.CreateLocal(),
			"Candidate" => () => factory.CreateCandidate(),
			"Master" => () => factory.CreateMaster("alpha"),
			"Download" => () => factory.CreateMaster("alpha", downloadDirectory),
			_ => () => factory.CreateSnapshot("alpha", 11)
		};
		Func<Task> close = role switch
		{
			"Local" => factory.CloseLocal,
			"Candidate" => factory.CloseCandidate,
			"Master" => () => factory.CloseMaster("alpha"),
			"Download" => () => factory.CloseMaster("alpha", downloadDirectory),
			_ => () => factory.CloseSnapshot("alpha", 11)
		};
		await close();
		IAppUnitOfWork unit = create();
		IMasterUnitOfWork other = factory.CreateMaster("beta");
		IEntityFrameworkUnitOfWork entityFrameworkUnit = (IEntityFrameworkUnitOfWork)unit;
		await entityFrameworkUnit.Database.OpenConnectionAsync();
		string path = entityFrameworkUnit.Database.GetDbConnection().DataSource;
		await SetRevision(unit, 23);

		await close();
		await close();

		Assert.ThrowsAsync<ObjectDisposedException>(async () => await unit.SaveChanges());
		using (FileStream file = new(
			path,
			FileMode.Open,
			FileAccess.ReadWrite,
			FileShare.None))
		{
			Assert.That(file.Length, Is.GreaterThan(0));
		}
		IAppUnitOfWork reopened = create();
		Assert.That(reopened, Is.Not.SameAs(unit));
		Assert.That(create(), Is.SameAs(reopened));
		Assert.That(factory.CreateMaster("beta"), Is.SameAs(other));
		Assert.That(await GetRevision(reopened), Is.EqualTo(23));
	}

	[Test]
	public async Task CandidatePreparationRebuildsOnlyCandidateAndInstallsWalDatabase()
	{
		string candidatePath = Path.Combine(_directory, StorageValues.CandidateFileName);
		File.Copy(DatabasePath(), candidatePath);
		await using ServiceProvider provider = CreateProvider();
		await using AsyncServiceScope scope = provider.CreateAsyncScope();
		IUnitOfWorkFactory factory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
		ILocalUnitOfWork local = factory.CreateLocal();
		ICandidateUnitOfWork candidate = factory.CreateCandidate();
		Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade database = ((IEntityFrameworkUnitOfWork)candidate).Database;
		await database.OpenConnectionAsync();
		await database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(
			Path.Combine(TestContext.CurrentContext.TestDirectory, "SqlAssets", "003-test-data.sql")));
		await database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
		await database.ExecuteSqlRawAsync("UPDATE TransactionEntry SET CumulativeAmount=0;");
		await SetRevision(candidate, 11);
		await ((IEntityFrameworkUnitOfWork)local).Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
		await SetRevision(local, 3);
		LocalConfig configuration = (await local.LocalConfigRepo.GetAll()).Single();
		configuration.SnapshotRevision = 11;
		configuration.AccountNameSeparator = "|";
		IPrepareCandidateCommand prepare = scope.ServiceProvider.GetRequiredService<IPrepareCandidateCommand>();
		await scope.ServiceProvider.GetRequiredService<IValidateCandidateCommand>().Validate();
		await using (IUnitOfWorkTransaction transaction = await candidate.BeginTransaction())
		{
			await prepare.Prepare(configuration, new LocalJson { Json = "{\"batches\":[]}" });
			await candidate.CommitTransaction(transaction);
		}
		Assert.That((await local.LocalConfigRepo.GetAll()).Single().SnapshotRevision, Is.Zero);
		Assert.That(await GetRevision(local), Is.EqualTo(3));
		await using (System.Data.Common.DbCommand command = database.GetDbConnection().CreateCommand())
		{
			command.CommandText = "SELECT CumulativeAmount FROM TransactionEntry WHERE Id=X'00000000000000000000000000000052';";
			Assert.That(await command.ExecuteScalarAsync(), Is.EqualTo(8745000L));
		}

		await scope.ServiceProvider.GetRequiredService<IInstallCandidateCommand>().Install();

		Assert.That(File.Exists(candidatePath), Is.False);
		ILocalUnitOfWork installed = factory.CreateLocal();
		Assert.That(installed, Is.Not.SameAs(local));
		Assert.That(await GetRevision(installed), Is.EqualTo(11));
		Assert.That((await installed.LocalConfigRepo.GetAll()).Single().AccountNameSeparator, Is.EqualTo("|"));
		Assert.That((await installed.LocalJsonRepo.GetAll()).Single().Json, Is.EqualTo("{\"batches\":[]}"));
		Assert.ThrowsAsync<ObjectDisposedException>(async () => await candidate.SaveChanges());
		Assert.ThrowsAsync<ObjectDisposedException>(async () => await local.SaveChanges());
	}

	[Test]
	public async Task ReplacementFailureLeavesLocalReopenable()
	{
		File.Copy(DatabasePath(), Path.Combine(_directory, StorageValues.CandidateFileName));
		IFileService files = Substitute.For<IFileService>();
		files.When(value => value.Replace(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()))
			.Do(_ => throw new IOException("Replacement failed."));
		await using ServiceProvider provider = CreateProvider(files);
		await using AsyncServiceScope scope = provider.CreateAsyncScope();
		IUnitOfWorkFactory factory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
		ILocalUnitOfWork local = factory.CreateLocal();
		await SetRevision(local, 3);
		await SetRevision(factory.CreateCandidate(), 11);

		Assert.ThrowsAsync<IOException>(() => scope.ServiceProvider.GetRequiredService<IInstallCandidateCommand>().Install());

		Assert.That(factory.CreateLocal(), Is.Not.SameAs(local));
		Assert.That(await GetRevision(factory.CreateLocal()), Is.EqualTo(3));
		Assert.That(File.Exists(Path.Combine(_directory, StorageValues.CandidateFileName)), Is.True);
	}

	[Test]
	public async Task CancelledInstallationKeepsLocalDatabase()
	{
		File.Copy(DatabasePath(), Path.Combine(_directory, StorageValues.CandidateFileName));
		await using ServiceProvider provider = CreateProvider();
		await using AsyncServiceScope scope = provider.CreateAsyncScope();
		IUnitOfWorkFactory factory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
		await SetRevision(factory.CreateLocal(), 3);
		await SetRevision(factory.CreateCandidate(), 11);

		Assert.CatchAsync<OperationCanceledException>(() =>
			scope.ServiceProvider.GetRequiredService<IInstallCandidateCommand>().Install(new CancellationToken(true)));

		Assert.That(await GetRevision(factory.CreateLocal()), Is.EqualTo(3));
		Assert.That(File.Exists(Path.Combine(_directory, StorageValues.CandidateFileName)), Is.True);
	}

	[Test]
	public void CleanDataFilesRemovesOnlySelectedDatabaseAndSupportsRepeatedCleanup()
	{
		string path = Path.Combine(_directory, StorageValues.CandidateFileName);
		string[] suffixes = { "", "-wal", "-shm", "-journal" };
		foreach (string suffix in suffixes)
		{
			File.WriteAllText(path + suffix, "interrupted download");
		}
		string unrelated = path + ".backup";
		File.WriteAllText(unrelated, "preserve");
		using ServiceProvider provider = CreateProvider();
		using IServiceScope scope = provider.CreateScope();
		ICleanDataFilesCommand command = scope.ServiceProvider.GetRequiredService<ICleanDataFilesCommand>();

		command.CleanAll(path);
		command.CleanAll(path);

		foreach (string suffix in suffixes)
		{
			Assert.That(File.Exists(path + suffix), Is.False);
		}
		Assert.That(File.Exists(unrelated), Is.True);
		Assert.That(File.Exists(DatabasePath()), Is.True);
	}

	private ServiceProvider CreateProvider(IFileService? files = null)
	{
		ServiceCollection services = new();
		IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			[StorageValues.LocalDirectoryConfigurationKey] = _directory,
		}).Build();
		services.AddSingleton(configuration);
		services.AddSharedModule();
		services.AddSingleton<IConfigurationService>(new MockConfigurationService(_directory));
		services.AddEntityFrameworkSqLiteModule();
		if (files is not null)
		{
			services.AddSingleton(files);
		}
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
