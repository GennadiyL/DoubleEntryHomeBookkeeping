using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Core.Entities;
using Exchange.Models.Enums;
using Exchange.Models.Synchronizations;
using Master.Contracts.Access;
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
	private IMasterLock _masterLock = null!;
	private IMasterLockHandle _handle = null!;
	private CancellationTokenSource _lost = null!;
	private bool _releasedBeforeRollback;

	[SetUp]
	public void SetUp()
	{
		_unit = Substitute.For<IMasterAppUnitOfWork>();
		_apply = Substitute.For<IApplySyncBatchOperation>();
		_masterLock = Substitute.For<IMasterLock>();
		_handle = Substitute.For<IMasterLockHandle>();
		_lost = new CancellationTokenSource();
		_releasedBeforeRollback = false;
		_handle.LockLost.Returns(_lost.Token);
		_masterLock.TryAcquire("master", Arg.Any<CancellationToken>()).Returns(_handle);
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
		_masterLock.TryAcquire("master", Arg.Any<CancellationToken>()).Returns((IMasterLockHandle?)null);
		Assert.ThrowsAsync<MasterBusyException>(() => Synchronize(Request("first")));
		await _unit.DidNotReceive().BeginTransaction(Arg.Any<CancellationToken>());
		await _apply.DidNotReceiveWithAnyArgs().Run(default!, default, default, default);
		await _handle.DidNotReceive().DisposeAsync();
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
		await _masterLock.Received(1).TryAcquire("master", Arg.Any<CancellationToken>());
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
		await _handle.Received(1).DisposeAsync();
	}

	[Test]
	public async Task AcquisitionFailurePropagatesWithoutDatabaseAccess()
	{
		InvalidOperationException failure = new("Storage unavailable.");
		_masterLock.TryAcquire("master", Arg.Any<CancellationToken>()).ThrowsAsync(failure);
		Assert.That(Assert.ThrowsAsync<InvalidOperationException>(() => Synchronize(Request("first"))), Is.SameAs(failure));
		await _unit.DidNotReceive().BeginTransaction(Arg.Any<CancellationToken>());
		await _handle.DidNotReceive().DisposeAsync();
	}

	[Test]
	public async Task PreCancelledRequestDoesNotAcquireAccess()
	{
		Assert.CatchAsync<OperationCanceledException>(() => Synchronize(Request("first"), new CancellationToken(true)));
		await _masterLock.DidNotReceiveWithAnyArgs().TryAcquire(default!, default);
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
		if (localAccess)
		{
			services.AddLocalMasterAccess();
		}
		else
		{
			services.AddSingleton(_masterLock);
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
