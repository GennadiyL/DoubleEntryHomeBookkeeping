using Business.Core.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;

namespace Business.Models.Entities;

public class Transaction : BaseEntity, ITrackedEntity
{
	public DateTime Original { get; set; }
	public DateTime Current { get; set; }
	public bool IsDeleted { get; set; }
	public DateTime DateTime { get; set; }
	public TransactionState State { get; set; }
	public string? Comment { get; set; }
	public List<TransactionEntry> Entries { get; set; } = new();
}
