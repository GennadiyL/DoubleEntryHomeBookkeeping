using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.EntityFramework.SqLite;
using Master.Contracts.Services;
using Master.Contracts.Services.Synchronizations;
using Master.Impl;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Shared.Contracts;
using Shared.Contracts.Storage;
using Shared.Impl;

namespace UnitTests.Master;

/// <summary>
/// Verifies acknowledgement persistence using the production SQLite database.
/// Exercises Local and Remote storage selected through host registrations.
/// Remote operations use shared local storage without an Azure dependency.
/// Checks the installed revision and the injected Master UTC timestamp together.
/// Repeated and older receipts must leave acknowledgement metadata unchanged.
/// Initial revision zero is distinguished by its missing acknowledgement time.
/// Invalid identities and future revisions cannot modify persisted state.
/// Every test owns its database files and disposes scopes before cleanup.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class MasterAcknowledgeTests
{
	private DirectoryInfo _directory = null!;
	private IDateTimeService _clock = null!;
	private static readonly DateTime Now = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
	private string DatabasePath => Path.Combine(_directory.FullName, "Master_master.db");

	[SetUp]
	public async Task SetUp()
	{
		_directory = Directory.CreateTempSubdirectory("dehb-master-ack-tests-");
		_clock = Substitute.For<IDateTimeService>();
		_clock.UtcNow.Returns(Now);
		await using SqliteConnection connection = new($"Data Source={DatabasePath};Pooling=False");
		await connection.OpenAsync();
		foreach (string script in new[] { "001-schema.sql", "002-initial-data.sql" })
		{
			await using SqliteCommand command = connection.CreateCommand();
			command.CommandText = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "SqlAssets", script));
			await command.ExecuteNonQueryAsync();
		}
		using ServiceProvider provider = BuildProvider();
		using IServiceScope scope = provider.CreateScope();
		IMasterAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>();
		SystemConfig config = (await unit.SystemConfigRepo.GetAll()).Single();
		config.MasterDatasetKey = "master";
		config.CurrentRevision = 7;
		unit.SystemConfigRepo.Update(config);
		unit.LocalRegistrationRepo.Add(new LocalRegistration { Id = Guid.NewGuid(), LocalDatasetKey = "local" });
		unit.LocalRegistrationRepo.Add(new LocalRegistration { Id = Guid.NewGuid(), LocalDatasetKey = "other" });
		await unit.SaveChanges();
	}

	[TearDown]
	public void TearDown()
	{
		if (_directory is not null && Directory.Exists(_directory.FullName))
		{
			_directory.Delete(true);
		}
	}

	[TestCase(false, 0)]
	[TestCase(true, 0)]
	[TestCase(false, 5)]
	[TestCase(true, 5)]
	[TestCase(false, 7)]
	[TestCase(true, 7)]
	public async Task AcknowledgementPersistsWithoutAllocatingMasterRevision(bool remote, long installedRevision)
	{
		using ServiceProvider provider = BuildProvider(remote);
		Assert.That((await Acknowledge(provider, Request(installedRevision))).IsSuccess, Is.True);
		using IServiceScope scope = provider.CreateScope();
		IMasterAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>();
		LocalRegistration local = (await unit.LocalRegistrationRepo.GetByLocalDatasetKey("local"))!;
		LocalRegistration other = (await unit.LocalRegistrationRepo.GetByLocalDatasetKey("other"))!;
		Assert.Multiple(() =>
		{
			Assert.That(local.SyncRevision, Is.EqualTo(installedRevision));
			Assert.That(local.SyncDateTime, Is.EqualTo(Now));
			Assert.That(local.SyncDateTime!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
			Assert.That(other.SyncRevision, Is.Zero);
			Assert.That(other.SyncDateTime, Is.Null);
		});
		Assert.That((await unit.SystemConfigRepo.GetAll()).Single().CurrentRevision, Is.EqualTo(7));
		Assert.That(await unit.LocalSyncBatchRepo.GetAll(), Is.Empty);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task RepeatedAndOlderReceiptsPreserveMetadataWhileNewerReceiptAdvancesIt(bool remote)
	{
		using ServiceProvider provider = BuildProvider(remote);
		await Acknowledge(provider, Request(5));
		_clock.UtcNow.Returns(Now.AddDays(1));
		Assert.That((await Acknowledge(provider, Request(5))).IsSuccess, Is.True);
		Assert.That((await Acknowledge(provider, Request(3))).IsSuccess, Is.True);
		await AssertAcknowledgement(provider, 5, Now);
		Assert.That((await Acknowledge(provider, Request(7))).IsSuccess, Is.True);
		await AssertAcknowledgement(provider, 7, Now.AddDays(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task InitialRevisionZeroIsAcknowledgedOnce(bool remote)
	{
		using ServiceProvider provider = BuildProvider(remote);
		using (IServiceScope scope = provider.CreateScope())
		{
			IMasterAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>();
			SystemConfig config = (await unit.SystemConfigRepo.GetAll()).Single();
			config.CurrentRevision = 0;
			unit.SystemConfigRepo.Update(config);
			await unit.SaveChanges();
		}
		Assert.That((await Acknowledge(provider, Request(0))).IsSuccess, Is.True);
		_clock.UtcNow.Returns(Now.AddDays(1));
		Assert.That((await Acknowledge(provider, Request(0))).IsSuccess, Is.True);
		await AssertAcknowledgement(provider, 0, Now);
	}

	[TestCase("", "local", 0)]
	[TestCase("master", "", 0)]
	[TestCase("master", "local", -1)]
	[TestCase("wrong", "local", 0)]
	[TestCase("master", "unknown", 0)]
	[TestCase("master", "local", 8)]
	public async Task InvalidReceiptLeavesRegistrationUnchanged(string masterKey, string localKey, long revision)
	{
		using ServiceProvider provider = BuildProvider();
		Assert.ThrowsAsync<InvalidSyncBatchException>(() => Acknowledge(provider,
			new AcknowledgeRequest { MasterDatasetKey = masterKey, LocalDatasetKey = localKey, InstalledRevision = revision }));
		await AssertAcknowledgement(provider, 0, null);
	}

	[Test]
	public async Task BusyMasterCanBeRetriedAfterRelease()
	{
		using ServiceProvider provider = BuildProvider();
		IStorageLock access = (await provider.GetRequiredService<IStorageLockFactory>().TryAcquire("Master_master.db"))!;
		await using (access)
		{
			Assert.ThrowsAsync<MasterBusyException>(() => Acknowledge(provider, Request(7)));
		}
		Assert.That((await Acknowledge(provider, Request(7))).IsSuccess, Is.True);
	}

	private ServiceProvider BuildProvider(bool remote = false)
	{
		IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			["ConnectionStrings:DehbMasterSqLite"] = $"Data Source={DatabasePath};Pooling=False",
			["fileStorage:Root"] = _directory.FullName
		}).Build();
		ServiceCollection services = new();
		services.AddSingleton(configuration);
		services.AddSharedModule();
		services.AddSingleton(_clock);
		services.AddMasterDataAccessSqLiteModule(configuration);
		services.AddMasterModule();
		if (remote)
		{
			services.AddRemoteMasterStorage();
		}
		else
		{
			services.AddLocalMasterStorage(_ => DatabasePath);
		}
		return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
	}

	private static async Task AssertAcknowledgement(ServiceProvider provider, long revision, DateTime? time)
	{
		using IServiceScope scope = provider.CreateScope();
		LocalRegistration local = (await scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>()
			.LocalRegistrationRepo.GetByLocalDatasetKey("local"))!;
		Assert.That(local.SyncRevision, Is.EqualTo(revision));
		Assert.That(local.SyncDateTime, Is.EqualTo(time));
	}

	private static AcknowledgeRequest Request(long revision) =>
		new() { MasterDatasetKey = "master", LocalDatasetKey = "local", InstalledRevision = revision };

	private static async Task<AcknowledgeInfo> Acknowledge(ServiceProvider provider, AcknowledgeRequest request)
	{
		using IServiceScope scope = provider.CreateScope();
		return await scope.ServiceProvider.GetRequiredService<IMasterService>().Acknowledge(request);
	}
}
