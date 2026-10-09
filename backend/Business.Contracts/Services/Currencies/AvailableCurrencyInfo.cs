namespace Business.Contracts.Services.Currencies;

/// <summary>
/// Describes a currency offered by the offline culture catalog.
/// One record is returned per ISO currency code.
/// The first usable regional name and symbol are retained for that code.
/// Neutral or invalid cultures do not create records.
/// This result supplies the currency creation selector.
/// It does not represent a persisted currency.
/// No database identity or rate history is included.
/// Adding a currency is a separate mutation.
/// </summary>
public record AvailableCurrencyInfo
{
	public string Code { get; set; } = string.Empty;
	public string EnglishName { get; set; } = string.Empty;
	public string Symbol { get; set; } = string.Empty;
}
