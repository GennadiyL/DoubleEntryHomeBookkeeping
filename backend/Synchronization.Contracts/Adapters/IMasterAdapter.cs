using Synchronization.Contracts.Adapters.Master;

namespace Synchronization.Contracts.Adapters;

/// <summary>
/// Defines the Local application's provider-independent Master boundary.
/// Synchronization services use this contract to reach the selected provider.
/// Its DTOs are independent of every provider service contract.
/// In-process implementations map these DTOs and call the provider service.
/// Remote implementations retrieve scoped tokens through ITokenStoreAdapter for each request.
/// Provider authorization and ownership checks apply in both environments.
/// Download returns a caller-owned stream outside JSON serialization.
/// Implementations do not own Local change capture or database installation.
/// </summary>
public interface IMasterAdapter
{
	/// <summary>
	/// Verifies existing owner credentials, validates the returned identities and securely stores the issued token before reporting success.
	/// Returns registration and expiry metadata without exposing the token to the caller.
	/// </summary>
	public Task<AuthenticateInfo> Authenticate(Authenticate command, CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks synchronization authorization before capture; false means credentials are required, while transport failures propagate as errors.
	/// Retrieves authorization for the configured Master address and supplied identity; missing or unreadable tokens require credentials.
	/// Every protected request must still validate authorization independently of this check.
	/// </summary>
	public Task<bool> CheckAuthorization(MasterIdentity identity, CancellationToken cancellationToken = default);

	/// <summary>
	/// Merges pending batches in document order, skipping already merged batch identities, and returns a snapshot for Local download.
	/// Repeating the same durable attempt resolves its outcome without merging its changes again; cancellation does not undo publication.
	/// </summary>
	public Task<SynchronizeInfo> Synchronize(MasterIdentity identity, Synchronize command, CancellationToken cancellationToken = default);

	/// <summary>
	/// Opens the selected immutable business snapshot for download; the caller owns and disposes the returned stream.
	/// </summary>
	public Task<DownloadSnapshotInfo> Download(MasterIdentity identity, DownloadSnapshot command, CancellationToken cancellationToken = default);

	/// <summary>
	/// Records the Local copy's actual installed version for completion, expiry renewal and deletion acknowledgement; retries are idempotent.
	/// </summary>
	public Task<OperationStatusInfo> Acknowledge(MasterIdentity identity, AcknowledgeSnapshot command, CancellationToken cancellationToken = default);

	/// <summary>
	/// Registers a replacement for the authorized expired Local copy and selects a snapshot after the explicit user Download action.
	/// Retrying for the same expired registration resumes its replacement instead of creating another Local registration.
	/// </summary>
	public Task<ReplaceExpiredCopyInfo> ReplaceExpired(MasterIdentity identity, ReplaceExpiredCopy command, CancellationToken cancellationToken = default);
}
