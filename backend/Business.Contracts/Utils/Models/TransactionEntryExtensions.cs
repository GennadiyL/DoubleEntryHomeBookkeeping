using Business.Models.Entities;
using Business.Models.Entities.Config;

namespace Business.Contracts.Utils.Models;

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
