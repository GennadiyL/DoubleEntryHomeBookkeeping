namespace Master.Contracts.Services.Synchronizations;

/// <summary>
/// Starts replacement of an expired local copy on explicit user action.
/// Used by the expired-copy Download action.
/// Identifies the old local registration being replaced.
/// Infrastructure authorizes the caller for the target Master and Local registration.
/// The result identifies the new registration and snapshot transfer.
/// Local configuration must survive replacement.
/// Expired unsynchronized business changes are not merged.
/// Registration and installation mechanics remain implementation concerns.
/// </summary>
public record ReplaceExpiredCopyRequest
{
	public required string LocalDatasetKey { get; set; }
}
