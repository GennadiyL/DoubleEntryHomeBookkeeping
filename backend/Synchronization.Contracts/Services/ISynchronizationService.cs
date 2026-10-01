using Synchronization.Contracts.Services.Synchronizations;

namespace Synchronization.Contracts.Services;

/// <summary>
/// Coordinates synchronization and recovery for the current local dataset.
/// These application-facing operations obtain pending changes from local context.
/// They do not define the unresolved cloud change serialization protocol.
/// Each attempt retains durable identity across uncertain responses.
/// Owner and registration checks apply to every remote action.
/// Publication cannot be rolled back by cancellation.
/// Snapshot installation must precede acknowledgement.
/// Local settings remain outside synchronized business content.
/// </summary>
public interface ISynchronizationService
{
	/// <summary>
	/// Captures pending local changes and starts synchronization for the progress UI using stored priority and authorization.
	/// </summary>
	public Task<SynchronizeInfo> Synchronize(Synchronize command);

	/// <summary>
	/// Resolves a durable attempt outcome for recovery; timestamps alone cannot establish success.
	/// </summary>
	public Task<OperationStatusInfo> GetOutcome(SyncIdentity identity);

	/// <summary>
	/// Cancels work where possible for the progress UI; published changes remain published.
	/// </summary>
	public Task<OperationStatusInfo> Cancel(SyncIdentity identity);

	/// <summary>
	/// Downloads the selected immutable business snapshot for local installation; the caller disposes its stream.
	/// </summary>
	public Task<DownloadSnapshotInfo> Download(DownloadSnapshot command);

	/// <summary>
	/// Records actual successful installation for completion and deletion acknowledgement; retries must be idempotent.
	/// </summary>
	public Task<OperationStatusInfo> Acknowledge(AcknowledgeSnapshot command);

	/// <summary>
	/// Resolves an earlier attempt and selects the latest snapshot for recovery installation.
	/// </summary>
	public Task<TransferInfo> Recover(SyncIdentity identity);

	/// <summary>
	/// Registers a replacement copy and selects its snapshot after the user chooses Download on expiry.
	/// </summary>
	public Task<ReplaceExpiredCopyInfo> ReplaceExpired(ReplaceExpiredCopy command);
}
