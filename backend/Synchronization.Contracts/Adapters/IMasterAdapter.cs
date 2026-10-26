using Synchronization.Contracts.Adapters.Master;

namespace Synchronization.Contracts.Adapters;

/// <summary>
/// Defines the provider-independent Master synchronization boundary.
/// The host authenticates callers and authorizes requested dataset identities.
/// In-process adapters map independent DTOs to the Master service contracts.
/// The business service validates registration state, schema and synchronization rules.
/// Synchronization deduplicates captured batches using their immutable identities.
/// Download returns a consistent snapshot independently of a preceding synchronization.
/// Snapshot metadata identifies the actual revision represented by its bytes.
/// Acknowledgement records successful installation rather than completed downloading.
/// Initial dataset provisioning remains outside this synchronization contract.
/// </summary>
public interface IMasterAdapter
{
	/// <summary>
	/// Processes every submitted batch in order, skipping batches already applied, and returns the Master revision observed after processing.
	/// Holds exclusive access for the complete call; an occupied Master is rejected before database access rather than queued.
	/// Caller cancellation or loss of ownership stops processing and releases access after transaction cleanup.
	/// Successful completion confirms all submitted batches are applied; an error may follow committed earlier batches, which retries must deduplicate.
	/// </summary>
	public Task<SynchronizeResponse> Synchronize(SynchronizeRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	/// Opens the latest consistent Master snapshot selected when this call creates the snapshot; its revision must meet the requested minimum.
	/// No preceding Synchronize call or transfer descriptor is required. The caller owns and disposes the returned stream.
	/// Client must preserve unsynchronized work in its database and pending JSON before installing a replacement.
	/// </summary>
	public Task<DownloadResponse> Download(DownloadRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	/// Records the revision successfully installed by Local and the Master acknowledgement time, and returns whether acknowledgement succeeded.
	/// Repeated acknowledgements are idempotent; older receipts must not decrease SyncRevision.
	/// </summary>
	public Task<AcknowledgeResponse> Acknowledge(AcknowledgeRequest request, CancellationToken cancellationToken = default);
}
