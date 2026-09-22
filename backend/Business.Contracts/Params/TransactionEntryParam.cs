namespace Business.Contracts.Params;

public class TransactionEntryParam
{
	public Guid AccountId { get; set; }
	public decimal Amount { get; set; }
	public decimal Rate { get; set; }
}
