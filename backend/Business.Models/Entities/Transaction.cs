using Business.Core.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;

namespace Business.Models.Entities;

/// <summary>
/// Represents a bookkeeping transaction and its complete entry collection.
/// Stores its UTC occurrence time, optional description, and transaction state.
/// Synchronization treats the transaction and its entries as one aggregate.
/// Services determine Draft or Confirmed state and enforce balanced confirmed transactions.
/// Writable inherited identity supports creation and materialization of persistent state.
/// The model carries data; business services implement validation and lifecycle operations.
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
