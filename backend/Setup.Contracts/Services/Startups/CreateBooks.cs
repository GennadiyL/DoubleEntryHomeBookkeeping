namespace Setup.Contracts.Services.Startups;

/// <summary>
/// Supplies initial owner and dataset creation choices.
/// Used by the startup Create action after confirmed Master absence.
/// Login and Password are required and must not be logged.
/// BaseCurrencyCode selects the immutable dataset base currency.
/// AmountPrecision and RatePrecision accept values from zero through four.
/// LocalDatasetKey identifies the local copy being registered.
/// RequestId identifies a retried setup attempt; retry policy remains unresolved.
/// Successful initialization includes roots and the configured rebalancing account.
/// </summary>
public record CreateBooks
{
	public required string Login { get; set; }
	public required string Password { get; set; }
	public required string BaseCurrencyCode { get; set; }
	public int AmountPrecision { get; set; } = 2;
	public int RatePrecision { get; set; } = 4;
	public required string LocalDatasetKey { get; set; }
	public Guid RequestId { get; set; }
}
