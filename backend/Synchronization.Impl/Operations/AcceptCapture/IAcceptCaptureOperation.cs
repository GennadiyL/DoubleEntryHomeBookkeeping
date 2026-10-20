using Exchange.Models.Synchronizations;

namespace Synchronization.Impl.Operations.AcceptCapture;

/// <summary>
/// Defines local tracking acceptance for the exact captured synchronization batch.
/// Run uses the batch rather than a timestamp or Master revision cutoff.
/// Captured creations transition from a null local revision to the zero sentinel.
/// Captured modification flags are cleared without assigning positive revisions.
/// Physical deletion must preserve durable replay evidence and reference integrity.
/// The caller coordinates acceptance with durable persistence and transaction boundaries.
/// Acceptance does not mean Master publication or successful synchronization.
/// The interface is internal to the Synchronization implementation project.
/// </summary>
internal interface IAcceptCaptureOperation
{
	public Task Run(SyncBatch batch, CancellationToken cancellationToken = default);
}
