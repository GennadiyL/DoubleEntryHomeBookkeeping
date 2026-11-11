using Business.Models.Constants;
using System.Text.Json;
using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
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
/// Downloads into a temporary working folder and publishes each new committed revision.
/// Each batch uses its own serializable database transaction, acquired before reads.
/// The database transaction serializes receipt checks and revision allocation.
/// Business changes, their receipt and the Master revision commit together.
/// Retries skip matching receipts and reject changed content under the same key.
/// Merge failure rolls back the active transaction; publication failure stops further batches.
/// A failed publication requires a new API scope and fresh working copy for retry.
/// All operations use factory units and close connections before temporary directory cleanup.
/// </summary>
internal sealed class MasterService : IMasterService
{
	private static JsonSerializerOptions JsonOptions => new() { ReferenceHandler = null };
	private readonly ISharedContext _sharedContext;
	private readonly IUnitOfWorkFactory _unitOfWorkFactory;
	private readonly ICreateSnapshotCommand _createSnapshotCommand;
	private readonly IApplySyncBatchOperation _applySyncBatchOperation;
	private readonly IMasterStorageOperation _masterStorageOperation;
	private readonly IStorageLockFactory _storageLockFactory;

	public MasterService(
		ISharedContext sharedContext,
		IUnitOfWorkFactory unitOfWorkFactory,
		ICreateSnapshotCommand createSnapshotCommand,
		IApplySyncBatchOperation applySyncBatchOperation,
		IMasterStorageOperation masterStorageOperation,
		IStorageLockFactory storageLockFactory)
	{
		_sharedContext = sharedContext;
		_unitOfWorkFactory = unitOfWorkFactory;
		_createSnapshotCommand = createSnapshotCommand;
		_applySyncBatchOperation = applySyncBatchOperation;
		_masterStorageOperation = masterStorageOperation;
		_storageLockFactory = storageLockFactory;
	}

	public async Task<SynchronizeResponse> Synchronize(SynchronizeRequest request, CancellationToken cancellationToken = default)
	{
		ValidateRequest(request, cancellationToken);
		SyncContainer container = CopyContainer(request.Container);
		await using IStorageLock storageLock =
			await _storageLockFactory.TryAcquire(MasterStorageNames.MasterRelativePath(container.MasterDatasetKey), cancellationToken) ??
			throw new MasterBusyException();
		using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(
			cancellationToken, storageLock.CancellationToken);
		string directory = Path.Combine(
			_sharedContext.ConfigurationService.GetTempDirectory(), MasterStorageNames.Directory(container.MasterDatasetKey));
		try
		{
			await _masterStorageOperation.DownloadMaster(
				container.MasterDatasetKey,
				Path.Combine(directory, StorageValues.MasterFileName),
				storageLock,
				cancellation.Token);
			IMasterUnitOfWork unitOfWork = _unitOfWorkFactory.CreateMaster(container.MasterDatasetKey);
			long revision = await ProcessBatches(
				unitOfWork,
				container,
				storageLock,
				cancellation.Token);
			cancellation.Token.ThrowIfCancellationRequested();
			return new SynchronizeResponse { Revision = revision };
		}
		finally
		{
			await _unitOfWorkFactory.CloseMaster(container.MasterDatasetKey);
			_masterStorageOperation.Close(directory);
		}
	}

