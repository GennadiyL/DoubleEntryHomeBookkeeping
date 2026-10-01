using Business.Models.Entities;
using Business.Models.Entities.Config;

namespace Business.Contracts.Utils.Models;

/// <summary>
/// Calculates derived base-currency values from transaction entry data.
/// Uses the stored entry Amount and Rate rather than the current catalog rate.
/// Rounds each product to the System configuration amount precision.
/// Midpoint-to-even rounding matches transaction validation and accounting totals.
/// The precision must be within the supported zero-to-four range.
/// Decimal arithmetic failures propagate instead of producing a clamped value.
/// BaseAmount is derived and is neither stored nor independently synchronized.
/// Calling this helper does not mutate the entry or configuration.
/// </summary>
public static class TransactionEntryExtensions
{
	public static decimal GetBaseAmount(this TransactionEntry entry, SystemConfig config)
	{
		ArgumentNullException.ThrowIfNull(entry);
		ArgumentNullException.ThrowIfNull(config);
		ArgumentOutOfRangeException.ThrowIfLessThan(config.AmountPrecision, 0);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(config.AmountPrecision, 4);
		return Math.Round(entry.Amount * entry.Rate, config.AmountPrecision, MidpointRounding.ToEven);
	}
}
