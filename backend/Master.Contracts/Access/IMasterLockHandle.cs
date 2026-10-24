namespace Master.Contracts.Access;

/// <summary>
/// Represents ownership of exclusive access to one Master dataset.
/// MasterService owns the handle returned by a successful acquisition.
/// Asynchronous disposal releases access and stops any ownership renewal.
/// CancellationToken is cancelled when the implementation can no longer guarantee ownership.
/// The service links this token with request cancellation to stop protected work.
/// Request cancellation does not replace disposal of the acquired handle.
/// Provider-specific ownership identifiers remain outside this business contract.
/// The handle does not execute synchronization or manage database contents.
/// </summary>
public interface IMasterLockHandle : IAsyncDisposable
{
	public CancellationToken CancellationToken { get; }
}
