using Master.Contracts.Adapters;
using Master.Contracts.Adapters.MasterStorage;
using Shared.Contracts;
using Shared.Contracts.Storage;

namespace Master.Impl.Adapters;

/// <summary>
/// Opens storage sessions over existing local Master database files.
/// Retains dependencies only; each session holds its own path and access state.
/// The host maps each dataset identity to its authoritative filesystem path.
/// Relative paths resolve against the process working directory when opened.
/// Missing files are rejected instead of creating an empty database.
/// Publication checks lifetime and cancellation without copying a snapshot.
/// SQLite transaction commits already persist changes to the authoritative file.
/// Closing delegates to session disposal and never releases the caller's lock.
/// </summary>
internal sealed class LocalMasterStorageAdapter : IMasterStorageAdapter
{
	private readonly ISharedContext _sharedContext;
	private readonly Func<string, string> _getDatabasePath;

	public LocalMasterStorageAdapter(ISharedContext sharedContext, Func<string, string> getDatabasePath)
	{
		_sharedContext = sharedContext;
		_getDatabasePath = getDatabasePath;
	}

	public Task<IMasterStorageSession> Open(string masterDatasetKey, IStorageLock access,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(masterDatasetKey);
		ArgumentNullException.ThrowIfNull(access);
		cancellationToken.ThrowIfCancellationRequested();
		access.CancellationToken.ThrowIfCancellationRequested();
		string path = GetDatabasePath(masterDatasetKey);
		return Task.FromResult<IMasterStorageSession>(new LocalMasterStorageSession(path, access));
	}

	public Task Publish(IMasterStorageSession session, string snapshotPath, CancellationToken cancellationToken = default)
	{
		LocalMasterStorageSession state = GetSession(session);
		ObjectDisposedException.ThrowIf(state.IsDisposed, state);
		ArgumentException.ThrowIfNullOrWhiteSpace(snapshotPath);
		cancellationToken.ThrowIfCancellationRequested();
		state.Access.CancellationToken.ThrowIfCancellationRequested();
		return Task.CompletedTask;
	}

	public ValueTask Close(IMasterStorageSession session)
	{
		ArgumentNullException.ThrowIfNull(session);
		return session.DisposeAsync();
	}

	private string GetDatabasePath(string masterDatasetKey)
	{
		string path = Path.GetFullPath(_getDatabasePath(masterDatasetKey));
		if (!_sharedContext.FileService.Exists(path))
		{
			throw new FileNotFoundException("The local Master database does not exist.", path);
		}
		return path;
	}

	private static LocalMasterStorageSession GetSession(IMasterStorageSession session)
	{
		ArgumentNullException.ThrowIfNull(session);
		return session as LocalMasterStorageSession ??
			throw new ArgumentException("The session is not direct Local Master storage.", nameof(session));
	}
}
