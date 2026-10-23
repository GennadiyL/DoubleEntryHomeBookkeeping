namespace Master.Contracts.Access;

/// <summary>
/// Defines exclusive access acquisition for an individual Master dataset.
/// MasterService decides which business workflow requires the lock.
/// The service holds the acquired handle until the protected workflow finishes.
/// Acquisition returns null when another owner already holds the lock, without waiting for release.
/// Cancellation and infrastructure failures propagate instead of reporting contention.
/// Implementations provide the locking mechanism appropriate to their hosting environment.
/// Different dataset keys can be acquired independently.
/// This contract does not download, merge or publish database contents.
/// </summary>
public interface IMasterLock
{
	public Task<IMasterLockHandle?> TryAcquire(string masterDatasetKey, CancellationToken cancellationToken = default);
}
