using Business.Models.Enums;

namespace Business.Contracts.Services.Transactions;

/// <summary>
/// Supplies transaction properties and the complete ordered entry collection for saving.
/// DateTime is the transaction occurrence expressed in UTC.
/// State requests Draft, Planned or Confirmed, defaulting to Confirmed for existing callers.
/// Business forces Draft when fewer than two entries or a nonzero base total prevent confirmation.
/// Zero or one entry and an unbalanced total may be saved as Draft.
/// Every present entry still requires a valid account, rate and numeric amount.
/// Update preserves the parent identity and replaces all entries with newly created rows.
/// Entry list order determines their persisted positions.
/// Validation and persistence failures leave the complete prior aggregate unchanged.
/// </summary>
public record TransactionParam
{
	public TransactionState State { get; set; } = TransactionState.Confirmed;
	public DateTime DateTime { get; set; }
	public string? Description { get; set; }
	public List<TransactionEntryParam> Entries { get; } = new();
}
