namespace Synchronization.Contracts.Services.Synchronizations;

/// <summary>
/// Returns the application-facing outcome of the synchronization workflow.
/// Outcome distinguishes completion, authorization, cancellation and deferral.
/// Reason explains authorization requirements or deferred execution.
/// Complete and Cancelled outcomes have no stop reason.
/// Checkpoint describes the boundary reached by this attempt; it is not persisted.
/// Revision is the Master snapshot revision currently installed on Local.
/// Revision is null when no saved or captured container has been read.
/// The same revision is stored in SyncContainer and LocalConfig for acknowledgement.
/// This result neither persists recovery state nor exposes authorization secrets.
/// </summary>
public record SynchronizeInfo
{
	public required SynchronizationOutcome Outcome { get; set; }
	public SynchronizationStopReason? Reason { get; set; }
	public required SynchronizationCheckpoint Checkpoint { get; set; }
	public long? Revision { get; set; }
}