	private static void ValidateRequest(SynchronizeRequest request, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(request.Container);
		cancellationToken.ThrowIfCancellationRequested();
		SyncContainer container = request.Container;
		if (container.FormatVersion != 1 ||
			container.SchemaVersion != 1 ||
			string.IsNullOrWhiteSpace(container.MasterDatasetKey) ||
			string.IsNullOrWhiteSpace(container.LocalDatasetKey))
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

	private async Task<long> ProcessBatches(
		IMasterUnitOfWork unitOfWork,
		SyncContainer container,
		IStorageLock storageLock,
		CancellationToken cancellationToken)
	{
		long revision = 0;
		for (int index = 0; index < Math.Max(1, container.Batches.Count); index++)
		{
			(long currentRevision, bool applied) = await ProcessBatchTransaction(
				unitOfWork,
				container,
				index,
				cancellationToken);
			revision = currentRevision;
			if (applied)
			{
				await UploadRevision(
					unitOfWork.MasterDatasetKey,
					storageLock,
					revision,
					cancellationToken);
			}
		}
		return revision;
	}

	private async Task<(long Revision, bool Applied)> ProcessBatchTransaction(
		IMasterUnitOfWork unitOfWork,
		SyncContainer container,
		int index,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		await using IUnitOfWorkTransaction transaction = await unitOfWork.BeginTransaction(cancellationToken);
		try
		{
			(long revision, bool applied) = await ProcessSubmission(
				unitOfWork,
				container,
				index,
				cancellationToken);
			await CommitBatch(
				unitOfWork,
				transaction,
				cancellationToken);
			return (revision, applied);
		}
		catch
		{
			await RollbackBatch(unitOfWork, transaction);
			throw;
		}
	}

	private async Task<(long Revision, bool Applied)> ProcessSubmission(
		IMasterUnitOfWork unitOfWork,
		SyncContainer container,
		int index,
		CancellationToken cancellationToken)
	{
		SystemConfig config = await GetConfiguration(
			unitOfWork,
			container.MasterDatasetKey,
			cancellationToken);
		LocalRegistration local = await GetRegistration(
			unitOfWork,
			container.LocalDatasetKey,
			cancellationToken);
		long previousRevision = config.CurrentRevision;
		if (container.Batches.Count != 0)
		{
			await ApplyBatch(
				unitOfWork,
				container.Batches[index],
				config,
				local,
				cancellationToken);
		}
		return (config.CurrentRevision, config.CurrentRevision != previousRevision);
	}

	private async Task UploadRevision(
		string masterDatasetKey,
		IStorageLock storageLock,
		long revision,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		await _createSnapshotCommand.CreateSnapshot(masterDatasetKey, cancellationToken);
		string directory = Path.Combine(
			_sharedContext.ConfigurationService.GetTempDirectory(), MasterStorageNames.Directory(masterDatasetKey));
		string sourceFilePath = Path.Combine(directory, MasterStorageNames.SnapshotFile(revision));
		await _masterStorageOperation.UploadSnapshot(
			masterDatasetKey,
			revision,
			sourceFilePath,
			cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
		await _masterStorageOperation.UploadMaster(
			masterDatasetKey,
			sourceFilePath,
			storageLock,
			cancellationToken);
	}

	private async Task ApplyBatch(
		IMasterUnitOfWork unitOfWork,
		SyncBatch batch,
		SystemConfig config,
		LocalRegistration local,
		CancellationToken cancellationToken)
	{
		string hash = GetBatchHash(batch);
		LocalSyncBatch? receipt = await unitOfWork.LocalSyncBatchRepo.GetBySyncKey(local.Id, batch.SyncKey, cancellationToken);
		if (receipt is not null)
		{
			ValidateReceipt(receipt, hash);
		}
		else
		{
			await ApplyNewBatch(
				unitOfWork,
				batch,
				config,
				local,
				hash,
				cancellationToken);
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

	private async Task ApplyNewBatch(
		IMasterUnitOfWork unitOfWork,
		SyncBatch batch,
		SystemConfig config,
		LocalRegistration local,
		string hash,
		CancellationToken cancellationToken)
	{
		long revision = AllocateRevision(config);
		await _applySyncBatchOperation.Run(
			config.MasterDatasetKey,
			batch,
			local.Id,
			revision,
			cancellationToken);
		RecordAppliedBatch(
			unitOfWork,
			batch,
			config,
			local,
			hash,
			revision);
		await unitOfWork.SaveChanges(cancellationToken);
	}

	private static long AllocateRevision(SystemConfig config)
	{
		if (config.CurrentRevision == long.MaxValue)
		{
			throw new InvalidSyncBatchException("The Master revision is exhausted.");
		}
		return config.CurrentRevision + 1;
	}

	private void RecordAppliedBatch(
		IMasterUnitOfWork unitOfWork,
		SyncBatch batch,
		SystemConfig config,
		LocalRegistration local,
		string hash,
		long revision)
	{
		unitOfWork.LocalSyncBatchRepo.Add(new LocalSyncBatch
		{
			Id = Guid.NewGuid(),
			LocalRegistrationId = local.Id,
			SyncKey = batch.SyncKey,
			Revision = revision,
			PayloadHash = hash
		});
		config.CurrentRevision = revision;
		unitOfWork.SystemConfigRepo.Update(config);
	}

	private async Task CommitBatch(
		IMasterUnitOfWork unitOfWork,
		IUnitOfWorkTransaction transaction,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		await unitOfWork.CommitTransaction(transaction, cancellationToken);
	}

	private async Task RollbackBatch(
		IMasterUnitOfWork unitOfWork,
		IUnitOfWorkTransaction transaction)
	{
		try
		{
			await unitOfWork.RollbackTransaction(transaction, CancellationToken.None);
		}
		catch (Exception exception)
		{
			_sharedContext.LogService.Error("Master synchronization rollback failed.", exception);
		}
	}

	private async Task<SystemConfig> GetConfiguration(
		IMasterUnitOfWork unitOfWork,
		string masterDatasetKey,
		CancellationToken cancellationToken)
	{
		ICollection<SystemConfig> configurations = await unitOfWork.SystemConfigRepo.GetAll(cancellationToken);
		if (configurations.Count != 1 ||
			configurations.Single().CurrentRevision < 0 ||
			configurations.Single().MasterDatasetKey != masterDatasetKey)
		{
			throw new InvalidSyncBatchException("The request does not identify the configured Master dataset.");
		}
		return configurations.Single();
	}

	private async Task<LocalRegistration> GetRegistration(
		IMasterUnitOfWork unitOfWork,
		string localDatasetKey,
		CancellationToken cancellationToken)
	{
		LocalRegistration? local = await unitOfWork.LocalRegistrationRepo.GetByLocalDatasetKey(localDatasetKey, cancellationToken);
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
		if (string.IsNullOrWhiteSpace(download.MasterDatasetKey) ||
			string.IsNullOrWhiteSpace(download.LocalDatasetKey) ||
			download.MinimumRevision < 0)
		{
			throw new InvalidSyncBatchException("Download requires both dataset identities and a nonnegative minimum revision.");
		}
		string directory = Path.Combine(_sharedContext.ConfigurationService.GetTempDirectory(), StorageValues.DownloadDirectoryName(Guid.NewGuid()));
		MemoryStream content = new();
		try
		{
			await _masterStorageOperation.DownloadMaster(
				download.MasterDatasetKey,
				Path.Combine(directory, StorageValues.MasterFileName),
				null,
				cancellationToken);
			long revision;
			try
			{
				IMasterUnitOfWork unitOfWork = _unitOfWorkFactory.CreateMaster(download.MasterDatasetKey, directory);
				SystemConfig config = await GetConfiguration(
					unitOfWork,
					download.MasterDatasetKey,
					cancellationToken);
				await GetRegistration(
					unitOfWork,
					download.LocalDatasetKey,
					cancellationToken);
				revision = config.CurrentRevision;
				if (revision < download.MinimumRevision)
				{
					throw new InvalidSyncBatchException("The Master revision is below the requested minimum revision.");
				}
			}
			finally
			{
				await _unitOfWorkFactory.CloseMaster(download.MasterDatasetKey, directory);
			}
			string path = Path.Combine(directory, StorageValues.MasterFileName);
			if (revision != 0)
			{
				path = Path.Combine(directory, MasterStorageNames.SnapshotFile(revision));
				await _masterStorageOperation.DownloadSnapshot(
					download.MasterDatasetKey,
					revision,
					path,
					cancellationToken);
			}
			await using (FileStream source = _sharedContext.FileService.Open(path, new FileStreamOptions
			{
				Mode = FileMode.Open,
				Access = FileAccess.Read,
				Share = FileShare.Read,
				Options = FileOptions.Asynchronous | FileOptions.SequentialScan
			}))
			{
				await source.CopyToAsync(content, cancellationToken);
			}
			cancellationToken.ThrowIfCancellationRequested();
			content.Position = 0;
			return new DownloadResponse { Revision = revision, Content = content };
		}
		catch
		{
			await content.DisposeAsync();
			throw;
		}
		finally
		{
			_masterStorageOperation.Close(directory);
		}
	}

	public async Task<AcknowledgeResponse> Acknowledge(AcknowledgeRequest request, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);
		cancellationToken.ThrowIfCancellationRequested();
		AcknowledgeRequest acknowledgement = request with { };
		if (string.IsNullOrWhiteSpace(acknowledgement.MasterDatasetKey) ||
			string.IsNullOrWhiteSpace(acknowledgement.LocalDatasetKey) ||
			acknowledgement.InstalledRevision < 0)
		{
			throw new InvalidSyncBatchException("Acknowledgement requires both dataset identities and a nonnegative installed revision.");
		}
		await using IStorageLock storageLock =
			await _storageLockFactory.TryAcquire(MasterStorageNames.MasterRelativePath(acknowledgement.MasterDatasetKey), cancellationToken) ??
			throw new MasterBusyException();
		using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(
			cancellationToken, storageLock.CancellationToken);
		string directory = Path.Combine(
			_sharedContext.ConfigurationService.GetTempDirectory(), MasterStorageNames.Directory(acknowledgement.MasterDatasetKey));
		try
		{
			await _masterStorageOperation.DownloadMaster(
				acknowledgement.MasterDatasetKey,
				Path.Combine(directory, StorageValues.MasterFileName),
				storageLock,
				cancellation.Token);
			IMasterUnitOfWork unitOfWork = _unitOfWorkFactory.CreateMaster(acknowledgement.MasterDatasetKey);
			if (await AcknowledgeTransaction(
				unitOfWork,
				acknowledgement,
				cancellation.Token))
			{
				await _createSnapshotCommand.CreateSnapshot(acknowledgement.MasterDatasetKey, cancellation.Token);
				SystemConfig config = await GetConfiguration(
					unitOfWork,
					acknowledgement.MasterDatasetKey,
					cancellation.Token);
				await _masterStorageOperation.UploadMaster(
					acknowledgement.MasterDatasetKey,
					Path.Combine(directory, MasterStorageNames.SnapshotFile(config.CurrentRevision)),
					storageLock,
					cancellation.Token);
			}
			cancellation.Token.ThrowIfCancellationRequested();
			return new AcknowledgeResponse { IsSuccess = true };
		}
		finally
		{
			await _unitOfWorkFactory.CloseMaster(acknowledgement.MasterDatasetKey);
			_masterStorageOperation.Close(directory);
		}
	}

	private async Task<bool> AcknowledgeTransaction(
		IMasterUnitOfWork unitOfWork,
		AcknowledgeRequest request,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		await using IUnitOfWorkTransaction transaction = await unitOfWork.BeginTransaction(cancellationToken);
		try
		{
			bool changed = await RecordAcknowledgement(
				unitOfWork,
				request,
				cancellationToken);
			await CommitBatch(
				unitOfWork,
				transaction,
				cancellationToken);
			return changed;
		}
		catch
		{
			await RollbackBatch(unitOfWork, transaction);
			throw;
		}
	}

	private async Task<bool> RecordAcknowledgement(
		IMasterUnitOfWork unitOfWork,
		AcknowledgeRequest request,
		CancellationToken cancellationToken)
	{
		SystemConfig config = await GetConfiguration(
			unitOfWork,
			request.MasterDatasetKey,
			cancellationToken);
		LocalRegistration local = await GetRegistration(
			unitOfWork,
			request.LocalDatasetKey,
			cancellationToken);
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
		unitOfWork.LocalRegistrationRepo.Update(local);
		await unitOfWork.SaveChanges(cancellationToken);
		return true;
	}

}
