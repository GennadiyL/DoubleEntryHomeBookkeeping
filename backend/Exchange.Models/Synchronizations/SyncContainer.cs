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
	[JsonPropertyName("formatVersion")]
	public required int FormatVersion { get; set; }

	[JsonPropertyName("schemaVersion")]
	public required int SchemaVersion { get; set; }

	[JsonPropertyName("masterDatasetKey")]
	public required string MasterDatasetKey { get; set; }

	[JsonPropertyName("localDatasetKey")]
	public required string LocalDatasetKey { get; set; }

	[JsonPropertyName("revision")]
	public long Revision { get; set; }

	[JsonPropertyName("batches")]
	[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
	public List<SyncBatch> Batches { get; } = new List<SyncBatch>();
}
