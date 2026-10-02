using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Shared.Contracts;
using Shared.Impl.Json;

namespace Shared.Impl.Services;

/// <summary>
/// Defines the JSON service.
/// Serializes and deserializes JSON with the shared default serializer policy.
/// Consumers resolve IJsonService for text and stream conversion operations.
/// It configures the internal decimal converter without exposing System.Text.Json choices.
/// It does not contain business-specific DTO mapping.
/// </summary>
internal class JsonService : IJsonService
{
	private readonly int _decimalPlaces = 4;
	private readonly MidpointRounding _roundingMode = MidpointRounding.ToEven;

	public string Serialize<T>(T obj, JsonSerializerOptions? options = null) where T : class
	{
		options = GetJsonSerializerOptions(options);
		string json = JsonSerializer.Serialize(obj, options);
		return json;
	}

	public T? Deserialize<T>(string json, JsonSerializerOptions? options = null) where T : class
	{
		options = GetJsonSerializerOptions(options);
		T? obj = JsonSerializer.Deserialize<T>(json, options);
		return obj;
	}

	private JsonSerializerOptions GetJsonSerializerOptions(JsonSerializerOptions? options)
	{
		options ??= new JsonSerializerOptions
		{
			WriteIndented = true,
			PropertyNameCaseInsensitive = true,
			ReferenceHandler = ReferenceHandler.IgnoreCycles,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.Never,
			PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate
		};

		options.Converters.Add(new JsonStringEnumConverter());
		options.Converters.Add(new JsonDecimalFormatConverter(_decimalPlaces, _roundingMode));

		return options;
	}
}
