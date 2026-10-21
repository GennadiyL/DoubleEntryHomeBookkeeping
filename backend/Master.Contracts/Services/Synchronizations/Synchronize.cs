namespace Master.Contracts.Services.Synchronizations;

/// <summary>
/// Submits a durable synchronization attempt to the Master business service.
/// SyncKey identifies the attempt and is reused after an uncertain response.
/// KnownVersion identifies the business snapshot currently installed on Local.
/// SchemaVersion must match the Master schema before changes are exchanged.
/// Priority selects preferred conflicting content, subject to business validity.
/// ChangesJson carries the ordered pending batches with their original immutable identities.
/// The JSON remains opaque at this boundary; provider mappings interpret the exchange document.
/// Master skips batches already merged and does not apply their changes a second time.
/// Neither Local UI state nor persistent entity instances cross this boundary.
/// </summary>
public record Synchronize
{
	public required string SyncKey { get; set; }
	public long KnownVersion { get; set; }
	public int SchemaVersion { get; set; }
	public ConflictPriority Priority { get; set; }
	public required string ChangesJson { get; set; }
}
