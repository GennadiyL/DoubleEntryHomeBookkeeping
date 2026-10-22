using System.Text.Json.Serialization;

namespace Business.Impl.Operations.Currency;

/// <summary>
/// Contains the permanent identity and display defaults of a supported currency.
/// Id is assigned once in the bundled catalog and shared by every installation.
/// Code identifies the currency independently of its editable dataset name.
/// Symbol and EnglishName provide defaults for new persistent currency rows.
/// Init-only properties preserve the deserialized catalog values.
/// This profile is internal to the Business implementation.
/// Dataset membership, exchange rates and synchronization metadata are separate.
/// Catalog updates preserve existing identities and may add new profiles.
/// </summary>
internal record CurrencyProfile
{
	[JsonPropertyName("id")]
	public required Guid Id { get; init; }
	[JsonPropertyName("code")]
	public required string Code { get; init; }
	[JsonPropertyName("symbol")]
	public required string Symbol { get; init; }
	[JsonPropertyName("englishName")]
	public required string EnglishName { get; init; }
}
