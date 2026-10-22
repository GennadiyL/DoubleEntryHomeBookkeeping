using System.Text.Json;
using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Core.Entities;
using Exchange.Models.Synchronizations;
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
/// Each batch uses its own serializable database transaction, acquired before reads.
/// The database transaction serializes receipt checks and revision allocation.
/// Business changes, their receipt and the Master revision commit together.
/// Retries skip matching receipts and reject changed content under the same key.
/// Cancellation or failure rolls back the current batch, preserving earlier commits.
/// Download and acknowledgement are implemented separately.
/// </summary>
internal sealed class MasterService : IMasterService
{
	private static JsonSerializerOptions JsonOptions => new() { ReferenceHandler = null };
	private readonly ISharedContext _sharedContext;
	private readonly IMasterAppUnitOfWork _unitOfWork;
	private readonly IApplySyncBatchOperation _applySyncBatchOperation;

	public MasterService(ISharedContext sharedContext, IMasterAppUnitOfWork unitOfWork,
		IApplySyncBatchOperation applySyncBatchOperation)
	{
		_sharedContext = sharedContext;
		_unitOfWork = unitOfWork;
		_applySyncBatchOperation = applySyncBatchOperation;
	}

	public async Task<SynchronizeInfo> Synchronize(SynchronizeRequest request, CancellationToken cancellationToken = default)
	{
		ValidateRequest(request, cancellationToken);
		SyncContainer container = CopyContainer(request.Container);
		long revision = await ProcessBatches(container, cancellationToken);
		return new SynchronizeInfo { Revision = revision };
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

	private async Task<long> ProcessBatches(SyncContainer container, CancellationToken cancellationToken)
	{
		long revision = 0;
		for (int index = 0; index < Math.Max(1, container.Batches.Count); index++)
		{
			revision = await ProcessBatchTransaction(container, index, cancellationToken);
		}
		return revision;
	}

	private async Task<long> ProcessBatchTransaction(SyncContainer container, int index, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		await using IUnitOfWorkTransaction transaction = await _unitOfWork.BeginTransaction(cancellationToken);
		try
		{
			long revision = await ProcessSubmission(container, index, cancellationToken);
			await CommitBatch(transaction, cancellationToken);
			return revision;
		}
		catch
		{
			await RollbackBatch(transaction);
			throw;
		}
	}

	private async Task<long> ProcessSubmission(SyncContainer container, int index, CancellationToken cancellationToken)
	{
		SystemConfig config = await GetConfiguration(container, cancellationToken);
		LocalRegistration local = await GetRegistration(container, cancellationToken);
		if (container.Batches.Count != 0) { await ApplyBatch(container.Batches[index], config, local, cancellationToken); }
		return config.CurrentRevision;
	}

	private async Task ApplyBatch(SyncBatch batch, SystemConfig config, LocalRegistration local, CancellationToken cancellationToken)
	{
		string hash = GetBatchHash(batch);
		LocalSyncBatch? receipt = await _unitOfWork.LocalSyncBatchRepo.GetBySyncKey(local.Id, batch.SyncKey, cancellationToken);
		if (receipt is not null) { ValidateReceipt(receipt, hash); }
		else { await ApplyNewBatch(batch, config, local, hash, cancellationToken); }
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
		if (config.CurrentRevision == long.MaxValue) { throw new InvalidSyncBatchException("The Master revision is exhausted."); }
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
		try { await _unitOfWork.RollbackTransaction(transaction, CancellationToken.None); }
		catch (Exception exception) { _sharedContext.LogService.Error("Master synchronization rollback failed.", exception); }
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
		if (local is null) { throw new InvalidSyncBatchException("The submitting Local is not registered on this Master."); }
		return local;
	}
}
