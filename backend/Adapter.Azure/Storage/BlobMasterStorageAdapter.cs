using Master.Contracts.Adapters;
using Master.Contracts.Adapters.MasterStorage;
using Shared.Contracts;
using Shared.Contracts.Storage;

namespace Adapter.Azure.Storage;

/// <summary>
/// Opens and publishes Master database working copies through shared object storage.
/// Retains dependencies only; all per-open state belongs to the supplied session.
/// Maps the dataset key to the object name used by MasterService locking.
/// Each session retains its selected processor and the caller-owned storage lock.
/// The processor validates provider ownership and applies request conditions.
/// Publication and disposal share the session gate to protect temporary files.
/// Download failures dispose the partial session without releasing caller-owned access.
/// Database transactions, snapshot creation and provider selection remain outside this adapter.
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

	public async Task Publish(IMasterStorageSession session, string snapshotPath, CancellationToken cancellationToken = default)
	{
		BlobMasterStorageSession state = GetSession(session);
		using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(
			cancellationToken, state.Access.CancellationToken);
		await state.Gate.WaitAsync(cancellation.Token);
		try
		{
			await PublishSnapshot(state, snapshotPath, cancellation.Token);
		}
		finally
		{
			state.Gate.Release();
		}
	}

	public ValueTask Close(IMasterStorageSession session)
	{
		ArgumentNullException.ThrowIfNull(session);
		return session.DisposeAsync();
	}

	private BlobMasterStorageSession CreateSession(string masterDatasetKey, IStorageLock access)
	{
		IStorageProcessor processor = _storageProcessorFactory.CreateProcessor();
		string objectName = MasterStorageNames.Database(masterDatasetKey);
		return new BlobMasterStorageSession(_sharedContext, processor, objectName, access,
			_sharedContext.DirectoryService.CreateTempSubdirectory("dehb-master-"));
	}

	private async Task<IMasterStorageSession> DownloadSession(BlobMasterStorageSession session, CancellationToken cancellationToken)
	{
		try
		{
			await Download(session, cancellationToken);
			return session;
		}
		catch
		{
			await Close(session);
			throw;
		}
	}

	private async Task Download(BlobMasterStorageSession session, CancellationToken cancellationToken)
	{
		using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(
			cancellationToken, session.Access.CancellationToken);
		cancellation.Token.ThrowIfCancellationRequested();
		await using FileStream destination = OpenDownload(_sharedContext.FileService, session.StoragePath);
		await session.Storage.Read(session.ObjectName, destination, session.Access, cancellation.Token);
		cancellation.Token.ThrowIfCancellationRequested();
	}

	private async Task PublishSnapshot(BlobMasterStorageSession session, string snapshotPath, CancellationToken cancellationToken)
	{
		try
		{
			EnsureUsable(session);
			await UploadSnapshot(session, snapshotPath, cancellationToken);
		}
		catch
		{
			session.IsFaulted = true;
			throw;
		}
	}

	private async Task UploadSnapshot(BlobMasterStorageSession session, string snapshotPath, CancellationToken cancellationToken)
	{
		ValidateSnapshotPath(session.StoragePath, snapshotPath);
		cancellationToken.ThrowIfCancellationRequested();
		await using FileStream snapshot = OpenSnapshot(_sharedContext.FileService, snapshotPath);
		await session.Storage.Write(session.ObjectName, snapshot, true, session.Access, cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
	}

	private static BlobMasterStorageSession GetSession(IMasterStorageSession session)
	{
		ArgumentNullException.ThrowIfNull(session);
		return session as BlobMasterStorageSession ??
			throw new ArgumentException("The session is not a Master storage working copy.", nameof(session));
	}

	private static void EnsureUsable(BlobMasterStorageSession session)
	{
		ObjectDisposedException.ThrowIf(session.IsDisposed, session);
		if (session.IsFaulted)
		{
			throw new InvalidOperationException("The storage session failed. Reopen the Master database before retrying.");
		}
	}

	private static void ValidateSnapshotPath(string storagePath, string snapshotPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(snapshotPath);
		StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		if (string.Equals(Path.GetFullPath(snapshotPath), storagePath, comparison))
		{
			throw new ArgumentException("Publication requires a separate completed database snapshot.", nameof(snapshotPath));
		}
	}

	private static FileStream OpenDownload(IFileService fileService, string path) =>
		fileService.Open(path, new FileStreamOptions
		{
			Mode = FileMode.CreateNew,
			Access = FileAccess.Write,
			Share = FileShare.None,
			Options = FileOptions.Asynchronous | FileOptions.SequentialScan
		});

	private static FileStream OpenSnapshot(IFileService fileService, string path) =>
		fileService.Open(path, new FileStreamOptions
		{
			Mode = FileMode.Open,
			Access = FileAccess.Read,
			Share = FileShare.Read,
			BufferSize = 81920,
			Options = FileOptions.Asynchronous | FileOptions.SequentialScan
		});
}
