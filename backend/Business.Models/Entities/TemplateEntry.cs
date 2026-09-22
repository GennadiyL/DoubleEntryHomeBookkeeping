using Business.Core.Entities;

namespace Business.Models.Entities;

public class TemplateEntry : BaseEntity
{
	public required Template Template { get; set; }
	public Guid TemplateId { get; set; }
	public required Account Account { get; set; }
	public Guid AccountId { get; set; }
	public decimal Amount { get; set; }
}
