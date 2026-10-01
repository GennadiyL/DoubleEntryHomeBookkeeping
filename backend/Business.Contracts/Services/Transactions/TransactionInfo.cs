using Business.Models.Enums;

namespace Business.Contracts.Services.Transactions;

/// <summary>
/// Describes a persisted transaction returned for viewing or editing.
/// DateTime uses UTC; date filters use the current device timezone.
/// Entries contains the whole aggregate, even when only one entry matches a filter.
/// Entries are ordered by stored Position.
/// The collection instance is stable and initially empty.
/// State distinguishes Draft from Confirmed transactions.
/// No persistence navigation or synchronization fields are exposed.
/// Edits are saved through the transaction mutation contract.
/// </summary>
public record TransactionInfo
{
	public Guid Id { get; set; }
	public DateTime DateTime { get; set; }
	public TransactionState State { get; set; }
	public string? Description { get; set; }
	public List<TransactionEntryInfo> Entries { get; } = new();
}
