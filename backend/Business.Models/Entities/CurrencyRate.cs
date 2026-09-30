using Business.Core.Entities;
using Business.Models.Constants;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;

namespace Business.Models.Entities;

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
