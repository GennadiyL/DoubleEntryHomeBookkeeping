using Shared.Contracts.Storage;

namespace Master.Impl.Operations.MasterStorage;

/// <summary>
/// Holds state for direct persistence to an existing local Master database.
/// StoragePath remains the authoritative file for the session lifetime.
/// The operation performs publication checks; the session has no publication behavior.
/// The supplied access lock remains owned by the business workflow.
/// Disposal marks the session unusable without removing the persistent database.
/// Repeated disposal is safe and preserves caller-owned snapshot files.
/// Database connections must be closed separately by the persistence caller.
/// No working copy, storage processor or temporary directory is owned here.
/// </summary>
internal sealed class LocalMasterStorageSession : IMasterStorageSession
{
	private int _disposed;

	internal LocalMasterStorageSession(string databasePath, IStorageLock access)
	{
		StoragePath = databasePath;
		Access = access;
	}

	public string StoragePath { get; }
	internal IStorageLock Access { get; }
	internal bool IsDisposed => Volatile.Read(ref _disposed) != 0;

	public ValueTask DisposeAsync()
	{
		Interlocked.Exchange(ref _disposed, 1);
		return ValueTask.CompletedTask;
	}
}
