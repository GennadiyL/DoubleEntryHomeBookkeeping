using Business.Core.Entities;

namespace Business.Models.Entities;

/// <summary>
/// Represents an account-bearing entry owned by a transaction.
/// Stores parent and account references with their matching foreign keys.
/// Amount, Rate, and Position preserve entry content and order, including repeated accounts.
/// Entry creation, editing, deletion, and synchronization follow the parent transaction lifecycle.
/// Writable inherited identity supports creation and materialization of persistent state.
/// The model carries data; business services implement validation and lifecycle operations.
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
