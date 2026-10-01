namespace Business.Contracts.Services.Currencies;

/// <summary>
/// Supplies a currency/date pair and values for a rate upsert.
/// CurrencyId and Date identify the target without a caller-supplied row identity.
/// Date is a calendar date and undergoes no timezone conversion.
/// An existing pair is updated; an absent pair creates a new rate.
/// Rate must remain positive after configured rounding; the base-currency rate is one.
/// The initial fallback date is fixed; ordinary dates start at 2001-01-01.
/// Description is optional and independent of transaction or template descriptions.
/// Changing the requested date targets another pair rather than moving an existing row.
/// </summary>
public record CurrencyRateParam
{
	public Guid CurrencyId { get; set; }

	public DateOnly Date { get; set; }

	public decimal Rate { get; set; }

	public string? Description { get; set; }
}
