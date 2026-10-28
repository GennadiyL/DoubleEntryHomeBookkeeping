namespace Synchronization.Contracts.Services.Synchronizations;

/// <summary>
/// Identifies the application-facing result of a synchronization attempt.
/// Undefined represents an unset result and never indicates success.
/// Complete means acknowledgement and pending-state cleanup have finished.
/// AuthorizationRequired asks the caller to obtain valid credentials.
/// Cancelled means caller-requested cancellation reached a safe boundary.
/// Deferred identifies an expected interruption that permits a later retry.
/// Stop reasons explain authorization requirements and deferrals.
/// Returned checkpoints describe this attempt independently of why execution stopped.
/// </summary>
public enum SynchronizationOutcome
{
	Undefined = 0,
	Complete = 1,
	AuthorizationRequired = 2,
	Cancelled = 3,
	Deferred = 4
}
