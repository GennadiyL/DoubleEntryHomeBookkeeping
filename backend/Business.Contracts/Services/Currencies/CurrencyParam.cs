namespace Business.Contracts.Services.Currencies;

/// <summary>
/// Supplies currency metadata for creation or editing.
/// Code identifies the ISO currency and must be unique in the dataset.
/// The first successful save fixes Code; updates preserve it.
/// Name and Symbol are editable display values.
/// The creation operation supplies the initial rate separately.
/// Favorite status and catalog ordering use their separate operations.
/// Identity and synchronization metadata are assigned by the service.
/// Currency creation and its initial rate commit together.
/// </summary>
public record CurrencyParam
{
	public required string Code { get; set; }

	public required string Symbol { get; set; }

	public required string Name { get; set; }
}
