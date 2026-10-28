using Business.Models.Enums;
using Business.Models.Constants;
using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Core.Entities;
using Exchange.Models.Synchronizations;
using Shared.Contracts;
using Synchronization.Contracts.Adapters;
using Synchronization.Contracts.Adapters.Master;
using Synchronization.Contracts.Services;
using Synchronization.Contracts.Services.Synchronizations;
using Synchronization.Impl.Operations.AcceptCapture;
using Synchronization.Impl.Operations.Acknowledge;
using Synchronization.Impl.Operations.CaptureChanges;
using Synchronization.Impl.Operations.Download;

namespace Synchronization.Impl.Services;

/// <summary>
/// Coordinates durable Local capture, submission, installation and acknowledgement.
/// Stores SyncContainer directly in LocalJson and restarts the workflow on each attempt.
/// Capture and tracking acceptance commit together before any remote submission.
/// Submission uncertainty retains original batch identities for safe replay.
/// Snapshot installation atomically prunes receipted batches and updates the installed revision.
/// Expected provider stops and caller cancellation return application outcomes.
/// Invalid data and Local persistence failures propagate without discarding pending work.
/// The host blocks business editing and supplies a fresh scope for each attempt.
/// Authorization remains the selected Master adapter's responsibility.
/// </summary>
internal sealed class SynchronizationService : ISynchronizationService
{
	private readonly SynchronizationGate _synchronizationGate;
	private readonly ISharedContext _sharedContext;
	private readonly ILocalAppUnitOfWork _unitOfWork;
	private readonly IMasterAdapter _masterAdapter;
	private readonly ICaptureChangesOperation _captureChangesOperation;
	private readonly IAcceptCaptureOperation _acceptCaptureOperation;
	private readonly IDownloadOperation _downloadOperation;
	private readonly IAcknowledgeOperation _acknowledgeOperation;

	public SynchronizationService(
		ISharedContext sharedContext,
		ILocalAppUnitOfWork unitOfWork,
		IMasterAdapter masterAdapter,
		ICaptureChangesOperation captureChangesOperation,
		IAcceptCaptureOperation acceptCaptureOperation,
		IDownloadOperation downloadOperation,
		IAcknowledgeOperation acknowledgeOperation,
		SynchronizationGate synchronizationGate)
	{
		_synchronizationGate = synchronizationGate;
		_sharedContext = sharedContext;
		_unitOfWork = unitOfWork;
		_masterAdapter = masterAdapter;
		_captureChangesOperation = captureChangesOperation;
		_acceptCaptureOperation = acceptCaptureOperation;
		_downloadOperation = downloadOperation;
		_acknowledgeOperation = acknowledgeOperation;
	}

	public async Task<SynchronizeInfo> Synchronize(CancellationToken cancellationToken = default)
	{
		try
		{
			await _synchronizationGate.Semaphore.WaitAsync(cancellationToken);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			return Result(null, SynchronizationOutcome.Cancelled, null, SynchronizationCheckpoint.None);
		}

		try
		{
			return await Run(cancellationToken);
		}
		finally
		{
			_synchronizationGate.Semaphore.Release();
		}
	}

