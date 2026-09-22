using Business.Core.Entities;

namespace Business.Models.Entities.Config;

public class SystemConfig : BaseEntity
{
	public required string MainCurrencyIsoCode { get; set; }
	public DateOnly MinDate { get; set; }
	public DateOnly MaxDate { get; set; }
}
