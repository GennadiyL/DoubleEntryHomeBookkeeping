using Business.Core.Entities;
using Business.Models.Constants;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;

namespace Business.Models.Entities;

/// <summary>
/// Represents a dated exchange rate to the dataset base currency.
/// CurrencyId and Date identify the pair used by rate upserts.
/// Date is a calendar date without timezone conversion; Description is optional.
/// Rate is positive after configured rounding and is one for the base currency.
/// IsInitial derives from the shared 1970-01-01 fallback-date constant.
/// The initial row cannot be deleted or moved to another date; its permitted value remains editable.
/// Ordinary rates use dates from 2001-01-01 and follow the shared soft-delete lifecycle.
/// Rate lookup selects the latest applicable date without changing stored transaction-entry rates.
/// </summary>
public class CurrencyRate : BaseEntity, ITrackedEntity
{
	public long? EditRevision { get; set; }
	public long? DeleteRevision { get; set; }
	public ModificationType ModificationType { get; set; }
	public required Currency Currency { get; set; }
	public Guid CurrencyId { get; set; }
	public DateOnly Date { get; set; }
	public decimal Rate { get; set; }
	public string? Description { get; set; }
	public bool IsInitial => Date == AppValues.InitialDate;
}
