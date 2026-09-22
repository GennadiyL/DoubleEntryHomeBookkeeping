using Business.Core.Entities;
using Business.Models.Entities.Interfaces;

namespace Business.Models.Entities;

public class CurrencyRate : BaseEntity, ITrackedEntity
{
	public DateTime Original { get; set; }
	public DateTime Current { get; set; }
	public bool IsDeleted { get; set; }
	public required Currency Currency { get; set; }
	public Guid CurrencyId { get; set; }
	public DateOnly Date { get; set; }
	public decimal Rate { get; set; }
	public string? Comment { get; set; } = string.Empty;
}
