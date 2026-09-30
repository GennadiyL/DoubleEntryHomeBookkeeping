using Business.Core.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;

namespace Business.Models.Entities;

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
