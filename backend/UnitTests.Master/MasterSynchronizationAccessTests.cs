using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Core.Entities;
using Exchange.Models.Enums;
using Exchange.Models.Synchronizations;
using Shared.Contracts.Storage;
using Master.Contracts.Adapters;
using Master.Contracts.Adapters.MasterStorage;
using Master.Contracts.Services;
using Master.Contracts.Services.Synchronizations;
using Master.Impl;
using Master.Impl.Operations.ApplySyncBatch;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shared.Impl;

namespace UnitTests.Master;

/// <summary>
/// Verifies service ownership of the complete synchronization access lifetime.
/// Resolves MasterService through its production registration and public interface.
/// Persistence and merge operations are substituted to control asynchronous boundaries.
/// Busy requests must not open a transaction or process any batch.
/// Ownership loss and caller cancellation must roll back before releasing access.
/// Successful earlier commits remain independent of a later cancelled batch.
/// A real local lock verifies contention between separate service scopes.
/// No database files, storage accounts or remote hosts are used.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "NUnit TearDown disposes the provider and cancellation source.")]
[TestFixture(Category = "Local")]
public sealed class MasterSynchronizationAccessTests
{
	private ServiceProvider _provider = null!;
	private IMasterAppUnitOfWork _unit = null!;
	private IApplySyncBatchOperation _apply = null!;
	private IStorageLockFactory _storageLockFactory = null!;
	private IStorageLock _handle = null!;
	private CancellationTokenSource _lost = null!;
	private bool _releasedBeforeRollback;
	private IMasterStorageAdapter _storage = null!;
	private IMasterStorageSession _session = null!;
	private static readonly string[] CleanupEvents = ["database-close", "storage-dispose", "release"];
	private static readonly string[] StorageEvents =
	[
		"acquire", "storage-open", "database-open", "commit", "snapshot", "publish",
		"commit", "snapshot", "publish", "database-close", "storage-dispose", "release"
	];

	[SetUp]
	public void SetUp()
	{
		_unit = Substitute.For<IMasterAppUnitOfWork>();
		_apply = Substitute.For<IApplySyncBatchOperation>();
		_storageLockFactory = Substitute.For<IStorageLockFactory>();
		_handle = Substitute.For<IStorageLock>();
		_lost = new CancellationTokenSource();
		_releasedBeforeRollback = false;
		_handle.CancellationToken.Returns(_lost.Token);
		_storageLockFactory.TryAcquire("Master_master.db", Arg.Any<CancellationToken>()).Returns(_handle);
		ConfigureStorage();
		ConfigurePersistence();
		_provider = BuildProvider();
	}

	[TearDown]
	public void TearDown()
	{
		_provider?.Dispose();
		_lost?.Dispose();
	}

	[Test]
	public async Task BusyMasterIsRejectedBeforeDatabaseAccess()
	{
		_storageLockFactory.TryAcquire("Master_master.db", Arg.Any<CancellationToken>()).Returns((IStorageLock?)null);
		Assert.ThrowsAsync<MasterBusyException>(() => Synchronize(Request("first")));
		await _unit.DidNotReceive().BeginTransaction(Arg.Any<CancellationToken>());
		await _apply.DidNotReceiveWithAnyArgs().Run(default!, default, default, default);
		await _storage.DidNotReceiveWithAnyArgs().Open(default!, default!, default);
		await _handle.DidNotReceive().DisposeAsync();
	}

