using Business.Models.Entities;

namespace Business.Contracts.Utils.Models;

/// <summary>
/// Calculates derived base-currency values from transaction entry data.
/// Uses the stored entry Amount and Rate rather than the current catalog rate.
/// Rounds each product to the supplied amount precision.
/// Midpoint-to-even rounding matches transaction validation and accounting totals.
/// The precision must be non-null and within the supported zero-to-four range.
/// Decimal arithmetic failures propagate instead of producing a clamped value.
/// BaseAmount is derived and is neither stored nor independently synchronized.
/// Calling this helper does not mutate the entry.
/// </summary>
public static class TransactionEntryExtensions
{
	public static decimal GetBaseAmount(this TransactionEntry entry, int? amountPrecision)
	{
		ArgumentNullException.ThrowIfNull(entry);
		if (!amountPrecision.HasValue)
		{
			throw new ArgumentNullException(nameof(amountPrecision));
		}

		int precision = amountPrecision.Value;
		ArgumentOutOfRangeException.ThrowIfLessThan(precision, 0);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(precision, 4);
		return Math.Round(entry.Amount * entry.Rate, precision, MidpointRounding.ToEven);
	}
}
