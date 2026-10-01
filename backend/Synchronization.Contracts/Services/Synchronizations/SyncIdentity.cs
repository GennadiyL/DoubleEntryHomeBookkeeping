namespace Synchronization.Contracts.Services.Synchronizations;

/// <summary>
/// Identifies one durable synchronization attempt for a local copy.
/// SyncKey is reused when resolving an uncertain outcome.
/// LocalDatasetKey identifies the registered local dataset.
/// Used by outcome, cancellation and recovery operations.
/// Both values are non-entity keys rather than business entity IDs.
/// Owner authorization is obtained from the application session.
/// Supplying these keys alone never grants access.
/// The record carries no changes or persistent model instances.
/// </summary>
public record SyncIdentity
{
	public required string SyncKey { get; set; }
	public required string LocalDatasetKey { get; set; }
}
