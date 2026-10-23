using Adapter.Azure.Access;
using Azure.Storage.Blobs;
using Master.Contracts.Adapters;
using Master.Contracts.Adapters.MasterStorage;
using Master.Contracts.Access;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace Adapter.Azure.Storage;

/// <summary>
/// Prepares a temporary Master database from an existing Azure blob.
/// The host supplies the same authenticated blob mapping used by the Azure lock.
/// Only a live Azure access handle for the requested dataset and blob is accepted.
/// Each opened session owns a unique temporary directory on the worker filesystem.
/// Download failures clean up partial files without releasing caller-owned access.
/// The session publishes standalone snapshots using the existing lease identity.
/// Provider details remain inside the Azure assembly and are not exposed to business code.
/// Database creation, SQLite backups and transaction orchestration are excluded.
/// </summary>
internal sealed class BlobMasterStorageAdapter(
	ISharedContext sharedContext, Func<string, BlobClient> getMasterBlob, ILogger<BlobMasterStorageAdapter> logger) : IMasterStorageAdapter
{
	public async Task<IMasterStorageSession> Open(string masterDatasetKey, IMasterLockHandle access,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(masterDatasetKey);
		ArgumentNullException.ThrowIfNull(access);
		cancellationToken.ThrowIfCancellationRequested();
		access.LockLost.ThrowIfCancellationRequested();
		BlobClient blob = getMasterBlob(masterDatasetKey);
		AzureMasterLockHandle ownership = ValidateAccess(masterDatasetKey, blob, access);
		BlobMasterStorageSession session = new(sharedContext, blob, ownership,
			sharedContext.DirectoryService.CreateTempSubdirectory("dehb-master-"), logger);
		return await DownloadSession(session, cancellationToken);
	}

	private static AzureMasterLockHandle ValidateAccess(string masterDatasetKey, BlobClient blob, IMasterLockHandle access)
	{
		if (access is not AzureMasterLockHandle ownership || ownership.MasterDatasetKey != masterDatasetKey ||
			ownership.BlobUri != blob.Uri || string.IsNullOrWhiteSpace(ownership.LeaseId))
		{
			throw new ArgumentException("The access handle does not own the requested Master blob.", nameof(access));
		}
		return ownership;
	}

	private static async Task<IMasterStorageSession> DownloadSession(BlobMasterStorageSession session, CancellationToken cancellationToken)
	{
		try
		{
			await session.Download(cancellationToken);
			return session;
		}
		catch
		{
			await session.DisposeAsync();
			throw;
		}
	}
}
