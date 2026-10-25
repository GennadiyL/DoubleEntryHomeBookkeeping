using Master.Contracts.Adapters.MasterStorage;
using Shared.Contracts.Storage;

namespace Master.Impl.Adapters;

/// <summary>
/// Represents direct persistence to an existing local Master database.
/// StoragePath remains the authoritative file for the lifetime of the session.
/// SQLite transaction commits already persist changes to this file.
/// Publish therefore performs lifetime and cancellation checks without copying files.
/// The supplied snapshot remains owned by the caller and is never opened or deleted.
/// Disposal marks the session unusable without removing the persistent database.
/// Database connections must be closed separately by the persistence caller.
/// The acquired Master access handle remains owned by the business workflow.
/// </summary>
internal sealed class LocalMasterStorageSession(string databasePath, IStorageLock access) : IMasterStorageSession
{
	private int _disposed;

	public string StoragePath { get; } = databasePath;

	public Task Publish(string snapshotPath, CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
		ArgumentException.ThrowIfNullOrWhiteSpace(snapshotPath);
		cancellationToken.ThrowIfCancellationRequested();
		access.CancellationToken.ThrowIfCancellationRequested();
		return Task.CompletedTask;
	}

	public ValueTask DisposeAsync()
	{
		Interlocked.Exchange(ref _disposed, 1);
		return ValueTask.CompletedTask;
	}
}
