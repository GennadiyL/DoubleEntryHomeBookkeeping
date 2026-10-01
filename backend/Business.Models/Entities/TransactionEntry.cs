using Business.Core.Entities;

namespace Business.Models.Entities;

/// <summary>
/// Represents an account-bearing entry owned by a transaction.
/// Parent and account references match their corresponding foreign keys.
/// Amount is in account currency; Rate independently stores conversion to base currency.
/// Base amount is derived by midpoint-to-even rounding of Amount times Rate.
/// Position is the zero-based entry index within the complete transaction.
/// Repeated accounts remain separate entries with their own amounts and rates.
/// Aggregate updates replace all entry rows with new identities in submitted list order.
/// Entries have no independent synchronization flags; changes mark the parent content.
/// Creation, replacement and deletion are managed at the transaction aggregate boundary.
/// </summary>
public class TransactionEntry : BaseEntity
{
	public required Transaction Transaction { get; set; }
	public Guid TransactionId { get; set; }
	public required Account Account { get; set; }
	public Guid AccountId { get; set; }
	public decimal Amount { get; set; }
	public int Position { get; set; }
	public decimal Rate { get; set; }
}
