using Business.Contracts.Services.Transactions;

namespace Dehb.WebApi.Params;

/// <summary>
/// Identifies the transaction and its complete replacement editor content.
/// Business validates the aggregate and recalculates its affected balances.
/// </summary>
public record UpdateTransactionParam : TransactionParam
{
	public Guid EntityId { get; set; }
}