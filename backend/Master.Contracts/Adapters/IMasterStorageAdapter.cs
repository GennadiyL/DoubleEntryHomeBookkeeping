using Master.Contracts.Adapters.MasterStorage;
using Master.Contracts.Access;

namespace Master.Contracts.Adapters;

/// <summary>
/// Prepares access to the persisted database for one Master dataset.
/// The caller acquires exclusive Master access before opening a storage session.
/// The returned session exposes a local database path for the persistence layer.
/// Local storage uses the existing database without copying it.
/// Remote storage prepares an owned working copy and publishes completed snapshots.
/// The caller closes database connections before disposing the storage session.
/// Access ownership outlives the session and remains the caller's responsibility.
/// Business validation, SQLite transactions and snapshot creation are outside this adapter.
/// </summary>
public interface IMasterStorageAdapter
{
	public Task<IMasterStorageSession> Open(
		string masterDatasetKey,
		IMasterLockHandle access,
		CancellationToken cancellationToken = default);
}
