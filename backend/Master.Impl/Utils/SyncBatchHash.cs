using System.Security.Cryptography;
using System.Text.Json;

namespace Master.Impl.Utils;

/// <summary>
/// Computes the stable receipt fingerprint for one serialized exchange batch.
/// SHA-256 covers batch identity, time, priority, changes and nested property values.
/// JSON object members are sorted ordinally at every nesting level.
/// Insignificant whitespace and string escaping do not affect the fingerprint.
/// Array ordering is retained because it may carry business meaning.
/// Number representations are preserved rather than rounded through floating point.
/// The uppercase hexadecimal digest is persisted in LocalSyncBatch.PayloadHash.
/// Dataset scoping is supplied separately by the receipt's Local registration.
/// </summary>
internal static class SyncBatchHash
{
	public static string Calculate(string json)
	{
		using JsonDocument document = JsonDocument.Parse(json);
		using MemoryStream stream = new();
		using (Utf8JsonWriter writer = new(stream)) { Write(writer, document.RootElement); }
		return Convert.ToHexString(SHA256.HashData(stream.GetBuffer().AsSpan(0, checked((int)stream.Length))));
	}

	private static void Write(Utf8JsonWriter writer, JsonElement element)
	{
		switch (element.ValueKind)
		{
			case JsonValueKind.Object:
				writer.WriteStartObject();
				foreach (JsonProperty property in element.EnumerateObject().OrderBy(item => item.Name, StringComparer.Ordinal))
				{
					writer.WritePropertyName(property.Name);
					Write(writer, property.Value);
				}
				writer.WriteEndObject();
				break;
			case JsonValueKind.Array:
				writer.WriteStartArray();
				foreach (JsonElement item in element.EnumerateArray()) { Write(writer, item); }
				writer.WriteEndArray();
				break;
			default:
				element.WriteTo(writer);
				break;
		}
	}
}
