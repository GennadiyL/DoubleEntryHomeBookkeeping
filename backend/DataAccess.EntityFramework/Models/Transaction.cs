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

	public DateTime Original
	{
		get;
		set;
	}

	public DateTime Current
	{
		get;
		set;
	}

	public bool IsDeleted
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

	public string? Comment
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
