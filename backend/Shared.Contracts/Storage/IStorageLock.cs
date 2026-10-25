namespace Shared.Contracts.Storage;

/// <summary>
/// Represents exclusive ownership of one storage object.
/// The caller retains the lock for the complete protected workflow.
/// Asynchronous disposal releases ownership and stops any renewal.
/// CancellationToken is cancelled if continued ownership cannot be guaranteed.
/// Request cancellation does not replace disposal of an acquired lock.
/// Provider-specific lease details remain outside this shared contract.
/// The lock does not read, write or interpret the protected object's contents.
/// </summary>
public interface IStorageLock : IAsyncDisposable
{
	public CancellationToken CancellationToken { get; }
}
