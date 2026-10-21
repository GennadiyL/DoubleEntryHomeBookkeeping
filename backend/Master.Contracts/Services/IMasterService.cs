using Master.Contracts.Services.Synchronizations;

namespace Master.Contracts.Services;

/// <summary>
/// Defines the server-side synchronization business boundary.
/// Local callers reach this service through a provider adapter.
/// Azure Functions expose the same operations through HTTP.
/// In-process execution preserves the same authorization and ownership checks.
/// Synchronization deduplicates durable attempts before merging changes.
/// Publication stores the immutable snapshot and its durable outcome atomically.
/// Download never implies successful installation on a Local copy.
/// Acknowledgement records the actual installed version after activation.
/// Initial dataset provisioning remains outside this synchronization contract.
/// </summary>
public interface IMasterService
{
	/// <summary>
	/// Verifies existing owner credentials and issues replacement authorization for the registered Local copy.
	/// </summary>
	public Task<AuthenticateInfo> Authenticate(Authenticate command, CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks synchronization authorization before capture; false means credentials are required, while transport failures propagate as errors.
	/// Every protected request must still validate authorization independently of this check.
	/// </summary>
	public Task<bool> CheckAuthorization(MasterAuthorization? authorization, CancellationToken cancellationToken = default);

	/// <summary>
	/// Merges pending batches in document order, skipping already merged batch identities, and returns a snapshot for Local download.
	/// Repeating the same durable attempt resolves its outcome without merging its changes again; cancellation does not undo publication.
	/// </summary>
	public Task<SynchronizeInfo> Synchronize(MasterAuthorization authorization, Synchronize command, CancellationToken cancellationToken = default);

	/// <summary>
	/// Opens the selected immutable business snapshot for download; the caller owns and disposes the returned stream.
	/// </summary>
	public Task<DownloadSnapshotInfo> Download(MasterAuthorization authorization, DownloadSnapshot command, CancellationToken cancellationToken = default);

	/// <summary>
	/// Records the Local copy's actual installed version for completion, expiry renewal and deletion acknowledgement; retries are idempotent.
	/// </summary>
	public Task<OperationStatusInfo> Acknowledge(MasterAuthorization authorization, AcknowledgeSnapshot command, CancellationToken cancellationToken = default);

	/// <summary>
	/// Registers a replacement for the authorized expired Local copy and selects a snapshot after the explicit user Download action.
	/// Retrying for the same expired registration resumes its replacement instead of creating another Local registration.
	/// </summary>
	public Task<ReplaceExpiredCopyInfo> ReplaceExpired(MasterAuthorization authorization, ReplaceExpiredCopy command, CancellationToken cancellationToken = default);
}
