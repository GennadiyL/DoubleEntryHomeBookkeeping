using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Core.Entities;
using Master.Contracts.Services;
using Master.Contracts.Services.Synchronizations;
using Master.Impl;
using Master.Impl.Operations.ApplySyncBatch;
using Master.Impl.Operations.MasterStorage;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Shared.Contracts.Storage;
using Shared.Impl;

namespace UnitTests.Master;

/// <summary>
/// Verifies acknowledgement transactions and storage ownership during failures.
/// Resolves the service through its normal dependency injection registration.
/// Substituted persistence exposes commit, rollback and publication boundaries.
/// Request cancellation and ownership loss must stop work before publication.
/// Persistence errors roll back with an uncancelled cleanup token.
/// Publication failures propagate after commit without attempting rollback.
/// Cleanup closes the database before storage disposal and access release.
/// These tests require no database files or remote infrastructure.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "NUnit TearDown disposes the provider and cancellation source.")]
[TestFixture(Category = "Local")]
public sealed class MasterAcknowledgeAccessTests
{
	private ServiceProvider _provider = null!;
	private IMasterAppUnitOfWork _unit = null!;
	private IMasterStorageOperation _storage = null!;
	private IMasterStorageSession _session = null!;
	private IStorageLockFactory _locks = null!;
	private IStorageLock _access = null!;
	private CancellationTokenSource _lost = null!;
	private LocalRegistration _local = null!;
	private readonly List<string> _events = [];
	private static readonly string[] CleanupEvents = ["database-close", "storage-dispose", "release"];
	private static readonly string[] RollbackEvents = ["rollback", "database-close", "storage-dispose", "release"];
	private static readonly string[] SuccessEvents = ["commit", "snapshot", "publish", "database-close", "storage-dispose", "release"];

	[SetUp]
	public void SetUp()
	{
		_events.Clear();
		_unit = Substitute.For<IMasterAppUnitOfWork>();
		_storage = Substitute.For<IMasterStorageOperation>();
		_session = Substitute.For<IMasterStorageSession>();
		_session.StoragePath.Returns(Path.Combine(Path.GetTempPath(), $"ack-test-{Guid.NewGuid():N}.db"));
		_locks = Substitute.For<IStorageLockFactory>();
		_access = Substitute.For<IStorageLock>();
		_lost = new CancellationTokenSource();
		_access.CancellationToken.Returns(_lost.Token);
		_locks.TryAcquire("Master_master.db", Arg.Any<CancellationToken>()).Returns(_access);
		_storage.Open("master", _access, Arg.Any<CancellationToken>()).Returns(_session);
		_unit.BeginTransaction(Arg.Any<CancellationToken>()).Returns(Substitute.For<IUnitOfWorkTransaction>());
		_unit.SystemConfigRepo.GetAll(Arg.Any<CancellationToken>())
			.Returns(new[] { new SystemConfig { MasterDatasetKey = "master", CurrentRevision = 7 } });
		_local = new LocalRegistration { Id = Guid.NewGuid(), LocalDatasetKey = "local" };
		_unit.LocalRegistrationRepo.GetByLocalDatasetKey("local", Arg.Any<CancellationToken>()).Returns(_local);
		RecordCleanup();
		ServiceCollection services = new();
		services.AddSharedModule();
		services.AddMasterModule();
		services.AddSingleton(_unit);
		services.AddSingleton(_storage);
		services.AddSingleton(_locks);
		services.AddSingleton(Substitute.For<IApplySyncBatchOperation>());
		_provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
	}

	[TearDown]
	public void TearDown()
	{
		_provider?.Dispose();
		_lost?.Dispose();
	}

	[Test]
	public async Task BusyMasterDoesNotOpenStorage()
	{
		_locks.TryAcquire("Master_master.db", Arg.Any<CancellationToken>()).Returns((IStorageLock?)null);
		Assert.ThrowsAsync<MasterBusyException>(() => Acknowledge());
		await _storage.DidNotReceiveWithAnyArgs().Open(default!, default!, default);
		await _unit.DidNotReceiveWithAnyArgs().BeginTransaction(default);
	}

