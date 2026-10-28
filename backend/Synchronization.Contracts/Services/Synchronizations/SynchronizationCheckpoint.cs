namespace Synchronization.Contracts.Services.Synchronizations;

/// <summary>
/// Describes the synchronization boundary reached by the current attempt.
/// None means this attempt has not advanced or has completed.
/// Captured means replay data is secured and Local capture is accepted.
/// Captured also covers attempted submission whose remote completion is unconfirmed.
/// Submitted means all batches were accepted and snapshot installation is pending.
/// Installed means the actual installed revision awaits acknowledgement.
/// Acknowledged means Master confirmed receipt and Local cleanup remains pending.
/// Checkpoints are returned to the caller and are not stored in LocalJson.
/// Every retry captures changes and resubmits the saved container.
/// </summary>
public enum SynchronizationCheckpoint
{
	None = 0,
	Captured = 1,
	Submitted = 2,
	Installed = 3,
	Acknowledged = 4
}
