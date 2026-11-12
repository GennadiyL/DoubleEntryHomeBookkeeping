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
	[JsonPropertyName(nameof(SyncKey))]
	public required string SyncKey { get; set; }

	[JsonPropertyName(nameof(CreatedAt))]
	public required DateTime CreatedAt { get; set; }

	[JsonPropertyName(nameof(Priority))]
	public required ConflictPriority Priority { get; set; }

	[JsonPropertyName(nameof(Added))]
	[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
	public List<SyncObject> Added { get; } = new List<SyncObject>();

	[JsonPropertyName(nameof(Updated))]
	[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
	public List<SyncObject> Updated { get; } = new List<SyncObject>();

	[JsonPropertyName(nameof(Deleted))]
	[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
	public List<SyncObject> Deleted { get; } = new List<SyncObject>();
}
