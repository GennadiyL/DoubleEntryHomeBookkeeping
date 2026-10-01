using Business.Models.Enums;

namespace Business.Contracts.Services.Transactions;

public record TransactionParam
{
	public DateTime DateTime { get; set; }
	public TransactionState State { get; set; }
	public string? Description { get; set; }
	public List<TransactionEntryParam> Entries { get; } = new();
}
