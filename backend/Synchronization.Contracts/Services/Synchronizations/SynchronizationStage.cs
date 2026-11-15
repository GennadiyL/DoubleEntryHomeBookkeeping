namespace Synchronization.Contracts.Services.Synchronizations;

/// <summary>
/// Identifies the work currently performed by one synchronization attempt.
/// The service reports stages through its per-call progress receiver.
/// Values describe activity rather than a percentage or durable checkpoint.
/// Download and installation stages are omitted when Local is already current.
/// Presentation layers map these identifiers to their own display text.
/// Cancellation requests remain separate from stage notifications.
/// Complete and Cancelled belong to SynchronizationOutcome instead.
/// Stage reports do not replace awaiting the final result and cleanup.
/// </summary>
public enum SynchronizationStage
{
	CheckingAccess = 1,
	CapturingChanges = 2,
	SynchronizingWithMaster = 3,
	DownloadingSnapshot = 4,
	PreparingLocalData = 5,
	InstallingLocalData = 6,
	Acknowledging = 7
}
