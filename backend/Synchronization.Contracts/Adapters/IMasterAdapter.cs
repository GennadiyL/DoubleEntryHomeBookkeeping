using Synchronization.Contracts.Adapters.Master;

namespace Synchronization.Contracts.Adapters;

/// <summary>
/// Defines the Local application's provider-independent Master boundary.
/// Synchronization services use this contract to reach the selected provider.
/// Its DTOs are independent of every provider service contract.
/// In-process implementations map these DTOs and call the provider service.
/// Remote implementations map these DTOs to the provider transport contract.
/// Provider authorization and ownership checks apply in both environments.
/// Download returns a caller-owned stream outside JSON serialization.
/// Implementations do not own Local change capture or database installation.
/// </summary>
public interface IMasterAdapter
{
	/// <summary>
	/// Verifies existing owner credentials and issues replacement authorization for the registered Local copy.
	/// </summary>
	public Task<AuthenticateInfo> Authenticate(Authenticate command, CancellationToken cancellationToken = default);

	/// <summary>
	/// Validates and merges submitted changes, publishes the resulting snapshot and returns the outcome for Local download.
	/// </summary>
	public Task<SynchronizeInfo> Synchronize(MasterAuthorization authorization, Synchronize command, CancellationToken cancellationToken = default);

	/// <summary>
	/// Resolves the durable attempt outcome for retry and recovery without applying the submitted changes again.
	/// </summary>
	public Task<OperationStatusInfo> GetOutcome(MasterAuthorization authorization, SyncIdentity identity, CancellationToken cancellationToken = default);

	/// <summary>
	/// Requests cancellation where possible and returns the durable outcome; already published changes remain published.
	/// </summary>
	public Task<OperationStatusInfo> Cancel(MasterAuthorization authorization, SyncIdentity identity, CancellationToken cancellationToken = default);

	/// <summary>
	/// Opens the selected immutable business snapshot for download; the caller owns and disposes the returned stream.
	/// </summary>
	public Task<DownloadSnapshotInfo> Download(MasterAuthorization authorization, DownloadSnapshot command, CancellationToken cancellationToken = default);

	/// <summary>
	/// Records the Local copy's actual installed version for completion, expiry renewal and deletion acknowledgement; retries are idempotent.
	/// </summary>
	public Task<OperationStatusInfo> Acknowledge(MasterAuthorization authorization, AcknowledgeSnapshot command, CancellationToken cancellationToken = default);

	/// <summary>
	/// Resolves the previous attempt and selects the latest published snapshot for Local recovery installation.
	/// </summary>
	public Task<TransferInfo> Recover(MasterAuthorization authorization, SyncIdentity identity, CancellationToken cancellationToken = default);

	/// <summary>
	/// Registers a replacement for the authorized expired Local copy and selects the snapshot for its explicit download action.
	/// </summary>
	public Task<ReplaceExpiredCopyInfo> ReplaceExpired(MasterAuthorization authorization, ReplaceExpiredCopy command, CancellationToken cancellationToken = default);
}
