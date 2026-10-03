namespace Business.Contracts.Services.Transactions;

/// <summary>
/// Contains a fresh capped transaction list for the selected end date.
/// Transactions are complete aggregates ordered newest first.
/// The count limit applies to transactions rather than entries.
/// LimitExceeded indicates that additional matching transactions exist.
/// It does not promise an exact total count.
/// Entry cumulative amounts include history outside this list.
/// Reading the result performs no cumulative maintenance.
/// The stable collection is replaced by a new result on navigation.
/// </summary>
public record TransactionListInfo
{
	public List<TransactionInfo> Transactions { get; } = new();
	public bool LimitExceeded { get; set; }
}