	[Test]
	public async Task StorageLifetimeCoversEachCommittedPublicationAndDatabaseCleanup()
	{
		List<string> events = RecordStorageLifecycle();
		await Synchronize(Request("first", "second"));
		Assert.That(events, Is.EqualTo(StorageEvents));
		await _unit.Received(1).OpenSession(_session.StoragePath, Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task StorageOpenFailureReleasesAccessWithoutOpeningDatabase()
	{
		IOException failure = new("Download failed.");
		_storage.Open("master", _handle, Arg.Any<CancellationToken>()).ThrowsAsync(failure);
		Assert.That(Assert.ThrowsAsync<IOException>(() => Synchronize(Request("first"))), Is.SameAs(failure));
		await _unit.DidNotReceiveWithAnyArgs().OpenSession(default!, default);
		await _handle.Received(1).DisposeAsync();
	}

	[Test]
	public async Task DatabaseOpenFailureClosesDatabaseBeforeStorageAndAccess()
	{
		List<string> events = RecordStorageLifecycle();
		_unit.OpenSession(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new IOException("Open failed."));
		Assert.ThrowsAsync<IOException>(() => Synchronize(Request("first")));
		Assert.That(events.TakeLast(3), Is.EqualTo(CleanupEvents));
		await _unit.DidNotReceive().BeginTransaction(Arg.Any<CancellationToken>());
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task PublicationFailureStopsAfterCommitWithoutRollback(bool snapshotFailure)
	{
		List<string> events = RecordStorageLifecycle();
		IOException failure = new("Publication failed.");
		if (snapshotFailure)
		{
			_unit.CreateSnapshot(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(failure);
		}
		else
		{
			_storage.Publish(_session, Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(failure);
		}
		Assert.That(Assert.ThrowsAsync<IOException>(() => Synchronize(Request("first", "second"))), Is.SameAs(failure));
		await _unit.Received(1).CommitTransaction(Arg.Any<IUnitOfWorkTransaction>(), Arg.Any<CancellationToken>());
		await _unit.DidNotReceiveWithAnyArgs().RollbackTransaction(default!, default);
		await _apply.Received(1).Run(Arg.Any<SyncBatch>(), Arg.Any<Guid>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
		Assert.That(events.TakeLast(3), Is.EqualTo(CleanupEvents));
	}

	[Test]
	public async Task EmptyOrReplayedSubmissionDoesNotPublish()
	{
		await Synchronize(Request());
		await _storage.DidNotReceiveWithAnyArgs().Publish(default!, default!, default);
		await Synchronize(Request("first"));
		LocalSyncBatch receipt = (LocalSyncBatch)_unit.LocalSyncBatchRepo.ReceivedCalls()
			.Single(call => call.GetMethodInfo().Name == "Add").GetArguments()[0]!;
		_unit.LocalSyncBatchRepo.GetBySyncKey(Arg.Any<Guid>(), "first", Arg.Any<CancellationToken>()).Returns(receipt);
		_storage.ClearReceivedCalls();
		_unit.ClearReceivedCalls();
		Assert.That((await Synchronize(Request("first"))).Revision, Is.EqualTo(1));
		await _storage.DidNotReceiveWithAnyArgs().Publish(default!, default!, default);
		await _unit.DidNotReceiveWithAnyArgs().CreateSnapshot(default!, default);
	}

	[Test]
	public async Task SuccessHoldsOneHandleAcrossAllBatchCommits()
	{
		_unit.CommitTransaction(Arg.Any<IUnitOfWorkTransaction>(), Arg.Any<CancellationToken>()).Returns(_ =>
		{
			Assert.That(_handle.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IAsyncDisposable.DisposeAsync)), Is.False);
			return Task.CompletedTask;
		});
		Assert.That((await Synchronize(Request("first", "second"))).Revision, Is.EqualTo(2));
		await _storageLockFactory.Received(1).TryAcquire("Master_master.db", Arg.Any<CancellationToken>());
		await _unit.Received(2).CommitTransaction(Arg.Any<IUnitOfWorkTransaction>(), Arg.Any<CancellationToken>());
		await _handle.Received(1).DisposeAsync();
	}

	[Test]
	public async Task AlreadyLostOwnershipDoesNotOpenTransaction()
	{
		await _lost.CancelAsync();
		Assert.CatchAsync<OperationCanceledException>(() => Synchronize(Request("first")));
		await _unit.DidNotReceive().BeginTransaction(Arg.Any<CancellationToken>());
		await _handle.Received(1).DisposeAsync();
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task CancellationDuringSecondBatchRollsBackBeforeReleasingAccess(bool ownershipLost)
	{
		using CancellationTokenSource caller = new();
		CancelDuringBatch("second", ownershipLost ? _lost : caller);
		VerifyOwnershipDuringRollback();
		Assert.CatchAsync<OperationCanceledException>(() => Synchronize(Request("first", "second", "third"), caller.Token));
		await _unit.Received(1).CommitTransaction(Arg.Any<IUnitOfWorkTransaction>(), Arg.Any<CancellationToken>());
		await _unit.Received(1).RollbackTransaction(Arg.Any<IUnitOfWorkTransaction>(), CancellationToken.None);
		Assert.That(_releasedBeforeRollback, Is.False);
		await _apply.Received(2).Run(Arg.Any<SyncBatch>(), Arg.Any<Guid>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
		await _handle.Received(1).DisposeAsync();
	}

	[Test]
	public async Task MergeFailureRollsBackBeforeReleasingAccess()
	{
		InvalidSyncBatchException failure = new("Invalid batch.");
		_apply.Run(Arg.Any<SyncBatch>(), Arg.Any<Guid>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).ThrowsAsync(failure);
		VerifyOwnershipDuringRollback();
		Assert.That(Assert.ThrowsAsync<InvalidSyncBatchException>(() => Synchronize(Request("first"))), Is.SameAs(failure));
		await _unit.Received(1).RollbackTransaction(Arg.Any<IUnitOfWorkTransaction>(), CancellationToken.None);
		Assert.That(_releasedBeforeRollback, Is.False);
		await _handle.Received(1).DisposeAsync();
	}

	[Test]
	public async Task OwnershipLostAtFinalCommitDoesNotReportSuccess()
	{
		_unit.CommitTransaction(Arg.Any<IUnitOfWorkTransaction>(), Arg.Any<CancellationToken>()).Returns(_ => _lost.CancelAsync());
		Assert.CatchAsync<OperationCanceledException>(() => Synchronize(Request("first")));
		await _unit.Received(1).CommitTransaction(Arg.Any<IUnitOfWorkTransaction>(), Arg.Any<CancellationToken>());
		await _unit.DidNotReceive().RollbackTransaction(Arg.Any<IUnitOfWorkTransaction>(), Arg.Any<CancellationToken>());
		await _storage.DidNotReceiveWithAnyArgs().Publish(default!, default!, default);
		await _handle.Received(1).DisposeAsync();
	}

	[Test]
	public async Task AcquisitionFailurePropagatesWithoutDatabaseAccess()
	{
		InvalidOperationException failure = new("Storage unavailable.");
		_storageLockFactory.TryAcquire("Master_master.db", Arg.Any<CancellationToken>()).ThrowsAsync(failure);
		Assert.That(Assert.ThrowsAsync<InvalidOperationException>(() => Synchronize(Request("first"))), Is.SameAs(failure));
		await _unit.DidNotReceive().BeginTransaction(Arg.Any<CancellationToken>());
		await _handle.DidNotReceive().DisposeAsync();
	}

	[Test]
	public async Task PreCancelledRequestDoesNotAcquireAccess()
	{
		Assert.CatchAsync<OperationCanceledException>(() => Synchronize(Request("first"), new CancellationToken(true)));
		await _storageLockFactory.DidNotReceiveWithAnyArgs().TryAcquire(default!, default);
	}

	[Test]
	public async Task ConcurrentScopeGetsBusyUntilFirstSynchronizationFinishes()
	{
		using ServiceProvider provider = BuildProvider(true);
		TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
		PauseMerge(entered, resume);
		Task<SynchronizeInfo> first = Synchronize(provider, Request("first"));
		try
		{
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.ThrowsAsync<MasterBusyException>(() => Synchronize(provider, Request("second")));
		}
		finally
		{
			resume.TrySetResult();
		}
		await first.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That((await Synchronize(provider, Request())).Revision, Is.EqualTo(1));
	}

	private void ConfigurePersistence()
	{
		SystemConfig config = new() { MasterDatasetKey = "master" };
		LocalRegistration local = new() { Id = Guid.NewGuid(), LocalDatasetKey = "local" };
		_unit.SystemConfigRepo.GetAll(Arg.Any<CancellationToken>()).Returns(new[] { config });
		_unit.LocalRegistrationRepo.GetByLocalDatasetKey("local", Arg.Any<CancellationToken>()).Returns(local);
		_unit.LocalSyncBatchRepo.GetBySyncKey(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((LocalSyncBatch?)null);
		_unit.BeginTransaction(Arg.Any<CancellationToken>()).Returns(_ => Substitute.For<IUnitOfWorkTransaction>());
	}

	private ServiceProvider BuildProvider(bool localAccess = false)
	{
		ServiceCollection services = new();
		services.AddSharedModule();
		services.AddMasterModule();
		services.AddSingleton(_unit);
		services.AddSingleton(_apply);
		services.AddSingleton(_storage);
		if (localAccess)
		{
			services.AddLocalStorageModule();
		}
		else
		{
			services.AddSingleton(_storageLockFactory);
		}
		return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
	}

	private void CancelDuringBatch(string syncKey, CancellationTokenSource cancellation)
	{
		_apply.Run(Arg.Is<SyncBatch>(batch => batch.SyncKey == syncKey), Arg.Any<Guid>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
			.Returns(call =>
			{
				cancellation.Cancel();
				call.Arg<CancellationToken>().ThrowIfCancellationRequested();
				return Task.CompletedTask;
			});
	}

	private void ConfigureStorage()
	{
		_storage = Substitute.For<IMasterStorageAdapter>();
		_session = Substitute.For<IMasterStorageSession>();
		_session.StoragePath.Returns(Path.Combine(Path.GetTempPath(), "mock-master.db"));
		_storage.Open("master", Arg.Any<IStorageLock>(), Arg.Any<CancellationToken>()).Returns(_session);
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012", Justification = "Configures substitute ValueTask returns; no asynchronous operation starts here.")]
	private List<string> RecordStorageLifecycle()
	{
		List<string> events = [];
		_storageLockFactory.TryAcquire("Master_master.db", Arg.Any<CancellationToken>()).Returns(_ => { events.Add("acquire"); return _handle; });
		_storage.Open("master", _handle, Arg.Any<CancellationToken>()).Returns(_ => { events.Add("storage-open"); return _session; });
		_unit.OpenSession(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => Record(events, "database-open"));
		_unit.CommitTransaction(Arg.Any<IUnitOfWorkTransaction>(), Arg.Any<CancellationToken>()).Returns(_ => Record(events, "commit"));
		_unit.CreateSnapshot(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => Record(events, "snapshot"));
		_storage.Publish(_session, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => Record(events, "publish"));
		_unit.CloseSession().Returns(_ => Record(events, "database-close"));
		_session.DisposeAsync().Returns(_ => new ValueTask(Record(events, "storage-dispose")));
		_handle.DisposeAsync().Returns(_ => new ValueTask(Record(events, "release")));
		return events;
	}

	private static Task Record(List<string> events, string value)
	{
		events.Add(value);
		return Task.CompletedTask;
	}

	private void VerifyOwnershipDuringRollback()
	{
		_unit.RollbackTransaction(Arg.Any<IUnitOfWorkTransaction>(), CancellationToken.None).Returns(_ =>
		{
			_releasedBeforeRollback = _handle.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IAsyncDisposable.DisposeAsync));
			return Task.CompletedTask;
		});
	}

	private void PauseMerge(TaskCompletionSource entered, TaskCompletionSource resume)
	{
		_apply.Run(Arg.Any<SyncBatch>(), Arg.Any<Guid>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(_ =>
		{
			entered.TrySetResult();
			return resume.Task;
		});
	}

	private Task<SynchronizeInfo> Synchronize(SynchronizeRequest request, CancellationToken cancellationToken = default) =>
		Synchronize(_provider, request, cancellationToken);

	private static async Task<SynchronizeInfo> Synchronize(ServiceProvider provider, SynchronizeRequest request,
		CancellationToken cancellationToken = default)
	{
		using IServiceScope scope = provider.CreateScope();
		return await scope.ServiceProvider.GetRequiredService<IMasterService>().Synchronize(request, cancellationToken);
	}

	private static SynchronizeRequest Request(params string[] keys)
	{
		SyncContainer container = new() { FormatVersion = 1, SchemaVersion = 1, MasterDatasetKey = "master", LocalDatasetKey = "local" };
		container.Batches.AddRange(keys.Select(key => new SyncBatch
		{
			SyncKey = key, CreatedAt = new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc), Priority = ConflictPriority.Local
		}));
		return new SynchronizeRequest { Container = container };
	}
}
