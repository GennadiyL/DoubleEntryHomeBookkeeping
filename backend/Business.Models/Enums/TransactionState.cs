namespace Business.Models.Enums;

/// <summary>
/// Identifies the derived lifecycle state stored for a bookkeeping transaction.
/// Undefined is an unset sentinel and is never a valid persisted transaction state.
/// Draft has fewer than two entries or a nonzero rounded base total.
/// Draft permits empty entries but still requires valid dates, references and numeric values.
/// Confirmed requires at least two valid entries and an exact zero rounded base total.
/// Only Confirmed contributes to accounting balances, totals and reports.
/// Planned is reserved for future use without a first-release creation or transition workflow.
/// Services derive State on every save and may return a Confirmed transaction to Draft.
/// </summary>
public enum TransactionState
{
	Undefined = 0,
	Draft = 1,
	Planned = 2,
	Confirmed = 3
}
