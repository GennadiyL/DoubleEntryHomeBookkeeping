using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Impl.Json;

/// <summary>
/// Defines the decimal JSON converter.
/// Reads and writes decimal values using the shared JSON formatting policy.
/// JsonService adds one converter instance to its serializer options.
/// It keeps decimal wire formatting inside the project-independent JSON implementation.
/// It is not exposed as a service contract or used for non-decimal values.
/// </summary>
internal sealed class JsonDecimalFormatConverter : JsonConverter<decimal>
{
	private readonly int _decimalPlaces;
	private readonly MidpointRounding _roundingMode;

	public JsonDecimalFormatConverter(int decimalPlaces, MidpointRounding roundingMode)
	{
		_decimalPlaces = decimalPlaces;
		_roundingMode = roundingMode;
	}

	public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		reader.GetDecimal();

	public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
		writer.WriteNumberValue(Math.Round(value, _decimalPlaces, _roundingMode));
}
