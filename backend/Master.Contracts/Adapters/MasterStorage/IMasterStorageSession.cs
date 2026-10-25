namespace Master.Contracts.Adapters.MasterStorage;

/// <summary>
/// Holds the state and local database path for one opened Master storage session.
/// StoragePath is consumed by the DAL when constructing its SQLite connection.
/// The adapter owns opening and publication behavior; the session owns cleanup.
/// Implementations retain their storage target, borrowed lock and lifecycle state.
/// Remote sessions coordinate publication and disposal using a per-session gate.
/// A failed remote publication invalidates that session without affecting other sessions.
/// Disposal cleans up owned temporary files but never deletes the authoritative Local database.
/// Database connections must close first; the caller releases the storage lock afterward.
/// </summary>
public interface IMasterStorageSession : IAsyncDisposable
{
	public string StoragePath { get; }
}
