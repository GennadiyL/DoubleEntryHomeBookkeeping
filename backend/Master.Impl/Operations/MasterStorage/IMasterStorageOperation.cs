using Shared.Contracts.Storage;

namespace Master.Impl.Operations.MasterStorage;

/// <summary>
/// Prepares and publishes the persisted database for one Master dataset.
/// The caller acquires exclusive Master access before opening a storage session.
/// Operations receive session state explicitly; the operation retains only dependencies.
/// Local storage uses the existing database without copying it.
/// Remote storage prepares an owned working copy and publishes completed snapshots.
/// The caller closes database connections before closing or disposing the session.
/// Access ownership outlives the session and remains the caller's responsibility.
/// Business validation, SQLite transactions and snapshot creation are outside this operation.
/// </summary>
internal interface IMasterStorageOperation
{
	/// <summary>
	/// Opens an existing database and returns session state with its usable local path for the DAL.
	/// </summary>
	public Task<IMasterStorageSession> Open(
		string masterDatasetKey,
		IStorageLock access,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Publishes a completed standalone snapshot using the opened session's storage and access ownership.
	/// Local publication is a no-op; a failed remote publication prevents reuse of that session.
	/// </summary>
	public Task Publish(
		IMasterStorageSession session,
		string snapshotPath,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Disposes the session after database connections close, releasing owned temporary files.
	/// Equivalent to session disposal; does not release the caller's storage lock.
	/// </summary>
	public ValueTask Close(IMasterStorageSession session);
}
