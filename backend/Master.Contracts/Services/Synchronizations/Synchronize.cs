namespace Master.Contracts.Services.Synchronizations;

/// <summary>
/// Submits a durable synchronization attempt to the Master business service.
/// SyncKey identifies the attempt and is reused after an uncertain response.
/// KnownVersion identifies the business snapshot currently installed on Local.
/// SchemaVersion must match the Master schema before changes are exchanged.
/// Priority selects preferred conflicting content, subject to business validity.
/// ChangesJson carries the pending change document, whose schema is deferred.
/// It is an opaque contract placeholder, not an approved merge protocol.
/// Batch identities and ordering inside the document must be defined before implementation.
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
