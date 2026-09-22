using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

internal class TransactionEntry : IDalEntity
{
	public Guid Id
	{
		get;
		set;
	}

	public Transaction? Transaction
	{
		get;
		set;
	}

	public Guid TransactionId
	{
		get;
		set;
	}

	public Account? Account
	{
		get;
		set;
	}

	public Guid AccountId
	{
		get;
		set;
	}

	public decimal Amount
	{
		get;
		set;
	}

	public decimal Rate
	{
		get;
		set;
	}
}
