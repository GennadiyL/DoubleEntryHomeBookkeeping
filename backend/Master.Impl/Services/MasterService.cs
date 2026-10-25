using System.Text.Json;
using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Core.Entities;
using Exchange.Models.Synchronizations;
using Shared.Contracts.Storage;
using Master.Contracts.Adapters;
using Master.Contracts.Adapters.MasterStorage;
using Master.Contracts.Services;
using Master.Contracts.Services.Synchronizations;
using Master.Impl.Operations.ApplySyncBatch;
using Master.Impl.Utils;
using Shared.Contracts;

namespace Master.Impl.Services;

/// <summary>
/// Coordinates ordered Local submissions against the scoped Master database.
/// Infrastructure authorizes identities before entering this business service.
/// Validates exchange versions, dataset identity and Local registration.
/// Holds exclusive Master access across every batch and links ownership loss to cancellation.
/// Opens storage under that access and publishes each new committed batch before continuing.
/// Each batch uses its own serializable database transaction, acquired before reads.
/// The database transaction serializes receipt checks and revision allocation.
/// Business changes, their receipt and the Master revision commit together.
/// Retries skip matching receipts and reject changed content under the same key.
/// Merge failure rolls back the active transaction; publication failure stops further batches.
/// A failed remote publication requires a new scope and storage session for retry.
/// Download and acknowledgement are implemented separately.
/// </summary>
internal sealed class MasterService : IMasterService
{
	private static JsonSerializerOptions JsonOptions => new() { ReferenceHandler = null };
	private readonly ISharedContext _sharedContext;
	private readonly IMasterAppUnitOfWork _unitOfWork;
	private readonly IApplySyncBatchOperation _applySyncBatchOperation;
	private readonly IStorageLockFactory _storageLockFactory;
	private readonly IMasterStorageAdapter _storage;

	public MasterService(ISharedContext sharedContext, IMasterAppUnitOfWork unitOfWork,
		IApplySyncBatchOperation applySyncBatchOperation, IStorageLockFactory storageLockFactory, IMasterStorageAdapter storage)
	{
		_sharedContext = sharedContext;
		_unitOfWork = unitOfWork;
		_applySyncBatchOperation = applySyncBatchOperation;
		_storageLockFactory = storageLockFactory;
		_storage = storage;
	}

	public async Task<SynchronizeInfo> Synchronize(SynchronizeRequest request, CancellationToken cancellationToken = default)
	{
		ValidateRequest(request, cancellationToken);

		SyncContainer container = CopyContainer(request.Container);

		await using IStorageLock storageLock =
			await _storageLockFactory.TryAcquire(MasterStorageNames.Database(container.MasterDatasetKey), cancellationToken) ??
			throw new MasterBusyException();

		using CancellationTokenSource cancellation =
			CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, storageLock.CancellationToken);
		cancellation.Token.ThrowIfCancellationRequested();

		await using IMasterStorageSession session = await _storage.Open(container.MasterDatasetKey, storageLock, cancellation.Token);

		long revision = await ProcessDatabase(container, session, cancellation.Token);
		cancellation.Token.ThrowIfCancellationRequested();

