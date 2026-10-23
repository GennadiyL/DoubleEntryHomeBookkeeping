using System.Collections.Concurrent;
using Master.Contracts.Access;

namespace Master.Impl.Access;

/// <summary>
/// Provides exclusive Master access within one application service provider.
/// A singleton instance shares one semaphore per dataset across all request scopes.
/// Acquisition checks availability immediately without waiting for another owner.
/// Different dataset keys can be acquired independently.
/// Dataset keys use ordinal comparison and must identify the same Master consistently.
/// Semaphores remain cached for the lifetime of the application provider.
/// Returned handles release ownership when the business workflow finishes.
/// This implementation does not coordinate independent processes or providers.
/// </summary>
internal sealed class LocalMasterLock : IMasterLock
{
	private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

	public async Task<IMasterLockHandle?> TryAcquire(string masterDatasetKey, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(masterDatasetKey);
		cancellationToken.ThrowIfCancellationRequested();
		SemaphoreSlim semaphore = _locks.GetOrAdd(masterDatasetKey, _ => new SemaphoreSlim(1, 1));
		return await semaphore.WaitAsync(0, cancellationToken) ? new LocalMasterLockHandle(semaphore) : null;
	}
}
