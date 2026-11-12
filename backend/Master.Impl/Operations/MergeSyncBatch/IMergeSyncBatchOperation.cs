using Exchange.Models.Synchronizations;
using Master.Impl.Operations.ApplySyncBatch;

namespace Master.Impl.Operations.MergeSyncBatch;

/// <summary>
/// Reconciles one captured batch with supplied detached Master rows.
/// Receives configuration of the merge through explicit invocation arguments.
/// Resolves entity content and ordering using revisions and conflict priority.
/// Matches currency rates by their stable currency/date natural key.
/// Mutates supplied rows without saving them or retaining them between calls.
/// Restoration, validation and final revision stamping follow this operation.
/// The caller owns the surrounding transaction and persistence.
/// Implementations are selected through Master dependency injection.
/// </summary>
internal interface IMergeSyncBatchOperation
{
	/// <summary>
	/// Merges the batch into the supplied graph before dependency restoration and validation.
	/// </summary>
	public void Run(SyncBatch batch, Guid localRegistrationId, long revision, Dictionary<string, SyncEntityState> sets,
		Dictionary<long, Guid> sources, CancellationToken cancellationToken = default);
}