	private async Task<SynchronizeInfo> Run(CancellationToken cancellationToken)
	{
		LocalJson? row = await _unitOfWork.LocalJsonRepo.GetById(EntitiesIds.LocalJsonId, CancellationToken.None);
		SyncContainer? container = ReadSyncContainer(row);
		SynchronizationCheckpoint checkpoint = SynchronizationCheckpoint.None;
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			SystemConfig system = (await _unitOfWork.SystemConfigRepo.GetAll(cancellationToken)).Single();
			LocalConfig local = (await _unitOfWork.LocalConfigRepo.GetAll(cancellationToken)).Single();
			if (string.IsNullOrWhiteSpace(system.MasterDatasetKey) || string.IsNullOrWhiteSpace(local.LocalDatasetKey))
			{
				throw new InvalidSyncBatchException("Synchronization requires both dataset identities.");
			}
			if (container is not null &&
			    (container.MasterDatasetKey != system.MasterDatasetKey ||
				container.LocalDatasetKey != local.LocalDatasetKey ||
				container.Revision != local.SnapshotRevision))
			{
				throw new InvalidSyncBatchException("Saved container does not match the Local dataset and installed revision.");
			}
			
			AuthorizationResult authorization =
				await _masterAdapter.CheckAuthorization(
					system.MasterDatasetKey,
					local.LocalDatasetKey,
					cancellationToken);
			if (authorization == AuthorizationResult.LoginRequired)
			{
				return Result(
					container,
					SynchronizationOutcome.AuthorizationRequired,
					null,
					checkpoint);
			}
			
			container = await Capture(container, system, local, cancellationToken);
			checkpoint = SynchronizationCheckpoint.Captured;
			cancellationToken.ThrowIfCancellationRequested();
			
			SynchronizeResponse merged =
				await _masterAdapter.Synchronize(new SynchronizeRequest { Container = container }, cancellationToken);
			if (merged.Revision < container.Revision || merged.Revision < system.CurrentRevision)
			{
				throw new InvalidSyncBatchException("Master returned a revision older than the installed database.");
			}
			checkpoint = SynchronizationCheckpoint.Submitted;
			
			if (container.Batches.Count != 0 || merged.Revision != container.Revision)
			{
				if (HasChanges(await _captureChangesOperation.Run(cancellationToken)))
				{
					throw new InvalidSyncBatchException("Uncaptured Local changes prevent snapshot installation.");
				}
				long installed = await _downloadOperation.Run(new DownloadRequest
				{
					MasterDatasetKey = container.MasterDatasetKey,
					LocalDatasetKey = container.LocalDatasetKey,
					MinimumRevision = merged.Revision
				}, container, cancellationToken);
				container = container with { Revision = installed };
			}
			checkpoint = SynchronizationCheckpoint.Installed;
			cancellationToken.ThrowIfCancellationRequested();
			
			AcknowledgeResponse result = await _acknowledgeOperation.Run(new AcknowledgeRequest
			{
				MasterDatasetKey = container.MasterDatasetKey,
				LocalDatasetKey = container.LocalDatasetKey,
				InstalledRevision = container.Revision
			}, cancellationToken);
			if (!result.IsSuccess)
			{
				return Result(
					container,
					SynchronizationOutcome.Deferred,
					SynchronizationStopReason.AcknowledgementNotAccepted,
					checkpoint);
			}
			return Result(container, SynchronizationOutcome.Complete, null, SynchronizationCheckpoint.None);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			return Result(
				container,
				SynchronizationOutcome.Cancelled,
				null,
				checkpoint);
		}
		catch (MasterBusyException)
		{
			return Result(
				container,
				SynchronizationOutcome.Deferred,
				SynchronizationStopReason.MasterBusy,
				checkpoint);
		}
		catch (MasterRequestException exception)
		{
			SynchronizationOutcome outcome = exception.Reason is SynchronizationStopReason.TokenMissing or
				SynchronizationStopReason.TokenExpired or SynchronizationStopReason.TokenRejected
				? SynchronizationOutcome.AuthorizationRequired : SynchronizationOutcome.Deferred;
			return Result(container, outcome, exception.Reason, checkpoint);
		}
	}

	private SyncContainer? ReadSyncContainer(LocalJson? row)
	{
		if (row is null || row.Json.Length == 0)
		{
			return null;
		}
		SyncContainer container =
			_sharedContext.JsonService.Deserialize<SyncContainer>(row.Json)
			?? throw new InvalidSyncBatchException("Synchronization JSON is empty.");
		if (container.FormatVersion != 1 ||
		    container.SchemaVersion != 1 ||
		    container.Revision < 0 ||
			string.IsNullOrWhiteSpace(container.MasterDatasetKey) ||
		    string.IsNullOrWhiteSpace(container.LocalDatasetKey))
		{
			throw new InvalidSyncBatchException("Saved synchronization container is invalid or unsupported.");
		}
		return container;
	}

	private async Task<SyncContainer> Capture(
		SyncContainer? previous,
		SystemConfig system,
		LocalConfig local,
		CancellationToken cancellationToken)
	{
		await using IUnitOfWorkTransaction transaction = await _unitOfWork.BeginTransaction(cancellationToken);
		try
		{
			SyncBatch batch = await _captureChangesOperation.Run(cancellationToken);
			SyncContainer container = new()
			{
				FormatVersion = 1,
				SchemaVersion = 1,
				Revision = local.SnapshotRevision,
				MasterDatasetKey = system.MasterDatasetKey,
				LocalDatasetKey = local.LocalDatasetKey
			};
			
			if (previous is not null)
			{
				container.Batches.AddRange(previous.Batches);
			}

			if (HasChanges(batch))
			{
				container.Batches.Add(batch);
			}
			
			LocalJson? row = await _unitOfWork.LocalJsonRepo.GetById(EntitiesIds.LocalJsonId, cancellationToken);
			string json = _sharedContext.JsonService.Serialize(container);
			if (row is null)
			{
				_unitOfWork.LocalJsonRepo.Add(new LocalJson { Json = json });
			}
			else
			{
				row.Json = json;
				_unitOfWork.LocalJsonRepo.Update(row);
			}
			
			await _unitOfWork.SaveChanges(cancellationToken);
			await _acceptCaptureOperation.Run(cancellationToken);
			await _unitOfWork.CommitTransaction(transaction, CancellationToken.None);
			return container;
		}
		catch
		{
			await _unitOfWork.RollbackTransaction(transaction, CancellationToken.None);
			throw;
		}
	}

	private static bool HasChanges(SyncBatch batch) => batch.Added.Count + batch.Updated.Count + batch.Deleted.Count != 0;

	private static SynchronizeInfo Result(
		SyncContainer? container,
		SynchronizationOutcome outcome,
		SynchronizationStopReason? reason,
		SynchronizationCheckpoint checkpoint) => new()
	{
		Outcome = outcome,
		Reason = reason,
		Checkpoint = checkpoint,
		Revision = container?.Revision
	};
}
