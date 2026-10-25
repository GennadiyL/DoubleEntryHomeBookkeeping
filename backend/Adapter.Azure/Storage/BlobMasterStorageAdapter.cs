using Master.Contracts.Adapters;
using Master.Contracts.Adapters.MasterStorage;
using Shared.Contracts;
using Shared.Contracts.Storage;

namespace Adapter.Azure.Storage;

/// <summary>
/// Prepares a temporary Master database through the selected storage processor.
/// Maps the dataset key to the same object name used by MasterService locking.
/// Each opened session retains one processor and the caller-owned storage lock.
/// The processor validates provider-specific ownership and applies request conditions.
/// Each session owns a unique temporary directory on the worker filesystem.
/// Download failures clean up partial files without releasing caller-owned access.
/// Database creation, SQLite backups and transaction orchestration are excluded.
/// Provider configuration and implementation selection belong to the host.
/// </summary>
internal sealed class BlobMasterStorageAdapter : IMasterStorageAdapter
{
	private readonly ISharedContext _sharedContext;
	private readonly IStorageProcessorFactory _storageProcessorFactory;

	public BlobMasterStorageAdapter(ISharedContext sharedContext, IStorageProcessorFactory storageProcessorFactory)
	{
		_sharedContext = sharedContext;
		_storageProcessorFactory = storageProcessorFactory;
	}

	public async Task<IMasterStorageSession> Open(
		string masterDatasetKey,
		IStorageLock access,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(masterDatasetKey);
		ArgumentNullException.ThrowIfNull(access);
		cancellationToken.ThrowIfCancellationRequested();
		access.CancellationToken.ThrowIfCancellationRequested();
		BlobMasterStorageSession session = CreateSession(masterDatasetKey, access);
		return await DownloadSession(session, cancellationToken);
	}

	private BlobMasterStorageSession CreateSession(string masterDatasetKey, IStorageLock access)
	{
		IStorageProcessor processor = _storageProcessorFactory.CreateProcessor();
		string objectName = MasterStorageNames.Database(masterDatasetKey);
		return new BlobMasterStorageSession(_sharedContext, processor, objectName, access,
			_sharedContext.DirectoryService.CreateTempSubdirectory("dehb-master-"));
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
