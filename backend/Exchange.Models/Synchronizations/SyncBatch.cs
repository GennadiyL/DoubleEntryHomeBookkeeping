using System.Text.Json.Serialization;
using Exchange.Models.Enums;

namespace Exchange.Models.Synchronizations;

/// <summary>
/// Contains one durable batch of captured Local changes.
/// SyncKey is reused unchanged when a previous outcome is uncertain.
/// CreatedAt records the capture instant in UTC for diagnostics only.
/// Priority is captured with the batch and preserved across retries.
/// Added, Updated and Deleted express the operation for each object.
/// Deleted membership expresses deletion without a client-assigned deletion revision.
/// Master deduplicates the batch before conflict resolution and revision allocation.
/// Content and ordering are interpreted according to each object's modification flags.
/// </summary>
public record SyncBatch
{
	[JsonPropertyName("syncKey")]
	public required string SyncKey { get; set; }

	[JsonPropertyName("createdAt")]
	public required DateTime CreatedAt { get; set; }

	[JsonPropertyName("priority")]
	public required ConflictPriority Priority { get; set; }

	[JsonPropertyName("added")]
	[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
	public List<SyncObject> Added { get; } = new List<SyncObject>();

	[JsonPropertyName("updated")]
	[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
	public List<SyncObject> Updated { get; } = new List<SyncObject>();

	[JsonPropertyName("deleted")]
	[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
	public List<SyncObject> Deleted { get; } = new List<SyncObject>();
}
