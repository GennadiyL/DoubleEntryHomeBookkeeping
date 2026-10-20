using Exchange.Models.Synchronizations;

namespace Synchronization.Impl.Operations.CaptureChanges;

/// <summary>
/// Defines capture of uncaptured Local changes into a synchronization batch.
/// Run reads the current dataset's tracking metadata rather than a time or revision filter.
/// New creations and flagged updates or deletions determine batch membership.
/// The result carries the captured identity, time, priority and object values.
/// Capturing values does not itself acknowledge acceptance by Master.
/// The caller coordinates durable batch persistence with local tracking acceptance.
/// Network transfer and snapshot installation remain outside this operation.
/// The interface is internal to the Synchronization implementation project.
/// </summary>
internal interface ICaptureChangesOperation
{
	public Task<SyncBatch> Run(CancellationToken cancellationToken = default);
}
