using System.Text.Json.Serialization;

namespace Exchange.Models.Synchronizations;

/// <summary>
/// Contains the pending synchronization document exchanged between Local and Master.
/// FormatVersion identifies the document format independently of the database schema.
/// Revision is the Master snapshot revision currently installed on Local.
/// SchemaVersion identifies the business schema used by the captured data.
/// MasterDatasetKey and LocalDatasetKey scope every contained batch.
/// Batches are processed in their stored array order, never by timestamp.
/// Previously captured batches retain their identities and values on retries.
/// The document carries no credentials or database snapshot bytes.
/// Services validate versions, ownership and content before applying changes.
/// Default IJsonService options populate collections without reference handling.
/// </summary>
public record SyncContainer
{
	public const int CurrentFormatVersion = 2;

	[JsonPropertyName(nameof(FormatVersion))]
	public required int FormatVersion { get; set; }

	[JsonPropertyName(nameof(SchemaVersion))]
	public required int SchemaVersion { get; set; }

	[JsonPropertyName(nameof(MasterDatasetKey))]
	public required string MasterDatasetKey { get; set; }

	[JsonPropertyName(nameof(LocalDatasetKey))]
	public required string LocalDatasetKey { get; set; }

	[JsonPropertyName(nameof(Revision))]
	public long Revision { get; set; }

	[JsonPropertyName(nameof(Batches))]
	[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
	public List<SyncBatch> Batches { get; } = new List<SyncBatch>();
}
