using System.Text.Json;
using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Core.Entities;
using Exchange.Models.Synchronizations;
using Shared.Contracts.Storage;
using Master.Contracts.Services;
using Master.Contracts.Services.Synchronizations;
using Master.Impl.Operations.ApplySyncBatch;
using Master.Impl.Utils;
using Shared.Contracts;
using Master.Impl.Operations.MasterStorage;

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
	private readonly IMasterStorageOperation _masterStorageOperation;
	private readonly IStorageLockFactory _storageLockFactory;

	public MasterService(
		ISharedContext sharedContext,
		IMasterAppUnitOfWork unitOfWork,
		IApplySyncBatchOperation applySyncBatchOperation,
		IMasterStorageOperation masterStorageOperation,
		IStorageLockFactory storageLockFactory)
	{
		_sharedContext = sharedContext;
		_unitOfWork = unitOfWork;
		_applySyncBatchOperation = applySyncBatchOperation;
		_masterStorageOperation = masterStorageOperation;
		_storageLockFactory = storageLockFactory;
	}

	public async Task<SynchronizeResponse> Synchronize(SynchronizeRequest request, CancellationToken cancellationToken = default)
	{
		ValidateRequest(request, cancellationToken);

		SyncContainer container = CopyContainer(request.Container);

		await using IStorageLock storageLock =
			await _storageLockFactory.TryAcquire(MasterStorageNames.Database(container.MasterDatasetKey), cancellationToken) ??
			throw new MasterBusyException();

		using CancellationTokenSource cancellation =
			CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, storageLock.CancellationToken);
		cancellation.Token.ThrowIfCancellationRequested();

		await using IMasterStorageSession session = await _masterStorageOperation.Open(container.MasterDatasetKey, storageLock, cancellation.Token);

		long revision = await ProcessDatabase(container, session, cancellation.Token);
		cancellation.Token.ThrowIfCancellationRequested();

		return new SynchronizeResponse { Revision = revision };
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
		SystemConfig config = await GetConfiguration(container.MasterDatasetKey, cancellationToken);
		LocalRegistration local = await GetRegistration(container.LocalDatasetKey, cancellationToken);
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
			await _masterStorageOperation.Publish(session, snapshotPath, cancellationToken);
		}
		finally
		{
			DeleteSnapshot(snapshotPath);
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

	private async Task<SystemConfig> GetConfiguration(string masterDatasetKey, CancellationToken cancellationToken)
	{
		ICollection<SystemConfig> configurations = await _unitOfWork.SystemConfigRepo.GetAll(cancellationToken);
		if (configurations.Count != 1 || configurations.Single().CurrentRevision < 0 ||
			configurations.Single().MasterDatasetKey != masterDatasetKey)
		{
			throw new InvalidSyncBatchException("The request does not identify the configured Master dataset.");
		}
		return configurations.Single();
	}

	private async Task<LocalRegistration> GetRegistration(string localDatasetKey, CancellationToken cancellationToken)
	{
		LocalRegistration? local = await _unitOfWork.LocalRegistrationRepo.GetByLocalDatasetKey(localDatasetKey, cancellationToken);
		if (local is null)
		{
			throw new InvalidSyncBatchException("The submitting Local is not registered on this Master.");
		}
		return local;
	}

	public async Task<DownloadResponse> Download(DownloadRequest request, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);
		cancellationToken.ThrowIfCancellationRequested();
		DownloadRequest download = request with { };
		if (string.IsNullOrWhiteSpace(download.MasterDatasetKey) || string.IsNullOrWhiteSpace(download.LocalDatasetKey) ||
			download.MinimumRevision < 0)
		{
			throw new InvalidSyncBatchException("Download requires both dataset identities and a nonnegative minimum revision.");
		}

		string snapshotPath = Path.Combine(Path.GetTempPath(), $"dehb-download-{Guid.NewGuid():N}.db");
		try
		{
			long revision = await PrepareDownload(download, snapshotPath, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			return new DownloadResponse
			{
				Revision = revision,
				Content = _sharedContext.FileService.Open(snapshotPath, new FileStreamOptions
				{
					Mode = FileMode.Open,
					Access = FileAccess.Read,
					Share = FileShare.Read,
					Options = FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose
				})
			};
		}
		catch
		{
			DeleteSnapshot(snapshotPath);
			throw;
		}
	}

	private async Task<long> PrepareDownload(DownloadRequest request, string snapshotPath, CancellationToken cancellationToken)
	{
		await using IStorageLock storageLock =
			await _storageLockFactory.TryAcquire(MasterStorageNames.Database(request.MasterDatasetKey), cancellationToken) ??
			throw new MasterBusyException();
		using CancellationTokenSource cancellation =
			CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, storageLock.CancellationToken);
		cancellation.Token.ThrowIfCancellationRequested();
		await using IMasterStorageSession session = await _masterStorageOperation.Open(request.MasterDatasetKey, storageLock, cancellation.Token);
		try
		{
			await _unitOfWork.OpenSession(session.StoragePath, cancellation.Token);
			SystemConfig config = await GetConfiguration(request.MasterDatasetKey, cancellation.Token);
			await GetRegistration(request.LocalDatasetKey, cancellation.Token);
			long revision = config.CurrentRevision;
			if (revision < request.MinimumRevision)
			{
				throw new InvalidSyncBatchException("The Master revision is below the requested minimum revision.");
			}
			await _unitOfWork.CreateSnapshot(snapshotPath, cancellation.Token);
			cancellation.Token.ThrowIfCancellationRequested();
			return revision;
		}
		finally
		{
			await _unitOfWork.CloseSession();
		}
	}

	public async Task<AcknowledgeResponse> Acknowledge(AcknowledgeRequest request, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);
		cancellationToken.ThrowIfCancellationRequested();
		AcknowledgeRequest acknowledgement = request with { };
		if (string.IsNullOrWhiteSpace(acknowledgement.MasterDatasetKey) || string.IsNullOrWhiteSpace(acknowledgement.LocalDatasetKey) ||
			acknowledgement.InstalledRevision < 0)
		{
			throw new InvalidSyncBatchException("Acknowledgement requires both dataset identities and a nonnegative installed revision.");
		}

		await using IStorageLock storageLock =
			await _storageLockFactory.TryAcquire(MasterStorageNames.Database(acknowledgement.MasterDatasetKey), cancellationToken) ??
			throw new MasterBusyException();
		using CancellationTokenSource cancellation =
			CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, storageLock.CancellationToken);
		cancellation.Token.ThrowIfCancellationRequested();
		await using IMasterStorageSession session = await _masterStorageOperation.Open(acknowledgement.MasterDatasetKey, storageLock, cancellation.Token);
		try
		{
			await _unitOfWork.OpenSession(session.StoragePath, cancellation.Token);
			if (await AcknowledgeTransaction(acknowledgement, cancellation.Token))
			{
				await PublishBatch(session, cancellation.Token);
			}
			cancellation.Token.ThrowIfCancellationRequested();
			return new AcknowledgeResponse { IsSuccess = true };
		}
		finally
		{
			await _unitOfWork.CloseSession();
		}
	}

	private async Task<bool> AcknowledgeTransaction(AcknowledgeRequest request, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		await using IUnitOfWorkTransaction transaction = await _unitOfWork.BeginTransaction(cancellationToken);
		try
		{
			bool changed = await RecordAcknowledgement(request, cancellationToken);
			await CommitBatch(transaction, cancellationToken);
			return changed;
		}
		catch
		{
			await RollbackBatch(transaction);
			throw;
		}
	}

	private async Task<bool> RecordAcknowledgement(AcknowledgeRequest request, CancellationToken cancellationToken)
	{
		SystemConfig config = await GetConfiguration(request.MasterDatasetKey, cancellationToken);
		LocalRegistration local = await GetRegistration(request.LocalDatasetKey, cancellationToken);
		if (request.InstalledRevision > config.CurrentRevision)
		{
			throw new InvalidSyncBatchException("The installed revision exceeds the current Master revision.");
		}
		if (request.InstalledRevision < local.SyncRevision ||
			(request.InstalledRevision == local.SyncRevision && local.SyncDateTime is not null))
		{
			return false;
		}
		local.SyncRevision = request.InstalledRevision;
		local.SyncDateTime = _sharedContext.DateTimeService.UtcNow;
		_unitOfWork.LocalRegistrationRepo.Update(local);
		await _unitOfWork.SaveChanges(cancellationToken);
		return true;
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
}
