using Master.Impl.Operations.MasterStorage;
using Business.Models.Constants;
using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
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
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "NUnit TearDown disposes the temporary store.")]
[TestFixture(Category = "Local")]
public sealed class MasterAcknowledgeTests
{
	private MasterTestStore _store = null!;
	private IDateTimeService _clock = null!;
	private static readonly DateTime Now = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
	private string DatabasePath => _store.DatabasePath;

	[SetUp]
	public async Task SetUp()
	{
		_store = new MasterTestStore();
		await _store.Initialize(7);
		_clock = Substitute.For<IDateTimeService>();
		_clock.UtcNow.Returns(Now);
		using ServiceProvider provider = BuildProvider();
		using IServiceScope scope = provider.CreateScope();
		IMasterUnitOfWork unit = _store.OpenMaster(scope.ServiceProvider);
		unit.LocalRegistrationRepo.Add(new LocalRegistration { Id = Guid.NewGuid(), LocalDatasetKey = "other" });
		await unit.SaveChanges();
	}

	[TearDown]
	public void TearDown() => _store.Dispose();

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
		IMasterUnitOfWork unit = _store.OpenMaster(scope.ServiceProvider);
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
			IMasterUnitOfWork unit = _store.OpenMaster(scope.ServiceProvider);
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
		if (masterKey == "wrong")
		{
			string directory = Directory.CreateDirectory(Path.Combine(_store.StorageRoot, StorageValues.MasterDirectoryName("wrong"))).FullName;
			File.Copy(DatabasePath, Path.Combine(directory, StorageValues.MasterFileName));
		}
		Assert.ThrowsAsync<InvalidSyncBatchException>(() => Acknowledge(provider,
			new AcknowledgeRequest { MasterDatasetKey = masterKey, LocalDatasetKey = localKey, InstalledRevision = revision }));
		await AssertAcknowledgement(provider, 0, null);
	}

	[Test]
	public async Task BusyMasterCanBeRetriedAfterRelease()
	{
		using ServiceProvider provider = BuildProvider();
		IStorageLock access = (await provider.GetRequiredService<IStorageLockFactory>().TryAcquire(MasterStorageNames.MasterRelativePath("master")))!;
		await using (access)
		{
			Assert.ThrowsAsync<MasterBusyException>(() => Acknowledge(provider, Request(7)));
		}
		Assert.That((await Acknowledge(provider, Request(7))).IsSuccess, Is.True);
	}

	private ServiceProvider BuildProvider(bool remote = false) =>
		_store.CreateProvider(services => services.AddSingleton(_clock));

	private async Task AssertAcknowledgement(ServiceProvider provider, long revision, DateTime? time)
	{
		using IServiceScope scope = provider.CreateScope();
		LocalRegistration local = (await _store.OpenMaster(scope.ServiceProvider)
			.LocalRegistrationRepo.GetByLocalDatasetKey("local"))!;
		Assert.That(local.SyncRevision, Is.EqualTo(revision));
		Assert.That(local.SyncDateTime, Is.EqualTo(time));
	}

	private static AcknowledgeRequest Request(long revision) =>
		new() { MasterDatasetKey = "master", LocalDatasetKey = "local", InstalledRevision = revision };

	private static async Task<AcknowledgeResponse> Acknowledge(ServiceProvider provider, AcknowledgeRequest request)
	{
		using IServiceScope scope = provider.CreateScope();
		return await scope.ServiceProvider.GetRequiredService<IMasterService>().Acknowledge(request);
	}
}
