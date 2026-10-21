namespace Synchronization.Impl.Operations.AcceptCapture;

/// <summary>
/// Accepts the captured Local state while editing remains blocked.
/// No batch IDs or revision cutoff are required under that invariant.
/// Physically removes local soft deletions before accepting surviving rows.
/// New surviving rows receive the zero edit-revision sentinel.
/// Existing positive revisions remain unchanged and modification flags are cleared.
/// The caller secures replay data and owns the transaction around all operations.
/// A fresh tracking scope and resolved live dependencies are required for bulk SQL.
/// Failures propagate to the caller for rollback; acceptance is not Master success.
/// </summary>
internal interface IAcceptCaptureOperation
{
	public Task Run(CancellationToken cancellationToken = default);
}