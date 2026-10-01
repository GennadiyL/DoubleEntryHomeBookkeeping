using Business.Core.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;

namespace Business.Models.Entities;

/// <summary>
/// Represents a bookkeeping transaction and its complete entry collection.
/// Stores the occurrence as a UTC instant and an optional aggregate description.
/// Services derive State on every save instead of accepting a caller-supplied state.
/// Fewer than two entries or a nonzero rounded base total produces Draft.
/// Confirmed requires at least two valid entries and an exact zero rounded base total.
/// Draft permits empty or unbalanced entries but retains date, reference and numeric validation.
/// Only Confirmed transactions contribute to accounting balances and reports.
/// Updates preserve transaction identity and replace the complete entry set.
/// The transaction and its entries synchronize as one content aggregate.
/// Creation uses null revisions and None flags; application deletion is soft deletion.
/// </summary>
public class Transaction : BaseEntity, ITrackedEntity
{
	public long? EditRevision { get; set; }
	public long? DeleteRevision { get; set; }
	public ModificationType ModificationType { get; set; }
	public DateTime DateTime { get; set; }
	public TransactionState State { get; set; }
	public string? Description { get; set; }
	public List<TransactionEntry> Entries { get; set; } = new();
}
