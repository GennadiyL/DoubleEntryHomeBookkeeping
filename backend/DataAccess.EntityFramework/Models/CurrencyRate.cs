using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

internal class CurrencyRate : IDalEntity
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

	public Currency? Currency
	{
		get;
		set;
	}

	public Guid CurrencyId
	{
		get;
		set;
	}

	public DateOnly Date
	{
		get;
		set;
	}

	public decimal Rate
	{
		get;
		set;
	}

	public string? Comment
	{
		get;
		set;
	}
}
