using Shared.Contracts.Storage;

namespace Shared.Impl.Storage;

/// <summary>
/// Holds one successful acquisition of an application-local semaphore.
/// The caller owns this lock for the duration of its protected workflow.
/// Disposal releases the semaphore exactly once, including concurrent disposal.
/// The semaphore remains owned by the shared registry and is not disposed here.
/// Request cancellation does not release ownership before workflow cleanup.
/// Local ownership cannot expire while held, so its token remains uncancelled.
/// This lock performs no file, database or application-specific operations.
/// </summary>
internal sealed class LocalStorageLock : IStorageLock
{
	private SemaphoreSlim? _semaphore;

	public LocalStorageLock(SemaphoreSlim semaphore)
	{
		_semaphore = semaphore;
	}

	public CancellationToken CancellationToken => CancellationToken.None;

	public ValueTask DisposeAsync()
	{
		Interlocked.Exchange(ref _semaphore, null)?.Release();
		return ValueTask.CompletedTask;
	}
}