	[Test]
	public async Task PreCancelledRequestDoesNotAcquireAccess()
	{
		Assert.CatchAsync<OperationCanceledException>(() => Acknowledge(new CancellationToken(true)));
		await _locks.DidNotReceiveWithAnyArgs().TryAcquire(default!, default);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task CancellationBeforeCommitRollsBackBeforeReleasingAccess(bool ownershipLost)
	{
		using CancellationTokenSource caller = new();
		_unit.SaveChanges(Arg.Any<CancellationToken>()).Returns(_ =>
		{
			(ownershipLost ? _lost : caller).Cancel();
			return Task.CompletedTask;
		});
		Assert.CatchAsync<OperationCanceledException>(() => Acknowledge(caller.Token));
		await _unit.Received(1).RollbackTransaction(Arg.Any<IUnitOfWorkTransaction>(), CancellationToken.None);
		await _unit.DidNotReceiveWithAnyArgs().CommitTransaction(default!, default);
		await _storage.DidNotReceiveWithAnyArgs().Publish(default!, default!, default);
		Assert.That(_events, Is.EqualTo(RollbackEvents));
	}

	[Test]
	public async Task SaveFailureRollsBackAndPropagates()
	{
		IOException failure = new("Save failed.");
		_unit.SaveChanges(Arg.Any<CancellationToken>()).ThrowsAsync(failure);
		Assert.That(Assert.ThrowsAsync<IOException>(() => Acknowledge()), Is.SameAs(failure));
		await _unit.Received(1).RollbackTransaction(Arg.Any<IUnitOfWorkTransaction>(), CancellationToken.None);
		await _storage.DidNotReceiveWithAnyArgs().Publish(default!, default!, default);
		Assert.That(_events.TakeLast(3), Is.EqualTo(CleanupEvents));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task PublicationFailureDoesNotRollBackCommittedAcknowledgement(bool snapshotFailure)
	{
		IOException failure = new("Publication failed.");
		if (snapshotFailure)
		{
			_unit.CreateSnapshot(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(failure);
		}
		else
		{
			_storage.Publish(_session, Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(failure);
		}
		Assert.That(Assert.ThrowsAsync<IOException>(() => Acknowledge()), Is.SameAs(failure));
		await _unit.Received(1).CommitTransaction(Arg.Any<IUnitOfWorkTransaction>(), Arg.Any<CancellationToken>());
		await _unit.DidNotReceiveWithAnyArgs().RollbackTransaction(default!, default);
		Assert.That(_events.TakeLast(3), Is.EqualTo(CleanupEvents));
	}

	[TestCase(5)]
	[TestCase(7)]
	public async Task OlderAndRepeatedReceiptsDoNotSaveOrPublish(long recordedRevision)
	{
		_local.SyncRevision = recordedRevision;
		_local.SyncDateTime = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
		Assert.That((await Acknowledge()).IsSuccess, Is.True);
		await _unit.DidNotReceiveWithAnyArgs().SaveChanges(default);
		await _unit.DidNotReceiveWithAnyArgs().CreateSnapshot(default!, default);
		await _storage.DidNotReceiveWithAnyArgs().Publish(default!, default!, default);
	}

	[Test]
	public async Task SuccessPublishesOnlyAfterCommitAndCleansUpInOrder()
	{
		Assert.That((await Acknowledge()).IsSuccess, Is.True);
		Assert.That(_events, Is.EqualTo(SuccessEvents));
	}

	[Test]
	public async Task DatabaseOpenFailureStillCleansUp()
	{
		_unit.OpenSession(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new IOException("Open failed."));
		Assert.ThrowsAsync<IOException>(() => Acknowledge());
		await _unit.DidNotReceiveWithAnyArgs().BeginTransaction(default);
		Assert.That(_events, Is.EqualTo(CleanupEvents));
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012", Justification = "Configures substitute ValueTask returns; no asynchronous operation starts here.")]
	private void RecordCleanup()
	{
		_unit.CommitTransaction(Arg.Any<IUnitOfWorkTransaction>(), Arg.Any<CancellationToken>()).Returns(_ => Record("commit"));
		_unit.RollbackTransaction(Arg.Any<IUnitOfWorkTransaction>(), Arg.Any<CancellationToken>()).Returns(_ => Record("rollback"));
		_unit.CreateSnapshot(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => Record("snapshot"));
		_storage.Publish(_session, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => Record("publish"));
		_unit.CloseSession().Returns(_ => Record("database-close"));
		_session.DisposeAsync().Returns(_ => new ValueTask(Record("storage-dispose")));
		_access.DisposeAsync().Returns(_ => new ValueTask(Record("release")));
	}

	private Task Record(string value)
	{
		_events.Add(value);
		return Task.CompletedTask;
	}

	private async Task<AcknowledgeInfo> Acknowledge(CancellationToken cancellationToken = default)
	{
		using IServiceScope scope = _provider.CreateScope();
		return await scope.ServiceProvider.GetRequiredService<IMasterService>().Acknowledge(
			new AcknowledgeRequest { MasterDatasetKey = "master", LocalDatasetKey = "local", InstalledRevision = 5 }, cancellationToken);
	}
}
