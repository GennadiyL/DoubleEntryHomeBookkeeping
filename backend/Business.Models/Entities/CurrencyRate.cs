using Business.Core.Entities;
using Business.Models.Constants;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;

namespace Business.Models.Entities;

/// <summary>
/// Represents a dated exchange rate to the dataset base currency.
/// Stores a currency reference, decimal rate, calendar date, and optional description.
/// IsInitial derives the fallback marker from the shared initial-date constant.
/// Services validate rates and select applicable dates; transaction entries retain independent rates.
/// Writable inherited identity supports creation and materialization of persistent state.
/// The model carries data; business services implement validation and lifecycle operations.
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
	public bool IsInitial => Date == MainConstants.InitialDate;
}
