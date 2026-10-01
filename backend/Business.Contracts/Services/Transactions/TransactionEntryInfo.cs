namespace Business.Contracts.Services.Transactions;

/// <summary>
/// Describes one entry within a transaction result.
/// Amount is expressed in the referenced account currency.
/// Rate is the stored conversion rate to base currency.
/// The containing list preserves stored Position order.
/// An entry has no independently editable identity in this contract.
/// Aggregate edits replace the complete entry list.
/// Base amounts are derived using configured precision and midpoint-to-even rounding.
/// Changing this record does not update persistence.
/// </summary>
public record TransactionEntryInfo
{
	public Guid AccountId { get; set; }
	public decimal Amount { get; set; }
	public decimal Rate { get; set; }
}
