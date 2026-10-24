using Master.Contracts.Adapters;
using Master.Contracts.Adapters.MasterStorage;
using Master.Contracts.Access;
using Shared.Contracts;

namespace Master.Impl.Adapters;

/// <summary>
/// Opens storage sessions over existing local Master database files.
/// The host maps each dataset identity to its authoritative filesystem path.
/// Relative paths are resolved against the process working directory when opened.
/// Each session references the existing file rather than creating a working copy.
/// Missing files are rejected instead of silently creating an empty database.
/// Caller cancellation and access loss are checked before opening the session.
/// The service validates dataset identity and coordinates SQLite operations through the DAL.
/// The supplied access handle is borrowed and is never disposed by this adapter.
/// </summary>
internal sealed class LocalMasterStorageAdapter(ISharedContext sharedContext, Func<string, string> getDatabasePath) : IMasterStorageAdapter
{
	public Task<IMasterStorageSession> Open(string masterDatasetKey, IMasterLockHandle access,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(masterDatasetKey);
		ArgumentNullException.ThrowIfNull(access);
		cancellationToken.ThrowIfCancellationRequested();
		access.CancellationToken.ThrowIfCancellationRequested();
		string path = Path.GetFullPath(getDatabasePath(masterDatasetKey));
		if (!sharedContext.FileService.Exists(path))
		{
			throw new FileNotFoundException("The local Master database does not exist.", path);
		}
		return Task.FromResult<IMasterStorageSession>(new LocalMasterStorageSession(path, access));
	}
}
