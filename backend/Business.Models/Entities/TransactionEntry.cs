using Business.Core.Entities;

namespace Business.Models.Entities;

public class TransactionEntry : BaseEntity
{
	public required Transaction Transaction { get; set; }
	public Guid TransactionId { get; set; }
	public required Account Account { get; set; }
	public Guid AccountId { get; set; }
	public decimal Amount { get; set; }
	public decimal Rate { get; set; }
}
