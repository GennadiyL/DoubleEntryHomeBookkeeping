namespace Business.Models.Enums;

/// <summary>
/// Identifies the lifecycle state stored for a bookkeeping transaction.
/// Undefined is unset; Draft is unbalanced and excluded from balances and reports.
/// Confirmed is valid and balanced; Planned reserves a future workflow.
/// Services determine valid states on save and prevent Confirmed transactions returning to Draft.
/// Explicit numeric values preserve the meaning of stored or exchanged selections.
/// The enum describes state or preferences without executing the corresponding operations.
/// </summary>
public enum TransactionState
{
	Undefined = 0,
	Draft = 1,
	Planned = 2,
	Confirmed = 3
}
