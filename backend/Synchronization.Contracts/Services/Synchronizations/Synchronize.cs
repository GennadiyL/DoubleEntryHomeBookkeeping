namespace Synchronization.Contracts.Services.Synchronizations;

/// <summary>
/// Starts synchronization for the current application dataset.
/// Used by manual and application lifecycle synchronization actions.
/// The service obtains registration and authorization from local context.
/// It reads conflict priority from Local configuration.
/// It durably captures pending changes and allocates or reuses SyncKey.
/// Persistent models and undefined wire payloads do not cross this BFF boundary.
/// The cloud Changes protocol remains a separate unresolved TRD contract.
/// This input does not authorize overriding stored ownership or revisions.
/// </summary>
public record Synchronize
{
	public SynchronizationTrigger Trigger { get; set; }
}
