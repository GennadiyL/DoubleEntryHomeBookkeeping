namespace Business.Impl.Operations.Currency;

/// <summary>
/// Contains regional display defaults for one ISO currency.
/// Code identifies the currency within the offline culture catalog.
/// Symbol and Name come from the first usable matching region.
/// Init-only properties preserve the values selected during enumeration.
/// This profile is internal to the Business implementation.
/// It carries no persisted identity or synchronization metadata.
/// Exchange rates and dataset membership are outside its scope.
/// The currency service maps it to public selector records.
/// </summary>
internal record CurrencyProfile
{
	public required string Code { get; init; }
	public required string Symbol { get; init; }
	public required string Name { get; init; }
}
