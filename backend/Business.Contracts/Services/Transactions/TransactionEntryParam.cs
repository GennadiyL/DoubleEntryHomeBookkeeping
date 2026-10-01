namespace Business.Contracts.Services.Transactions;

/// <summary>
/// Supplies one entry when creating or replacing an aggregate.
/// AccountId identifies the account used by this entry.
/// Amount is expressed in that account currency.
/// The containing list determines order and the saved Position.
/// No persistent entry identity is supplied by the caller.
/// Editing the parent replaces its complete entry set.
/// The service validates entries together with their aggregate.
/// This input does not perform persistence or synchronization.
/// </summary>
public record TransactionEntryParam
{
	public Guid AccountId { get; set; }
	public decimal Amount { get; set; }
	public decimal Rate { get; set; }
}
