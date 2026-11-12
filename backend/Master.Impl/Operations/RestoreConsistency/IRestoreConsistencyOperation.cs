using Business.Models.Entities.Config;
using Master.Impl.Operations.ApplySyncBatch;

namespace Master.Impl.Operations.RestoreConsistency;

/// <summary>
/// Restores required dependencies in the supplied detached Master state.
/// Receives all invocation data explicitly and retains no batch state.
/// Updates referenced rows in place for subsequent validation and persistence.
/// Restores parent chains and retained Currency aggregate members.
/// Missing identities reject the operation rather than inventing content.
/// The caller owns the batch transaction and revision allocation.
/// No repositories or snapshot files are accessed by this operation.
/// Implementations are composed through the Master module registration.
/// </summary>
internal interface IRestoreConsistencyOperation
{
	/// <summary>
	/// Restores consistency in the supplied graph before Master validates and persists the batch.
	/// </summary>
	public void Run(Dictionary<string, SyncEntityState> sets, SystemConfig config, CancellationToken cancellationToken = default);
}
