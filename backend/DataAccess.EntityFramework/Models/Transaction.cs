using Business.Models.Enums;
using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

internal class Transaction : IDalEntity
{
	public Guid Id
	{
		get;
		set;
	}

	public long? EditRevision
	{
		get;
		set;
	}

	public long? DeleteRevision
	{
		get;
		set;
	}

	public ModificationType ModificationType
	{
		get;
		set;
	}

	public DateTime DateTime
	{
		get;
		set;
	}

	public TransactionState State
	{
		get;
		set;
	}

	public string? Description
	{
		get;
		set;
	}

	public ICollection<TransactionEntry> Entries
	{
		get;
		set;
	} = new List<TransactionEntry>();
}
