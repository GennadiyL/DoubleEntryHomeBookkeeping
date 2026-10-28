namespace Synchronization.Impl.Services;

/// <summary>
/// Serializes synchronization attempts within the single-instance Local application.
/// Shared across dependency scopes so concurrent calls cannot recapture pending work.
/// Waiting callers may cancel without interrupting the active attempt.
/// The workflow releases ownership after every result or exception.
/// Database transactions remain responsible for crash-safe persistence.
/// This gate does not authorize business editing during synchronization.
/// The host continues to block editing while the workflow is active.
/// Dependency injection owns the gate and disposes its semaphore at shutdown.
/// </summary>
internal sealed class SynchronizationGate : IDisposable
{
	public SemaphoreSlim Semaphore { get; } = new(1, 1);
	public void Dispose() => Semaphore.Dispose();
}
