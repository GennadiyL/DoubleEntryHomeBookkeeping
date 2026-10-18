namespace Business.Models.Enums;

/// <summary>
/// Identifies the lifecycle state stored for a bookkeeping transaction.
/// Undefined is an unset sentinel and is never a valid persisted transaction state.
/// Draft can be selected explicitly and is forced for fewer than two entries or a nonzero rounded base total.
/// Draft permits empty entries but still requires valid dates, references and numeric values.
/// Confirmed requires at least two valid entries and an exact zero rounded base total.
/// Only Confirmed contributes to accounting balances, totals and reports.
/// Planned retains a valid balanced transaction without contributing to accounting balances.
/// Services honor the requested state for valid balanced entries and otherwise force Draft.
/// </summary>
public enum TransactionState
{
	Undefined = 0,
	Draft = 1,
	Planned = 2,
	Confirmed = 3
}
