namespace Shared.Contracts.Storage;

/// <summary>
/// Acquires exclusive access to objects identified by their storage names.
/// The caller chooses which operations require protection and owns the returned lock.
/// Acquisition returns null when another owner holds the object, without waiting.
/// Cancellation and infrastructure errors propagate instead of reporting contention.
/// Different object names can be acquired independently.
/// Azure names identify existing blobs in the configured container.
/// Local names identify application-wide resources within one service provider.
/// </summary>
public interface IStorageLockFactory
{
	/// <summary>
	/// Tries to acquire an object immediately for a caller-managed protected workflow.
	/// A successful lock must be disposed after protected work and cleanup finish.
	/// </summary>
	public Task<IStorageLock?> TryAcquire(string objectName, CancellationToken cancellationToken = default);
}
