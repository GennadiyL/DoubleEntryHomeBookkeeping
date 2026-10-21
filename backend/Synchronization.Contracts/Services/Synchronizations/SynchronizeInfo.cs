namespace Synchronization.Contracts.Services.Synchronizations;

/// <summary>
/// Returns the application-facing outcome of the complete synchronization workflow.
/// Status identifies completion or the state that prevents completion.
/// AuthorizationRequired asks the UI to obtain credentials before resuming.
/// OutcomeUnknown retains pending work for later durable outcome resolution.
/// Expired requires the explicit replacement interaction rather than silent data loss.
/// Download descriptors and database streams remain inside the workflow.
/// Complete is reported only after the applicable installation and acknowledgement.
/// The result contains no authorization secrets or persistent business entities.
/// </summary>
public record SynchronizeInfo
{
	public required OperationStatusInfo Status { get; set; }
}