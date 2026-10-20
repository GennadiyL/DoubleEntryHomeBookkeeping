using System.Text.Json.Serialization;
using System.Text.Json;
using Exchange.Models.Enums;

namespace Exchange.Models.Synchronizations;

/// <summary>
/// Describes one captured business object using a stable protocol type name.
/// Type is resolved through supported protocol names, never arbitrary CLR type loading.
/// Id preserves the business object's identity across copies and retries.
/// EditRevision is null for creation, zero for an unknown accepted revision, or positive.
/// ModificationType distinguishes content changes from independent catalog ordering.
/// Properties preserves native JSON values, including nulls and nested arrays or objects.
/// Content changes carry complete business content; order-only changes carry ordering data.
/// Transaction and template entries are nested inside their parent aggregate content.
/// Master assigns resulting revisions; this document does not assign UpdateRevision.
/// </summary>
public record SyncObject
{
	[JsonPropertyName("type")]
	public required string Type { get; set; }

	[JsonPropertyName("id")]
	public required Guid Id { get; set; }

	[JsonPropertyName("editRevision")]
	public required long? EditRevision { get; set; }

	[JsonPropertyName("modificationType")]
	public required ModificationType ModificationType { get; set; }

	[JsonPropertyName("properties")]
	[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
	public Dictionary<string, JsonElement> Properties { get; } = new Dictionary<string, JsonElement>();
}
