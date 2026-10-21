namespace Synchronization.Contracts.Services.Synchronizations;

/// <summary>
/// Returns the application-facing outcome of explicit expired-copy replacement.
/// Status identifies completion or the state that prevents completion.
/// AuthorizationRequired asks the UI to obtain credentials before resuming.
/// Replacement registration and transfer descriptors remain inside the workflow.
/// Complete requires installation and acknowledgement of the replacement snapshot.
/// Retrying resumes durable replacement work instead of registering another copy.
/// Local settings survive replacement; expired unsynchronized business data does not.
/// The result contains no authorization secrets or persistent business entities.
/// </summary>
public record ReplaceExpiredCopyInfo
{
	public required OperationStatusInfo Status { get; set; }
}
