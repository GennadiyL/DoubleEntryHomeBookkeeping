using Exchange.Models.Synchronizations;

namespace Master.Impl.Operations.ApplySyncBatch;

/// <summary>
/// Applies one previously unprocessed batch to the Master unit of work.
/// The caller supplies the registered Local identity and allocated positive revision.
/// Content conflicts use entity revisions and accepted batch provenance.
/// Ordering is reconciled independently of content priority.
/// The caller serializes Master writes and owns the surrounding transaction.
/// The operation saves changes but never commits or rolls back that transaction.
/// Deduplication, revision allocation and receipt persistence belong to the caller.
/// A failure requires rollback before the unit of work can be used again.
/// </summary>
internal interface IApplySyncBatchOperation
{
	public Task Run(SyncBatch syncBatch, Guid localRegistrationId, long revision,
		CancellationToken cancellationToken = default);
}
