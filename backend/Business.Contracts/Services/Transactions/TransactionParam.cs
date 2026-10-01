namespace Business.Contracts.Services.Transactions;

/// <summary>
/// Supplies transaction properties and the complete ordered entry collection for saving.
/// DateTime is the transaction occurrence expressed in UTC.
/// Business derives Draft or Confirmed state instead of accepting it from the caller.
/// Zero or one entry and an unbalanced total may be saved as Draft.
/// Every present entry still requires a valid account, rate and numeric amount.
/// Update preserves the parent identity and replaces all entries with newly created rows.
/// Entry list order determines their persisted positions.
/// Validation and persistence failures leave the complete prior aggregate unchanged.
/// </summary>
public record TransactionParam
{
	public DateTime DateTime { get; set; }
	public string? Description { get; set; }
	public List<TransactionEntryParam> Entries { get; } = new();
}
