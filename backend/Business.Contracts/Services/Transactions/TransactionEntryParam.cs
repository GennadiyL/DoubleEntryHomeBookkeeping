namespace Business.Contracts.Services.Transactions;

/// <summary>
/// Supplies one entry when creating or replacing a transaction aggregate.
/// AccountId identifies a live account and Amount is expressed in its currency.
/// Rate is an independently supplied conversion to the dataset base currency.
/// Configured rounding must leave Rate positive; a base-currency account uses one.
/// The containing list determines the saved zero-based Position.
/// No persistent entry identity is supplied; parent editing replaces the complete set.
/// Services validate references, numeric storage bounds and aggregate calculations.
/// The input performs no persistence or independent entry synchronization.
/// </summary>
public record TransactionEntryParam
{
	public Guid AccountId { get; set; }
	public decimal Amount { get; set; }
	public decimal Rate { get; set; }
}
