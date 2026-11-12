namespace Synchronization.Impl.Operations.AcceptCapture;

/// <summary>
/// Accepts the captured Local state while editing remains blocked.
/// No batch IDs or revision cutoff are required under that invariant.
/// Hard-deletes captured rows in the same transaction as durable batch persistence.
/// Captured deleted rows are physically removed; surviving new rows receive zero.
/// Existing positive revisions remain unchanged and modification flags are cleared.
/// The caller secures replay data and owns the transaction around all operations.
/// A fresh tracking scope and resolved live dependencies are required for bulk SQL.
/// Failures propagate to the caller for rollback; acceptance is not Master success.
/// </summary>
internal interface IAcceptCaptureOperation
{
	public Task Run(CancellationToken cancellationToken = default);
}
