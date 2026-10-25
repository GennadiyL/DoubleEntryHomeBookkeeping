using System.Collections.Concurrent;
using Shared.Contracts.Storage;

namespace Shared.Impl.Storage;

/// <summary>
/// Provides exclusive object access within one application service provider.
/// A singleton instance shares one semaphore per object name across all request scopes.
/// Acquisition checks availability immediately without waiting for another owner.
/// Different object names can be acquired independently.
/// Object names use ordinal comparison and must identify the same resource consistently.
/// Semaphores remain cached for the lifetime of the application provider.
/// Returned handles release ownership when the business workflow finishes.
/// This implementation does not coordinate independent processes or providers.
/// </summary>
internal sealed class LocalStorageLockFactory : IStorageLockFactory
{
	private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

	public async Task<IStorageLock?> TryAcquire(string objectName, CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(objectName);
		cancellationToken.ThrowIfCancellationRequested();
		SemaphoreSlim semaphore = _locks.GetOrAdd(objectName, _ => new SemaphoreSlim(1, 1));
		return await semaphore.WaitAsync(0, cancellationToken) ? new LocalStorageLock(semaphore) : null;
	}
}
