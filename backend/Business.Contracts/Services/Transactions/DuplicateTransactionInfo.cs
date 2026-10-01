namespace Business.Contracts.Services.Transactions;

/// <summary>
/// Contains an unsaved transaction prepared by duplication.
/// DateTime is set to the current UTC time.
/// Accounts, amounts, stored rates and description come from the source.
/// Entry order is preserved in the stable collection.
/// No persistent transaction or entry identity is assigned here.
/// The user may edit the values before saving.
/// The normal Add operation validates and persists the aggregate.
/// Canceling discards these values without changing the source.
/// </summary>
public record DuplicateTransactionInfo
{
	public DateTime DateTime { get; set; }
	public string? Description { get; set; }
	public List<TransactionEntryInfo> Entries { get; } = new();
}
