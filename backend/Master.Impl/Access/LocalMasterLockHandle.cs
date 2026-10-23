using Master.Contracts.Access;

namespace Master.Impl.Access;

/// <summary>
/// Holds one successful acquisition of a local Master semaphore.
/// MasterService owns the handle for the duration of its protected workflow.
/// Disposal releases the semaphore exactly once, including concurrent disposal.
/// The semaphore remains owned by the shared lock registry and is not disposed here.
/// Request cancellation does not release ownership before workflow cleanup.
/// Local ownership has no lease duration and cannot expire while held.
/// LockLost therefore remains an uncancelled token.
/// This handle performs no database or synchronization operations.
/// </summary>
internal sealed class LocalMasterLockHandle(SemaphoreSlim semaphore) : IMasterLockHandle
{
	private SemaphoreSlim? _semaphore = semaphore;

	public CancellationToken LockLost => CancellationToken.None;

	public ValueTask DisposeAsync()
	{
		Interlocked.Exchange(ref _semaphore, null)?.Release();
		return ValueTask.CompletedTask;
	}
}
