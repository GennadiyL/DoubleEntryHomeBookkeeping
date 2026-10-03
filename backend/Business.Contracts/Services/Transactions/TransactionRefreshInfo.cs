namespace Business.Contracts.Services.Transactions;

/// <summary>
/// Contains fresh complete data for a previously displayed selection.
/// Transactions includes only retained matching live records.
/// RemovedTransactionIds identifies rows that left the selection.
/// No new identities are inserted to fill vacant positions.
/// Retained records use the same newest-first list ordering.
/// Cumulative entry values reflect the completed database mutation.
/// The client can warn when its edited identity was removed.
/// The result is a fresh read rather than a field-level delta.
/// </summary>
public record TransactionRefreshInfo
{
	public List<TransactionInfo> Transactions { get; } = new();
	public List<Guid> RemovedTransactionIds { get; } = new();
}