		return new SynchronizeInfo { Revision = revision };
	}

	private async Task<long> ProcessDatabase(SyncContainer container, IMasterStorageSession session, CancellationToken cancellationToken)
	{
		try
		{
			await _unitOfWork.OpenSession(session.StoragePath, cancellationToken);
			return await ProcessBatches(container, session, cancellationToken);
		}
		finally
		{
			await _unitOfWork.CloseSession();
		}
	}

	private static void ValidateRequest(SynchronizeRequest request, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(request.Container);
		cancellationToken.ThrowIfCancellationRequested();
		SyncContainer container = request.Container;
		if (container.FormatVersion != 1 || container.SchemaVersion != 1 ||
			string.IsNullOrWhiteSpace(container.MasterDatasetKey) || string.IsNullOrWhiteSpace(container.LocalDatasetKey))
		{
			throw new InvalidSyncBatchException("Synchronization requires format/schema version 1 and both dataset identities.");
		}
	}

	private SyncContainer CopyContainer(SyncContainer container)
	{
		try
		{
			return _sharedContext.JsonService.Deserialize<SyncContainer>(
				_sharedContext.JsonService.Serialize(container, JsonOptions), JsonOptions)!;
		}
		catch (JsonException exception)
		{
			throw new InvalidSyncBatchException($"Invalid synchronization document: {exception.Message}");
		}
	}

	private async Task<long> ProcessBatches(SyncContainer container, IMasterStorageSession session, CancellationToken cancellationToken)
	{
		long revision = 0;
		for (int index = 0; index < Math.Max(1, container.Batches.Count); index++)
		{
			(long currentRevision, bool applied) = await ProcessBatchTransaction(container, index, cancellationToken);
			revision = currentRevision;
			if (applied)
			{
				await PublishBatch(session, cancellationToken);
			}
		}
		return revision;
	}

	private async Task<(long Revision, bool Applied)> ProcessBatchTransaction(SyncContainer container, int index, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		await using IUnitOfWorkTransaction transaction = await _unitOfWork.BeginTransaction(cancellationToken);
		try
		{
			(long revision, bool applied) = await ProcessSubmission(container, index, cancellationToken);
			await CommitBatch(transaction, cancellationToken);
			return (revision, applied);
		}
		catch
		{
			await RollbackBatch(transaction);
			throw;
		}
	}

	private async Task<(long Revision, bool Applied)> ProcessSubmission(SyncContainer container, int index, CancellationToken cancellationToken)
	{
		SystemConfig config = await GetConfiguration(container, cancellationToken);
		LocalRegistration local = await GetRegistration(container, cancellationToken);
		long previousRevision = config.CurrentRevision;
		if (container.Batches.Count != 0)
		{
			await ApplyBatch(container.Batches[index], config, local, cancellationToken);
		}
		return (config.CurrentRevision, config.CurrentRevision != previousRevision);
	}

	private async Task PublishBatch(IMasterStorageSession session, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		string snapshotPath = session.StoragePath + $".snapshot-{Guid.NewGuid():N}.db";
		try
		{
			await _unitOfWork.CreateSnapshot(snapshotPath, cancellationToken);
			await session.Publish(snapshotPath, cancellationToken);
		}
		finally
		{
			DeleteSnapshot(snapshotPath);
		}
	}

	private void DeleteSnapshot(string snapshotPath)
	{
		try
		{
			_sharedContext.FileService.Delete(snapshotPath);
		}
		catch (IOException exception)
		{
			_sharedContext.LogService.Error("Master database snapshot cleanup failed.", exception);
		}
		catch (UnauthorizedAccessException exception)
		{
			_sharedContext.LogService.Error("Master database snapshot cleanup failed.", exception);
		}
	}

	private async Task ApplyBatch(SyncBatch batch, SystemConfig config, LocalRegistration local, CancellationToken cancellationToken)
	{
		string hash = GetBatchHash(batch);
		LocalSyncBatch? receipt = await _unitOfWork.LocalSyncBatchRepo.GetBySyncKey(local.Id, batch.SyncKey, cancellationToken);
		if (receipt is not null)
		{
			ValidateReceipt(receipt, hash);
		}
		else
		{
			await ApplyNewBatch(batch, config, local, hash, cancellationToken);
		}
	}

	private string GetBatchHash(SyncBatch batch)
	{
		if (batch is null || string.IsNullOrWhiteSpace(batch.SyncKey))
		{
			throw new InvalidSyncBatchException("Every batch requires its immutable SyncKey.");
		}
		return SyncBatchHash.Calculate(_sharedContext.JsonService.Serialize(batch, JsonOptions));
	}

	private static void ValidateReceipt(LocalSyncBatch receipt, string hash)
	{
		if (receipt.PayloadHash != hash)
		{
			throw new InvalidSyncBatchException($"Batch {receipt.SyncKey} was already applied with different content.");
		}
	}

	private async Task ApplyNewBatch(SyncBatch batch, SystemConfig config, LocalRegistration local,
		string hash, CancellationToken cancellationToken)
	{
		long revision = AllocateRevision(config);
		await _applySyncBatchOperation.Run(batch, local.Id, revision, cancellationToken);
		RecordAppliedBatch(batch, config, local, hash, revision);
		await _unitOfWork.SaveChanges(cancellationToken);
	}

	private static long AllocateRevision(SystemConfig config)
	{
		if (config.CurrentRevision == long.MaxValue)
		{
			throw new InvalidSyncBatchException("The Master revision is exhausted.");
		}
		return config.CurrentRevision + 1;
	}

	private void RecordAppliedBatch(SyncBatch batch, SystemConfig config, LocalRegistration local, string hash, long revision)
	{
		_unitOfWork.LocalSyncBatchRepo.Add(new LocalSyncBatch
		{
			Id = Guid.NewGuid(), LocalRegistrationId = local.Id, SyncKey = batch.SyncKey,
			Revision = revision, PayloadHash = hash
		});
		config.CurrentRevision = revision;
		_unitOfWork.SystemConfigRepo.Update(config);
	}

	private async Task CommitBatch(IUnitOfWorkTransaction transaction, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		await _unitOfWork.CommitTransaction(transaction, cancellationToken);
	}

	private async Task RollbackBatch(IUnitOfWorkTransaction transaction)
	{
		try
		{
			await _unitOfWork.RollbackTransaction(transaction, CancellationToken.None);
		}
		catch (Exception exception)
		{
			_sharedContext.LogService.Error("Master synchronization rollback failed.", exception);
		}
	}

	public Task<DownloadInfo> Download(DownloadRequest request, CancellationToken cancellationToken = default) =>
		throw new NotImplementedException();

	public Task<AcknowledgeInfo> Acknowledge(AcknowledgeRequest request, CancellationToken cancellationToken = default) =>
		throw new NotImplementedException();

	private async Task<SystemConfig> GetConfiguration(SyncContainer container, CancellationToken cancellationToken)
	{
		ICollection<SystemConfig> configurations = await _unitOfWork.SystemConfigRepo.GetAll(cancellationToken);
		if (configurations.Count != 1 || configurations.Single().CurrentRevision < 0 ||
			configurations.Single().MasterDatasetKey != container.MasterDatasetKey)
		{
			throw new InvalidSyncBatchException("The request does not identify the configured Master dataset.");
		}
		return configurations.Single();
	}

	private async Task<LocalRegistration> GetRegistration(SyncContainer container, CancellationToken cancellationToken)
	{
		LocalRegistration? local = await _unitOfWork.LocalRegistrationRepo.GetByLocalDatasetKey(container.LocalDatasetKey, cancellationToken);
		if (local is null)
		{
			throw new InvalidSyncBatchException("The submitting Local is not registered on this Master.");
		}
		return local;
	}
}
